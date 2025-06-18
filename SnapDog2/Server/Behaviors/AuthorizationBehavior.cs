using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that provides authorization checks for secured operations.
/// Validates that the current user has permission to execute the requested operation.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AuthorizationBehavior(ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the authorization pipeline.
    /// </summary>
    /// <param name="request">The request to authorize.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response, potentially with authorization checks applied.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;

        // Check if this request requires authorization
        if (!RequiresAuthorization(request))
        {
            _logger.LogTrace("No authorization required for request: {RequestName}", requestName);
            return await next();
        }

        _logger.LogDebug("Checking authorization for request: {RequestName}", requestName);

        // Perform authorization checks
        var authorizationResult = await CheckAuthorizationAsync(request, cancellationToken);

        if (authorizationResult.IsFailure)
        {
            _logger.LogWarning(
                "Authorization failed for request: {RequestName}. Reason: {Reason}",
                requestName,
                authorizationResult.Error
            );

            return CreateUnauthorizedResponse<TResponse>(authorizationResult.Error!);
        }

        _logger.LogTrace("Authorization passed for request: {RequestName}", requestName);
        return await next();
    }

    /// <summary>
    /// Determines if a request requires authorization.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if authorization is required; otherwise, false.</returns>
    private static bool RequiresAuthorization(TRequest request)
    {
        // Check if the request implements any authorization marker interfaces
        return request is IRequireAuthorization
            || request is IRequireAdminAuthorization
            || request is IRequireOwnershipAuthorization
            || IsCommandRequest(request);
    }

    /// <summary>
    /// Determines if the request is a command (which typically requires authorization).
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if it's a command request; otherwise, false.</returns>
    private static bool IsCommandRequest(TRequest request)
    {
        var requestName = typeof(TRequest).Name;
        return requestName.Contains("Command", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Update", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Start", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Stop", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the actual authorization check.
    /// </summary>
    /// <param name="request">The request to authorize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization result.</returns>
    private async Task<Result> CheckAuthorizationAsync(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // For now, this is a placeholder implementation
            // In a real application, this would:
            // 1. Get the current user from HttpContext or similar
            // 2. Check user roles and permissions
            // 3. Validate against the specific request requirements
            // 4. Check resource ownership if applicable

            // Handle different authorization requirements
            if (request is IRequireAdminAuthorization)
            {
                return await CheckAdminAuthorizationAsync(request, cancellationToken);
            }

            if (request is IRequireOwnershipAuthorization ownershipRequest)
            {
                return await CheckOwnershipAuthorizationAsync(ownershipRequest, cancellationToken);
            }

            if (request is IRequireAuthorization)
            {
                return await CheckBasicAuthorizationAsync(request, cancellationToken);
            }

            // Default authorization for commands
            if (IsCommandRequest(request))
            {
                return await CheckBasicAuthorizationAsync(request, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization check for request: {RequestName}", typeof(TRequest).Name);
            return Result.Failure("Authorization check failed due to an internal error.");
        }
    }

    /// <summary>
    /// Checks basic user authorization.
    /// </summary>
    /// <param name="request">The request to authorize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization result.</returns>
    private async Task<Result> CheckBasicAuthorizationAsync(TRequest request, CancellationToken cancellationToken)
    {
        // Placeholder implementation
        // In a real application, this would check if the user is authenticated
        await Task.CompletedTask;

        // For now, assume all basic authorization passes
        // This will be implemented properly when the API layer is added
        return Result.Success();
    }

    /// <summary>
    /// Checks admin-level authorization.
    /// </summary>
    /// <param name="request">The request to authorize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization result.</returns>
    private async Task<Result> CheckAdminAuthorizationAsync(TRequest request, CancellationToken cancellationToken)
    {
        // Placeholder implementation
        // In a real application, this would check if the user has admin role
        await Task.CompletedTask;

        // For now, assume admin authorization passes
        // This will be implemented properly when the API layer is added
        return Result.Success();
    }

    /// <summary>
    /// Checks ownership-based authorization.
    /// </summary>
    /// <param name="ownershipRequest">The request requiring ownership authorization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization result.</returns>
    private async Task<Result> CheckOwnershipAuthorizationAsync(
        IRequireOwnershipAuthorization ownershipRequest,
        CancellationToken cancellationToken
    )
    {
        // Placeholder implementation
        // In a real application, this would:
        // 1. Get the resource ID from the request
        // 2. Check if the current user owns the resource
        // 3. Validate the ownership relationship
        await Task.CompletedTask;

        var resourceId = ownershipRequest.GetResourceId();
        if (string.IsNullOrEmpty(resourceId))
        {
            return Result.Failure("Resource ID is required for ownership authorization.");
        }

        // For now, assume ownership authorization passes
        // This will be implemented properly when the API layer is added
        return Result.Success();
    }

    /// <summary>
    /// Creates an unauthorized response for the given response type.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An unauthorized response.</returns>
    private static T CreateUnauthorizedResponse<T>(string errorMessage)
    {
        // Handle Result<T> responses
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(T).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), new[] { typeof(string) });

            if (failureMethod != null)
            {
                var failureResult = failureMethod.Invoke(null, new object[] { errorMessage });
                return (T)failureResult!;
            }
        }

        // Handle non-generic Result responses
        if (typeof(T) == typeof(Result))
        {
            var failureResult = Result.Failure(errorMessage);
            return (T)(object)failureResult;
        }

        // For other response types, throw an exception
        throw new UnauthorizedAccessException(errorMessage);
    }
}

/// <summary>
/// Marker interface for requests that require basic user authorization.
/// </summary>
public interface IRequireAuthorization { }

/// <summary>
/// Marker interface for requests that require administrator authorization.
/// </summary>
public interface IRequireAdminAuthorization : IRequireAuthorization { }

/// <summary>
/// Marker interface for requests that require ownership-based authorization.
/// The user must own the resource being accessed or modified.
/// </summary>
public interface IRequireOwnershipAuthorization : IRequireAuthorization
{
    /// <summary>
    /// Gets the ID of the resource that requires ownership validation.
    /// </summary>
    /// <returns>The resource ID.</returns>
    string GetResourceId();
}

/// <summary>
/// Marker interface for requests that should bypass authorization checks.
/// Use this sparingly and only for truly public operations.
/// </summary>
public interface IBypassAuthorization { }
