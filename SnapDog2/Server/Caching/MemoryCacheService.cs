using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SnapDog2.Server.Caching;

/// <summary>
/// In-memory implementation of the cache service using IMemoryCache.
/// Provides thread-safe caching with configurable expiration policies and statistics.
/// </summary>
public sealed class MemoryCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _keyTracker;
    private readonly Timer _cleanupTimer;
    private long _hitCount;
    private long _missCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
    /// </summary>
    /// <param name="memoryCache">The underlying memory cache.</param>
    /// <param name="options">The cache configuration options.</param>
    /// <param name="logger">The logger.</param>
    public MemoryCacheService(
        IMemoryCache memoryCache,
        IOptions<CacheOptions> options,
        ILogger<MemoryCacheService> logger
    )
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyTracker = new ConcurrentDictionary<string, DateTime>();

        // Setup cleanup timer for expired entries
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _options.CleanupInterval, _options.CleanupInterval);

        _logger.LogDebug("MemoryCacheService initialized with options: {@CacheOptions}", _options);
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(key);

        _logger.LogTrace("Getting cache entry for key: {CacheKey}", key);

        if (_memoryCache.TryGetValue(key, out var cachedValue))
        {
            if (_options.EnableStatistics)
            {
                Interlocked.Increment(ref _hitCount);
            }

            _logger.LogTrace("Cache hit for key: {CacheKey}", key);

            // Update access time for sliding expiration
            _keyTracker.TryUpdate(key, DateTime.UtcNow, _keyTracker.GetValueOrDefault(key, DateTime.UtcNow));

            return Task.FromResult(cachedValue as T);
        }

        if (_options.EnableStatistics)
        {
            Interlocked.Increment(ref _missCount);
        }

        _logger.LogTrace("Cache miss for key: {CacheKey}", key);
        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        return SetInternalAsync(key, value, expiration, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        return SetAsync(key, value, _options.DefaultExpiration, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(key);

        _logger.LogTrace("Removing cache entry for key: {CacheKey}", key);

        _memoryCache.Remove(key);
        _keyTracker.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        _logger.LogDebug("Removing cache entries matching pattern: {Pattern}", pattern);

        var keysToRemove = _keyTracker.Keys.Where(key => IsPatternMatch(key, pattern)).ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out _);
        }

        _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(key);

        var exists = _keyTracker.ContainsKey(key);
        _logger.LogTrace("Cache key existence check for {CacheKey}: {Exists}", key, exists);

        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        _logger.LogTrace("Cache miss for key: {CacheKey}, executing factory function", key);

        var value = await factory(cancellationToken);
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
            return value;
        }

        // Handle the case where factory returns null - this is expected behavior
        // for cache patterns where the factory might not produce a value
        return value!; // We know this is null, but return it as T for consistency
    }

    /// <inheritdoc />
    public Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return GetOrSetAsync(key, factory, _options.DefaultExpiration, cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Clearing all cache entries");

        // Clear all tracked keys from memory cache
        foreach (var key in _keyTracker.Keys.ToList())
        {
            _memoryCache.Remove(key);
        }

        _keyTracker.Clear();

        // Reset statistics
        if (_options.EnableStatistics)
        {
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public CacheStatistics GetStatistics()
    {
        ThrowIfDisposed();

        return new CacheStatistics
        {
            TotalEntries = _keyTracker.Count,
            HitCount = _hitCount,
            MissCount = _missCount,
            EstimatedMemoryUsage = EstimateMemoryUsage(),
        };
    }

    /// <summary>
    /// Sets a value in the cache with the specified options.
    /// </summary>
    private async Task SetInternalAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken
    )
        where T : class
    {
        _logger.LogTrace("Setting cache entry for key: {CacheKey} with expiration: {Expiration}", key, expiration);

        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            SlidingExpiration = _options.SlidingExpiration,
            Priority = CacheItemPriority.Normal,
        };

        // Add eviction callback to track removals
        entryOptions.RegisterPostEvictionCallback(
            (k, v, reason, state) =>
            {
                _keyTracker.TryRemove(k.ToString()!, out _);
                _logger.LogTrace("Cache entry evicted for key: {CacheKey}, reason: {Reason}", k, reason);
            }
        );

        _memoryCache.Set(key, value, entryOptions);
        _keyTracker.TryAdd(key, DateTime.UtcNow);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up expired cache entries.
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var expiredKeys = _keyTracker
                .Where(kvp => DateTime.UtcNow - kvp.Value > _options.DefaultExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _memoryCache.Remove(key);
                _keyTracker.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    /// <summary>
    /// Checks if a key matches a pattern (supports wildcard matching).
    /// </summary>
    private static bool IsPatternMatch(string key, string pattern)
    {
        if (pattern.EndsWith('*'))
        {
            var prefix = pattern[..^1];
            return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith('*'))
        {
            var suffix = pattern[1..];
            return key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(key, pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Estimates the memory usage of the cache.
    /// </summary>
    private long EstimateMemoryUsage()
    {
        // This is a rough estimation - in a real-world scenario,
        // you might want to implement more sophisticated memory tracking
        return _keyTracker.Count * 1024; // Assume 1KB per entry on average
    }

    /// <summary>
    /// Throws an exception if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MemoryCacheService));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _disposed = true;

            _logger.LogDebug("MemoryCacheService disposed");
        }
    }
}
