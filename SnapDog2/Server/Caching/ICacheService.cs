namespace SnapDog2.Server.Caching;

/// <summary>
/// Defines the contract for caching services in the application.
/// Provides methods for storing, retrieving, and managing cached data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached value if found; otherwise, null.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sets a value in the cache with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The expiration time for the cached value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sets a value in the cache with the specified key and default expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values that match the specified pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys against (supports wildcards).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a cached value using a factory function.
    /// If the key doesn't exist, the factory function is called to generate the value.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory function to generate the value if not cached.</param>
    /// <param name="expiration">The expiration time for the cached value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached or newly generated value.</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Gets or sets a cached value using a factory function with default expiration.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory function to generate the value if not cached.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached or newly generated value.</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the cache usage.
    /// </summary>
    /// <returns>Cache statistics information.</returns>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Represents cache usage statistics.
/// </summary>
public sealed record CacheStatistics
{
    /// <summary>
    /// Gets the total number of cache entries.
    /// </summary>
    public int TotalEntries { get; init; }

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long HitCount { get; init; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long MissCount { get; init; }

    /// <summary>
    /// Gets the cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;

    /// <summary>
    /// Gets the estimated memory usage in bytes.
    /// </summary>
    public long EstimatedMemoryUsage { get; init; }
}
