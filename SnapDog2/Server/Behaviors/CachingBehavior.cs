using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Server.Caching;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that provides caching for query operations.
/// Caches successful query results and returns cached data when available.
/// Only applies caching to read operations (queries), not commands.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="logger">The logger.</param>
    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the caching pipeline for eligible requests.
    /// </summary>
    /// <param name="request">The request to potentially cache.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response, either from cache or freshly executed.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;

        // Only cache query operations, not commands
        if (!IsQueryRequest(requestName))
        {
            _logger.LogTrace("Skipping cache for command: {RequestName}", requestName);
            return await next();
        }

        // Generate cache key for the request
        var cacheKey = CacheKeyGenerator.GenerateKey(request);
        _logger.LogTrace("Generated cache key for {RequestName}: {CacheKey}", requestName, cacheKey);

        // Try to get cached response
        if (typeof(TResponse) == typeof(Result) || IsGenericResult(typeof(TResponse)))
        {
            var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null && IsSuccessfulResult(cachedResponse))
            {
                _logger.LogDebug("Cache hit for {RequestName} with key: {CacheKey}", requestName, cacheKey);
                return cachedResponse;
            }
        }

        _logger.LogTrace("Cache miss for {RequestName} with key: {CacheKey}", requestName, cacheKey);

        // Execute the request
        var response = await next();

        // Cache successful responses
        if (ShouldCacheResponse(response))
        {
            var cacheDuration = GetCacheDuration(requestName);
            await _cacheService.SetAsync(cacheKey, response, cacheDuration, cancellationToken);

            _logger.LogTrace(
                "Cached response for {RequestName} with key: {CacheKey} for {Duration}",
                requestName,
                cacheKey,
                cacheDuration
            );
        }
        else
        {
            _logger.LogTrace("Response not cached for {RequestName} - not successful", requestName);
        }

        return response;
    }

    /// <summary>
    /// Determines if the request is a query operation that should be cached.
    /// </summary>
    /// <param name="requestName">The request name.</param>
    /// <returns>True if the request should be cached; otherwise, false.</returns>
    private static bool IsQueryRequest(string requestName)
    {
        return requestName.Contains("Query", StringComparison.OrdinalIgnoreCase)
            || requestName.Contains("Get", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Find", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Search", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("List", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the response type is a generic Result&lt;T&gt;.
    /// </summary>
    /// <param name="responseType">The response type to check.</param>
    /// <returns>True if it's a generic Result type; otherwise, false.</returns>
    private static bool IsGenericResult(Type responseType)
    {
        return responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>);
    }

    /// <summary>
    /// Checks if a Result response indicates success.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <returns>True if the response is successful; otherwise, false.</returns>
    private static bool IsSuccessfulResult(object response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _ when IsGenericResult(response.GetType()) => (bool)(
                response.GetType().GetProperty("IsSuccess")?.GetValue(response) ?? false
            ),
            _ => true, // Non-Result types are considered successful if they exist
        };
    }

    /// <summary>
    /// Determines if a response should be cached.
    /// </summary>
    /// <param name="response">The response to evaluate.</param>
    /// <returns>True if the response should be cached; otherwise, false.</returns>
    private static bool ShouldCacheResponse(TResponse response)
    {
        if (response == null)
        {
            return false;
        }

        return IsSuccessfulResult(response);
    }

    /// <summary>
    /// Gets the appropriate cache duration for a request type.
    /// </summary>
    /// <param name="requestName">The request name.</param>
    /// <returns>The cache duration for the request type.</returns>
    private static TimeSpan GetCacheDuration(string requestName)
    {
        // Map request types to specific cache durations
        return requestName.ToLowerInvariant() switch
        {
            var name when name.Contains("systemstatus") => TimeSpan.FromMinutes(2),
            var name when name.Contains("audiostream") => TimeSpan.FromMinutes(30),
            var name when name.Contains("client") => TimeSpan.FromMinutes(10),
            var name when name.Contains("zone") => TimeSpan.FromHours(1),
            var name when name.Contains("playlist") => TimeSpan.FromMinutes(15),
            var name when name.Contains("radiostation") => TimeSpan.FromHours(2),
            _ => DefaultCacheDuration,
        };
    }
}

/// <summary>
/// Marker interface for requests that should not be cached.
/// Implement this interface on requests to explicitly exclude them from caching.
/// </summary>
public interface INonCacheable { }

/// <summary>
/// Marker interface for requests that require custom cache behavior.
/// Implement this interface to provide custom cache keys or durations.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Gets the custom cache key for this request.
    /// </summary>
    /// <returns>A custom cache key, or null to use the default generation.</returns>
    string? GetCacheKey() => null;

    /// <summary>
    /// Gets the cache duration for this request.
    /// </summary>
    /// <returns>The cache duration, or null to use the default duration.</returns>
    TimeSpan? GetCacheDuration() => null;
}
