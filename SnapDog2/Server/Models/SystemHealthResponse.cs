using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnapDog2.Infrastructure.HealthChecks.Models;

namespace SnapDog2.Server.Models;

/// <summary>
/// Response model for system health information.
/// Provides detailed health check results and service status information.
/// </summary>
public sealed record SystemHealthResponse
{
    /// <summary>
    /// Gets the overall health status of the system.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the health check was performed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total duration of all health checks in milliseconds.
    /// </summary>
    public long TotalDurationMs { get; init; }

    /// <summary>
    /// Gets the individual health check results.
    /// </summary>
    public Dictionary<string, HealthCheckResult> Results { get; init; } = new();

    /// <summary>
    /// Gets the number of healthy components.
    /// </summary>
    public int HealthyCount { get; init; }

    /// <summary>
    /// Gets the number of unhealthy components.
    /// </summary>
    public int UnhealthyCount { get; init; }

    /// <summary>
    /// Gets the number of degraded components.
    /// </summary>
    public int DegradedCount { get; init; }

    /// <summary>
    /// Gets the total number of components checked.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets additional system information.
    /// </summary>
    public Dictionary<string, object> SystemInfo { get; init; } = new();

    /// <summary>
    /// Gets tags for filtering and categorization.
    /// </summary>
    public IEnumerable<string>? Tags { get; init; }

    /// <summary>
    /// Creates a SystemHealthResponse from SystemHealthStatus.
    /// </summary>
    /// <param name="healthStatus">The system health status.</param>
    /// <returns>A new SystemHealthResponse instance.</returns>
    public static SystemHealthResponse FromSystemHealthStatus(SystemHealthStatus healthStatus)
    {
        ArgumentNullException.ThrowIfNull(healthStatus);

        var results = new Dictionary<string, HealthCheckResult>();
        foreach (var kvp in healthStatus.Results)
        {
            results[kvp.Key] = new HealthCheckResult
            {
                Status = kvp.Value.Status,
                Description = kvp.Value.Description,
                DurationMs = kvp.Value.ResponseTimeMs,
                Exception = kvp.Value.Error,
                Data = kvp.Value.Data ?? new Dictionary<string, object>(),
            };
        }

        return new SystemHealthResponse
        {
            Status = healthStatus.Status.ToString(),
            Timestamp = healthStatus.Timestamp,
            TotalDurationMs = healthStatus.TotalDurationMs,
            Results = results,
            HealthyCount = healthStatus.HealthyCount,
            UnhealthyCount = healthStatus.UnhealthyCount,
            DegradedCount = healthStatus.DegradedCount,
            TotalCount = healthStatus.TotalCount,
            SystemInfo = healthStatus.SystemInfo,
            Tags = healthStatus.Tags,
        };
    }

    /// <summary>
    /// Creates a basic SystemHealthResponse with minimal information.
    /// </summary>
    /// <param name="status">The overall health status.</param>
    /// <param name="healthyCount">Number of healthy components.</param>
    /// <param name="totalCount">Total number of components.</param>
    /// <returns>A new basic SystemHealthResponse instance.</returns>
    public static SystemHealthResponse CreateBasic(HealthStatus status, int healthyCount, int totalCount)
    {
        return new SystemHealthResponse
        {
            Status = status.ToString(),
            HealthyCount = healthyCount,
            TotalCount = totalCount,
            UnhealthyCount = totalCount - healthyCount,
        };
    }

    /// <summary>
    /// Gets a human-readable summary of the health status.
    /// </summary>
    /// <returns>A formatted string describing the health state.</returns>
    public string GetHealthSummary()
    {
        var summary = $"Health Status: {Status}";
        summary += $" | {HealthyCount}/{TotalCount} components healthy";

        if (UnhealthyCount > 0)
        {
            summary += $" | {UnhealthyCount} unhealthy";
        }

        if (DegradedCount > 0)
        {
            summary += $" | {DegradedCount} degraded";
        }

        if (TotalDurationMs > 0)
        {
            summary += $" | Check duration: {TotalDurationMs}ms";
        }

        return summary;
    }

    /// <summary>
    /// Gets the names of unhealthy components.
    /// </summary>
    /// <returns>A collection of unhealthy component names.</returns>
    public IEnumerable<string> GetUnhealthyComponents()
    {
        return Results.Where(kvp => kvp.Value.Status == "Unhealthy").Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets the names of degraded components.
    /// </summary>
    /// <returns>A collection of degraded component names.</returns>
    public IEnumerable<string> GetDegradedComponents()
    {
        return Results.Where(kvp => kvp.Value.Status == "Degraded").Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Determines if the system is considered healthy.
    /// </summary>
    /// <returns>True if the system is healthy; otherwise, false.</returns>
    public bool IsHealthy()
    {
        return Status == HealthStatus.Healthy.ToString();
    }

    /// <summary>
    /// Gets detailed information about a specific health check.
    /// </summary>
    /// <param name="componentName">The name of the component to get details for.</param>
    /// <returns>The health check result if found; otherwise, null.</returns>
    public HealthCheckResult? GetComponentDetails(string componentName)
    {
        return Results.TryGetValue(componentName, out var result) ? result : null;
    }
}

/// <summary>
/// Detailed result of a specific health check.
/// </summary>
public sealed record HealthCheckResult
{
    /// <summary>
    /// Gets the health status of the component.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the description of the health check result.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the duration of the health check in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Gets the exception that occurred during the health check, if any.
    /// </summary>
    public string? Exception { get; init; }

    /// <summary>
    /// Gets additional data associated with the health check.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when this health check was performed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets tags associated with this health check.
    /// </summary>
    public IEnumerable<string>? Tags { get; init; }
}
