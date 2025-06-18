namespace SnapDog2.Server.Caching;

/// <summary>
/// Configuration options for caching services.
/// Defines cache behavior, expiration policies, and performance settings.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cached items.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the maximum number of entries in the cache.
    /// When exceeded, the least recently used items will be evicted.
    /// </summary>
    public int MaxEntries { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum memory usage for the cache in bytes.
    /// When exceeded, items will be evicted to free memory.
    /// </summary>
    public long MaxMemoryUsage { get; set; } = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Gets or sets the sliding expiration time for cached items.
    /// Items will be kept alive as long as they are accessed within this timeframe.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the interval for cleaning up expired cache entries.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets whether to enable cache statistics collection.
    /// Enabling this may have a small performance impact but provides valuable metrics.
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets the compression threshold in bytes.
    /// Items larger than this size will be compressed before caching.
    /// Set to 0 to disable compression.
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024; // 1 KB

    /// <summary>
    /// Gets or sets cache key prefixes for different types of cached data.
    /// This helps organize cache keys and enables bulk operations.
    /// </summary>
    public CacheKeyPrefixes KeyPrefixes { get; set; } = new();

    /// <summary>
    /// Gets or sets specific expiration times for different types of cached data.
    /// </summary>
    public CacheExpirationPolicies ExpirationPolicies { get; set; } = new();
}

/// <summary>
/// Defines cache key prefixes for different types of data.
/// </summary>
public sealed class CacheKeyPrefixes
{
    /// <summary>
    /// Gets or sets the prefix for audio stream queries.
    /// </summary>
    public string AudioStreams { get; set; } = "audiostreams";

    /// <summary>
    /// Gets or sets the prefix for system status queries.
    /// </summary>
    public string SystemStatus { get; set; } = "systemstatus";

    /// <summary>
    /// Gets or sets the prefix for client queries.
    /// </summary>
    public string Clients { get; set; } = "clients";

    /// <summary>
    /// Gets or sets the prefix for zone queries.
    /// </summary>
    public string Zones { get; set; } = "zones";

    /// <summary>
    /// Gets or sets the prefix for playlist queries.
    /// </summary>
    public string Playlists { get; set; } = "playlists";

    /// <summary>
    /// Gets or sets the prefix for radio station queries.
    /// </summary>
    public string RadioStations { get; set; } = "radiostations";
}

/// <summary>
/// Defines specific expiration policies for different types of cached data.
/// </summary>
public sealed class CacheExpirationPolicies
{
    /// <summary>
    /// Gets or sets the expiration time for audio stream data.
    /// Audio streams change infrequently, so can be cached longer.
    /// </summary>
    public TimeSpan AudioStreams { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the expiration time for system status data.
    /// System status changes frequently, so should be cached for shorter periods.
    /// </summary>
    public TimeSpan SystemStatus { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the expiration time for client data.
    /// Client information changes moderately, so medium cache duration.
    /// </summary>
    public TimeSpan Clients { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the expiration time for zone configuration data.
    /// Zone configurations change infrequently, so can be cached longer.
    /// </summary>
    public TimeSpan Zones { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the expiration time for playlist data.
    /// Playlists change moderately, so medium cache duration.
    /// </summary>
    public TimeSpan Playlists { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the expiration time for radio station data.
    /// Radio stations change infrequently, so can be cached longer.
    /// </summary>
    public TimeSpan RadioStations { get; set; } = TimeSpan.FromHours(2);
}
