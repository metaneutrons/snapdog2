using SnapDog2.Core.State;
using SnapDog2.Infrastructure.HealthChecks.Models;

namespace SnapDog2.Server.Models;

/// <summary>
/// Response model for system status information.
/// Provides comprehensive overview of the system's operational state and health.
/// </summary>
public sealed record SystemStatusResponse
{
    /// <summary>
    /// Gets the current system status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the status was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the system uptime information.
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// Gets the system version information.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the audio stream statistics.
    /// </summary>
    public AudioStreamStatistics? StreamStatistics { get; init; }

    /// <summary>
    /// Gets the health check results.
    /// </summary>
    public SystemHealthStatus? HealthStatus { get; init; }

    /// <summary>
    /// Gets the performance metrics.
    /// </summary>
    public PerformanceMetrics? Performance { get; init; }

    /// <summary>
    /// Gets the client connection information.
    /// </summary>
    public ClientConnectionInfo? ClientInfo { get; init; }

    /// <summary>
    /// Gets the system error information.
    /// </summary>
    public SystemErrorInfo? ErrorInfo { get; init; }

    /// <summary>
    /// Gets additional system metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a SystemStatusResponse from the current system state.
    /// </summary>
    /// <param name="systemStatus">The current system status.</param>
    /// <param name="uptime">The system uptime.</param>
    /// <param name="version">The system version.</param>
    /// <returns>A new SystemStatusResponse instance.</returns>
    public static SystemStatusResponse FromSystemState(
        SystemStatus systemStatus,
        TimeSpan uptime,
        string version = "1.0.0"
    )
    {
        return new SystemStatusResponse
        {
            Status = systemStatus.ToString(),
            Timestamp = DateTime.UtcNow,
            Uptime = uptime,
            Version = version,
        };
    }

    /// <summary>
    /// Creates a comprehensive SystemStatusResponse with all information.
    /// </summary>
    /// <param name="systemStatus">The current system status.</param>
    /// <param name="uptime">The system uptime.</param>
    /// <param name="version">The system version.</param>
    /// <param name="streamStats">Audio stream statistics.</param>
    /// <param name="healthStatus">Health check results.</param>
    /// <param name="performance">Performance metrics.</param>
    /// <param name="clientInfo">Client connection information.</param>
    /// <param name="errorInfo">System error information.</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>A new comprehensive SystemStatusResponse instance.</returns>
    public static SystemStatusResponse CreateDetailed(
        SystemStatus systemStatus,
        TimeSpan uptime,
        string version,
        AudioStreamStatistics? streamStats = null,
        SystemHealthStatus? healthStatus = null,
        PerformanceMetrics? performance = null,
        ClientConnectionInfo? clientInfo = null,
        SystemErrorInfo? errorInfo = null,
        Dictionary<string, object>? metadata = null
    )
    {
        return new SystemStatusResponse
        {
            Status = systemStatus.ToString(),
            Timestamp = DateTime.UtcNow,
            Uptime = uptime,
            Version = version,
            StreamStatistics = streamStats,
            HealthStatus = healthStatus,
            Performance = performance,
            ClientInfo = clientInfo,
            ErrorInfo = errorInfo,
            Metadata = metadata ?? new Dictionary<string, object>(),
        };
    }

    /// <summary>
    /// Gets a human-readable summary of the system status.
    /// </summary>
    /// <returns>A formatted string describing the current system state.</returns>
    public string GetStatusSummary()
    {
        var summary = $"System Status: {Status}";

        if (StreamStatistics != null)
        {
            summary += $" | Streams: {StreamStatistics.ActiveCount}/{StreamStatistics.TotalCount} active";
        }

        if (HealthStatus != null)
        {
            summary += $" | Health: {HealthStatus.HealthyCount}/{HealthStatus.TotalCount} healthy";
        }

        if (Performance != null)
        {
            summary += $" | CPU: {Performance.CpuUsagePercent:F1}% | Memory: {Performance.MemoryUsageMb:F0}MB";
        }

        return summary;
    }

    /// <summary>
    /// Determines if the system is considered healthy.
    /// </summary>
    /// <returns>True if the system is healthy; otherwise, false.</returns>
    public bool IsHealthy()
    {
        if (Status == SystemStatus.Error.ToString())
        {
            return false;
        }

        if (HealthStatus?.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Statistics about audio streams in the system.
/// </summary>
public sealed record AudioStreamStatistics
{
    /// <summary>
    /// Gets the total number of configured streams.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of currently active streams.
    /// </summary>
    public int ActiveCount { get; init; }

    /// <summary>
    /// Gets the number of stopped streams.
    /// </summary>
    public int StoppedCount { get; init; }

    /// <summary>
    /// Gets the number of streams in error state.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the breakdown by codec type.
    /// </summary>
    public Dictionary<string, int> CodecBreakdown { get; init; } = new();

    /// <summary>
    /// Gets the breakdown by bitrate ranges.
    /// </summary>
    public Dictionary<string, int> BitrateBreakdown { get; init; } = new();

    /// <summary>
    /// Gets the most recently created stream timestamp.
    /// </summary>
    public DateTime? LastStreamCreated { get; init; }

    /// <summary>
    /// Gets the most recently started stream timestamp.
    /// </summary>
    public DateTime? LastStreamStarted { get; init; }
}

/// <summary>
/// System performance metrics.
/// </summary>
public sealed record PerformanceMetrics
{
    /// <summary>
    /// Gets the CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in megabytes.
    /// </summary>
    public double MemoryUsageMb { get; init; }

    /// <summary>
    /// Gets the disk usage percentage.
    /// </summary>
    public double DiskUsagePercent { get; init; }

    /// <summary>
    /// Gets the network throughput in bytes per second.
    /// </summary>
    public long NetworkThroughputBps { get; init; }

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the number of active connections.
    /// </summary>
    public int ActiveConnections { get; init; }

    /// <summary>
    /// Gets additional performance counters.
    /// </summary>
    public Dictionary<string, double> Counters { get; init; } = new();
}

/// <summary>
/// Client connection information.
/// </summary>
public sealed record ClientConnectionInfo
{
    /// <summary>
    /// Gets the total number of connected clients.
    /// </summary>
    public int TotalClients { get; init; }

    /// <summary>
    /// Gets the number of active clients.
    /// </summary>
    public int ActiveClients { get; init; }

    /// <summary>
    /// Gets the number of configured zones.
    /// </summary>
    public int TotalZones { get; init; }

    /// <summary>
    /// Gets the number of active zones.
    /// </summary>
    public int ActiveZones { get; init; }

    /// <summary>
    /// Gets the breakdown by client platform.
    /// </summary>
    public Dictionary<string, int> PlatformBreakdown { get; init; } = new();

    /// <summary>
    /// Gets the most recently connected client timestamp.
    /// </summary>
    public DateTime? LastClientConnected { get; init; }

    /// <summary>
    /// Gets the average connection duration.
    /// </summary>
    public TimeSpan? AverageConnectionDuration { get; init; }
}

/// <summary>
/// System error information.
/// </summary>
public sealed record SystemErrorInfo
{
    /// <summary>
    /// Gets the total number of errors in the time window.
    /// </summary>
    public int TotalErrors { get; init; }

    /// <summary>
    /// Gets the number of critical errors.
    /// </summary>
    public int CriticalErrors { get; init; }

    /// <summary>
    /// Gets the number of warnings.
    /// </summary>
    public int Warnings { get; init; }

    /// <summary>
    /// Gets the most recent error timestamp.
    /// </summary>
    public DateTime? LastErrorTimestamp { get; init; }

    /// <summary>
    /// Gets the most recent error message.
    /// </summary>
    public string? LastErrorMessage { get; init; }

    /// <summary>
    /// Gets the breakdown by error category.
    /// </summary>
    public Dictionary<string, int> ErrorCategories { get; init; } = new();

    /// <summary>
    /// Gets recent error details.
    /// </summary>
    public List<ErrorDetail> RecentErrors { get; init; } = new();
}

/// <summary>
/// Details about a specific error.
/// </summary>
public sealed record ErrorDetail
{
    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the error level (Error, Warning, etc.).
    /// </summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error category.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source component that generated the error.
    /// </summary>
    public string Source { get; init; } = string.Empty;
}
