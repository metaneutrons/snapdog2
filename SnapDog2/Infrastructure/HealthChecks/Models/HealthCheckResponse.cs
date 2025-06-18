using System.Text.Json.Serialization;

namespace SnapDog2.Infrastructure.HealthChecks.Models;

/// <summary>
/// Model for structured health check responses.
/// Provides detailed information about component health status, response times, and diagnostics.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Gets or sets the name of the health check component.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the health status of the component.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the response time for the health check in milliseconds.
    /// </summary>
    [JsonPropertyName("responseTime")]
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// Gets or sets the description of the health check result.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets error details if the health check failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Gets or sets additional metadata and diagnostic information.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the health check was performed.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the tags associated with this health check.
    /// </summary>
    [JsonPropertyName("tags")]
    public IEnumerable<string>? Tags { get; init; }
}
