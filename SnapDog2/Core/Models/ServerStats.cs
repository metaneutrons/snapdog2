namespace SnapDog2.Core.Models;

/// <summary>
/// Represents server performance statistics.
/// </summary>
public record ServerStats
{
    /// <summary>
    /// Gets the UTC timestamp when the stats were recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the CPU usage percentage (0-100).
    /// </summary>
    public required double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in megabytes.
    /// </summary>
    public required double MemoryUsageMb { get; init; }

    /// <summary>
    /// Gets the total available memory in megabytes.
    /// </summary>
    public required double TotalMemoryMb { get; init; }

    /// <summary>
    /// Gets the application uptime.
    /// </summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>
    /// Gets the number of active connections.
    /// </summary>
    public int ActiveConnections { get; init; }

    /// <summary>
    /// Gets the number of processed requests.
    /// </summary>
    public long ProcessedRequests { get; init; }
}
