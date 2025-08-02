namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Service for recording application metrics.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records the duration of a Cortex.Mediator request.
    /// </summary>
    /// <param name="requestType">The type of request (Command/Query).</param>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the request was successful.</param>
    void RecordCortexMediatorRequestDuration(string requestType, string requestName, long durationMs, bool success);

    /// <summary>
    /// Gets the current server performance statistics.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the server statistics.</returns>
    Task<ServerStats> GetServerStatsAsync();

    // TODO: Add other metric recording methods as needed
}
