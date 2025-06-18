using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SnapDog2.Api.Authorization;

/// <summary>
/// Custom authorization policy provider for dynamic policy creation.
/// </summary>
public class ApiAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
    private readonly ILogger<ApiAuthorizationPolicyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiAuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">Authorization options.</param>
    /// <param name="logger">Logger instance.</param>
    public ApiAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        ILogger<ApiAuthorizationPolicyProvider> logger
    )
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the default authorization policy.
    /// </summary>
    /// <returns>The default policy.</returns>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    /// <summary>
    /// Gets the fallback authorization policy.
    /// </summary>
    /// <returns>The fallback policy.</returns>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    /// <summary>
    /// Gets an authorization policy by name.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <returns>The authorization policy.</returns>
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        _logger.LogDebug("Requesting authorization policy: {PolicyName}", policyName);

        // Handle dynamic resource ownership policies
        if (policyName.StartsWith("ResourceOwnership.", StringComparison.OrdinalIgnoreCase))
        {
            var resourceType = policyName.Substring("ResourceOwnership.".Length);
            return Task.FromResult(CreateResourceOwnershipPolicy(resourceType));
        }

        // Handle role-based policies
        if (policyName.StartsWith("RequireRole.", StringComparison.OrdinalIgnoreCase))
        {
            var role = policyName.Substring("RequireRole.".Length);
            return Task.FromResult(CreateRolePolicy(role));
        }

        // Handle permission-based policies
        if (policyName.StartsWith("RequirePermission.", StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName.Substring("RequirePermission.".Length);
            return Task.FromResult(CreatePermissionPolicy(permission));
        }

        // Handle API key scope policies
        if (policyName.StartsWith("RequireScope.", StringComparison.OrdinalIgnoreCase))
        {
            var scope = policyName.Substring("RequireScope.".Length);
            return Task.FromResult(CreateScopePolicy(scope));
        }

        // Handle admin-only policies
        if (policyName.Equals("AdminOnly", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(CreateAdminOnlyPolicy());
        }

        // Handle read-only policies
        if (policyName.Equals("ReadOnly", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(CreateReadOnlyPolicy());
        }

        // Fall back to default provider
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    /// <summary>
    /// Creates a resource ownership policy for the specified resource type.
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <returns>The authorization policy.</returns>
    private AuthorizationPolicy CreateResourceOwnershipPolicy(string resourceType)
    {
        _logger.LogDebug("Creating resource ownership policy for type: {ResourceType}", resourceType);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("ApiKey")
            .AddRequirements(new ResourceOwnershipRequirement(resourceType))
            .Build();

        return policy;
    }

    /// <summary>
    /// Creates a role-based authorization policy.
    /// </summary>
    /// <param name="role">The required role.</param>
    /// <returns>The authorization policy.</returns>
    private AuthorizationPolicy CreateRolePolicy(string role)
    {
        _logger.LogDebug("Creating role policy for role: {Role}", role);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("ApiKey")
            .RequireRole(role)
            .Build();

        return policy;
    }

    /// <summary>
    /// Creates a permission-based authorization policy.
    /// </summary>
    /// <param name="permission">The required permission.</param>
    /// <returns>The authorization policy.</returns>
    private AuthorizationPolicy CreatePermissionPolicy(string permission)
    {
        _logger.LogDebug("Creating permission policy for permission: {Permission}", permission);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("ApiKey")
            .RequireClaim("permission", permission)
            .Build();

        return policy;
    }

    /// <summary>
    /// Creates a scope-based authorization policy for API keys.
    /// </summary>
    /// <param name="scope">The required scope.</param>
    /// <returns>The authorization policy.</returns>
    private AuthorizationPolicy CreateScopePolicy(string scope)
    {
        _logger.LogDebug("Creating scope policy for scope: {Scope}", scope);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("ApiKey")
            .RequireClaim("scope", scope)
            .Build();

        return policy;
    }

    /// <summary>
    /// Creates an admin-only authorization policy.
    /// </summary>
    /// <returns>The authorization policy.</returns>
    private AuthorizationPolicy CreateAdminOnlyPolicy()
    {
        _logger.LogDebug("Creating admin-only policy");

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("ApiKey")
            .RequireRole("Admin")
            .Build();

        return policy;
    }

    /// <summary>
    /// Creates a read-only authorization policy.
    /// </summary>
    /// <returns>The authorization policy.</returns>
    private AuthorizationPolicy CreateReadOnlyPolicy()
    {
        _logger.LogDebug("Creating read-only policy");

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("ApiKey")
            .RequireAssertion(context =>
            {
                // Allow if user has read permission or any write permission
                return context.User.HasClaim("permission", "read")
                    || context.User.HasClaim("permission", "write")
                    || context.User.HasClaim("permission", "admin")
                    || context.User.IsInRole("Admin")
                    || context.User.IsInRole("User");
            })
            .Build();

        return policy;
    }
}

/// <summary>
/// Authorization policy names used throughout the application.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy requiring admin role.
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Policy allowing read-only access.
    /// </summary>
    public const string ReadOnly = "ReadOnly";

    /// <summary>
    /// Policy for audio stream management.
    /// </summary>
    public const string ManageAudioStreams = "RequirePermission.manage_audiostreams";

    /// <summary>
    /// Policy for zone management.
    /// </summary>
    public const string ManageZones = "RequirePermission.manage_zones";

    /// <summary>
    /// Policy for client management.
    /// </summary>
    public const string ManageClients = "RequirePermission.manage_clients";

    /// <summary>
    /// Policy for playlist management.
    /// </summary>
    public const string ManagePlaylists = "RequirePermission.manage_playlists";

    /// <summary>
    /// Policy for system administration.
    /// </summary>
    public const string SystemAdmin = "RequirePermission.system_admin";

    /// <summary>
    /// Creates a resource ownership policy name for the specified resource type.
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <returns>The policy name.</returns>
    public static string ResourceOwnership(string resourceType)
    {
        return $"ResourceOwnership.{resourceType}";
    }

    /// <summary>
    /// Creates a role requirement policy name for the specified role.
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <returns>The policy name.</returns>
    public static string RequireRole(string role)
    {
        return $"RequireRole.{role}";
    }

    /// <summary>
    /// Creates a permission requirement policy name for the specified permission.
    /// </summary>
    /// <param name="permission">The permission name.</param>
    /// <returns>The policy name.</returns>
    public static string RequirePermission(string permission)
    {
        return $"RequirePermission.{permission}";
    }

    /// <summary>
    /// Creates a scope requirement policy name for the specified scope.
    /// </summary>
    /// <param name="scope">The scope name.</param>
    /// <returns>The policy name.</returns>
    public static string RequireScope(string scope)
    {
        return $"RequireScope.{scope}";
    }
}
