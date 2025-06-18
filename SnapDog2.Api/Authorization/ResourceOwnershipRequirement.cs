using Microsoft.AspNetCore.Authorization;

namespace SnapDog2.Api.Authorization;

/// <summary>
/// Authorization requirement for resource ownership validation.
/// </summary>
public class ResourceOwnershipRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceOwnershipRequirement"/> class.
    /// </summary>
    /// <param name="resourceType">The type of resource to validate ownership for.</param>
    public ResourceOwnershipRequirement(string resourceType)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
    }

    /// <summary>
    /// Gets the resource type for ownership validation.
    /// </summary>
    public string ResourceType { get; }
}

/// <summary>
/// Authorization handler for resource ownership requirements.
/// </summary>
public class ResourceOwnershipHandler : AuthorizationHandler<ResourceOwnershipRequirement>
{
    private readonly ILogger<ResourceOwnershipHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceOwnershipHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public ResourceOwnershipHandler(ILogger<ResourceOwnershipHandler> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Handles the authorization requirement.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The resource ownership requirement.</param>
    /// <returns>A task representing the authorization result.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnershipRequirement requirement
    )
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HTTP context is null, failing resource ownership check");
            context.Fail();
            return Task.CompletedTask;
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("User is not authenticated, failing resource ownership check");
            context.Fail();
            return Task.CompletedTask;
        }

        // Admin users bypass ownership checks
        if (user.IsInRole("Admin") || user.HasClaim("permission", "admin"))
        {
            _logger.LogDebug("User has admin privileges, allowing access to {ResourceType}", requirement.ResourceType);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get resource ID from route
        var resourceId = GetResourceIdFromRoute(httpContext, requirement.ResourceType);
        if (string.IsNullOrEmpty(resourceId))
        {
            _logger.LogWarning("Could not extract resource ID from route for {ResourceType}", requirement.ResourceType);

            // For collection operations (no ID), allow if user has appropriate permissions
            if (IsCollectionOperation(httpContext))
            {
                if (HasCollectionPermission(user, requirement.ResourceType, httpContext.Request.Method))
                {
                    _logger.LogDebug("User has collection permission for {ResourceType}", requirement.ResourceType);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User lacks collection permission for {ResourceType}", requirement.ResourceType);
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }

        // Check if user owns the resource or has appropriate permissions
        if (ValidateResourceOwnership(user, requirement.ResourceType, resourceId))
        {
            _logger.LogDebug(
                "User validated for {ResourceType} with ID {ResourceId}",
                requirement.ResourceType,
                resourceId
            );
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User failed validation for {ResourceType} with ID {ResourceId}",
                requirement.ResourceType,
                resourceId
            );
            context.Fail();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the resource ID from the route values.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <returns>The resource ID if found; otherwise, null.</returns>
    private string? GetResourceIdFromRoute(HttpContext httpContext, string resourceType)
    {
        var routeValues = httpContext.Request.RouteValues;

        // Try common ID parameter names
        var idKeys = new[] { "id", "resourceId", $"{resourceType}Id", $"{resourceType.ToLowerInvariant()}Id" };

        foreach (var key in idKeys)
        {
            if (routeValues.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString();
            }
        }

        // Try to extract from path segments
        var path = httpContext.Request.Path.Value;
        if (!string.IsNullOrEmpty(path))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Look for GUID or numeric patterns that might be IDs
            for (int i = 0; i < segments.Length; i++)
            {
                if (Guid.TryParse(segments[i], out _) || int.TryParse(segments[i], out _))
                {
                    return segments[i];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if the current operation is a collection operation (no specific resource ID).
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>True if it's a collection operation; otherwise, false.</returns>
    private bool IsCollectionOperation(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            return false;

        // Collection operations typically don't have IDs in the path
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // If the last segment is not a GUID or number, it's likely a collection operation
        if (segments.Length > 0)
        {
            var lastSegment = segments[^1];
            return !Guid.TryParse(lastSegment, out _) && !int.TryParse(lastSegment, out _);
        }

        return true;
    }

    /// <summary>
    /// Checks if the user has permission for collection operations on the resource type.
    /// </summary>
    /// <param name="user">The user claims principal.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <returns>True if the user has permission; otherwise, false.</returns>
    private bool HasCollectionPermission(
        System.Security.Claims.ClaimsPrincipal user,
        string resourceType,
        string httpMethod
    )
    {
        var requiredPermission = GetRequiredPermission(resourceType, httpMethod);

        // Check if user has the specific permission
        if (user.HasClaim("permission", requiredPermission))
        {
            return true;
        }

        // Check for broader permissions
        if (httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            // Read operations - check for read permission
            return user.HasClaim("permission", "read") || user.HasClaim("permission", "write") || user.IsInRole("User");
        }

        // Write operations - check for write permission
        return user.HasClaim("permission", "write") || user.IsInRole("User");
    }

    /// <summary>
    /// Gets the required permission for the resource type and HTTP method.
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <returns>The required permission.</returns>
    private string GetRequiredPermission(string resourceType, string httpMethod)
    {
        var action = httpMethod.ToUpperInvariant() switch
        {
            "GET" => "read",
            "POST" => "create",
            "PUT" => "update",
            "PATCH" => "update",
            "DELETE" => "delete",
            _ => "access",
        };

        return $"{action}_{resourceType.ToLowerInvariant()}";
    }

    /// <summary>
    /// Validates resource ownership for the user.
    /// </summary>
    /// <param name="user">The user claims principal.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <returns>True if the user owns the resource or has permission; otherwise, false.</returns>
    private bool ValidateResourceOwnership(
        System.Security.Claims.ClaimsPrincipal user,
        string resourceType,
        string resourceId
    )
    {
        // Get user ID from claims
        var userId =
            user.FindFirst("sub")?.Value
            ?? user.FindFirst("user_id")?.Value
            ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Could not extract user ID from claims");
            return false;
        }

        // For now, implement basic ownership logic
        // In a real implementation, this would query the database to check ownership

        // Check if user has ownership claim for this specific resource
        var ownershipClaim = $"owns_{resourceType.ToLowerInvariant()}_{resourceId}";
        if (user.HasClaim("ownership", ownershipClaim))
        {
            return true;
        }

        // Check if user has general ownership claims for this resource type
        var generalOwnershipClaim = $"owns_{resourceType.ToLowerInvariant()}";
        if (user.HasClaim("ownership", generalOwnershipClaim))
        {
            return true;
        }

        // Check resource-specific permissions
        var resourcePermission = $"access_{resourceType.ToLowerInvariant()}_{resourceId}";
        if (user.HasClaim("permission", resourcePermission))
        {
            return true;
        }

        // Check if user has manage permission for this resource type
        var managePermission = $"manage_{resourceType.ToLowerInvariant()}";
        if (user.HasClaim("permission", managePermission))
        {
            return true;
        }

        _logger.LogDebug(
            "User {UserId} does not own or have permission for {ResourceType} {ResourceId}",
            userId,
            resourceType,
            resourceId
        );

        return false;
    }
}

/// <summary>
/// Resource types used for ownership validation.
/// </summary>
public static class ResourceTypes
{
    /// <summary>
    /// Audio stream resource type.
    /// </summary>
    public const string AudioStream = "AudioStream";

    /// <summary>
    /// Zone resource type.
    /// </summary>
    public const string Zone = "Zone";

    /// <summary>
    /// Client resource type.
    /// </summary>
    public const string Client = "Client";

    /// <summary>
    /// Playlist resource type.
    /// </summary>
    public const string Playlist = "Playlist";

    /// <summary>
    /// Radio station resource type.
    /// </summary>
    public const string RadioStation = "RadioStation";

    /// <summary>
    /// Track resource type.
    /// </summary>
    public const string Track = "Track";
}
