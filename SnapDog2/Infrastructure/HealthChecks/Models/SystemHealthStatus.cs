using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SnapDog2.Infrastructure.HealthChecks.Models;

/// <summary>
/// Aggregate model for overall system health status.
/// Provides a comprehensive view of all system components and their health status.
/// </summary>
public class SystemHealthStatus
{
    /// <summary>
    /// Gets or sets the overall system health status.
    /// </summary>
    [JsonPropertyName("status")]
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the total duration of all health checks in milliseconds.
    /// </summary>
    [JsonPropertyName("totalDuration")]
    public long TotalDurationMs { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the health check was performed.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the individual health check results.
    /// </summary>
    [JsonPropertyName("results")]
    public Dictionary<string, HealthCheckResponse> Results { get; init; } = new();

    /// <summary>
    /// Gets or sets system-wide metrics and information.
    /// </summary>
    [JsonPropertyName("systemInfo")]
    public Dictionary<string, object> SystemInfo { get; init; } = new();

    /// <summary>
    /// Gets the number of healthy components.
    /// </summary>
    [JsonPropertyName("healthyCount")]
    public int HealthyCount => Results.Values.Count(static r => r.Status == "Healthy");

    /// <summary>
    /// Gets the number of unhealthy components.
    /// </summary>
    [JsonPropertyName("unhealthyCount")]
    public int UnhealthyCount => Results.Values.Count(static r => r.Status == "Unhealthy");

    /// <summary>
    /// Gets the number of degraded components.
    /// </summary>
    [JsonPropertyName("degradedCount")]
    public int DegradedCount => Results.Values.Count(static r => r.Status == "Degraded");

    /// <summary>
    /// Gets the total number of components checked.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount => Results.Count;

    /// <summary>
    /// Gets or sets additional tags for filtering and categorization.
    /// </summary>
    [JsonPropertyName("tags")]
    public IEnumerable<string>? Tags { get; init; }
}
