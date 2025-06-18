namespace SnapDog2.Server.Monitoring;

/// <summary>
/// Defines the contract for performance monitoring services.
/// Provides methods for tracking request performance metrics and detecting anomalies.
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Records the execution time for a request.
    /// </summary>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="executionTime">The execution time in milliseconds.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordExecutionTimeAsync(
        string requestName,
        long executionTime,
        bool success,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets performance metrics for a specific request type.
    /// </summary>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Performance metrics for the request type.</returns>
    Task<PerformanceMetrics?> GetMetricsAsync(string requestName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for all request types.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of performance metrics for all request types.</returns>
    Task<IEnumerable<PerformanceMetrics>> GetAllMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a request execution time is considered slow.
    /// </summary>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="executionTime">The execution time in milliseconds.</param>
    /// <returns>True if the request is considered slow; otherwise, false.</returns>
    bool IsSlowRequest(string requestName, long executionTime);

    /// <summary>
    /// Gets the threshold for slow requests of a specific type.
    /// </summary>
    /// <param name="requestName">The name of the request.</param>
    /// <returns>The slow request threshold in milliseconds.</returns>
    long GetSlowRequestThreshold(string requestName);

    /// <summary>
    /// Records a slow request alert.
    /// </summary>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="executionTime">The execution time in milliseconds.</param>
    /// <param name="threshold">The threshold that was exceeded.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordSlowRequestAsync(
        string requestName,
        long executionTime,
        long threshold,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets recent slow request alerts.
    /// </summary>
    /// <param name="timeWindow">The time window to look back for alerts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of recent slow request alerts.</returns>
    Task<IEnumerable<SlowRequestAlert>> GetRecentSlowRequestsAsync(
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Clears performance metrics older than the specified age.
    /// </summary>
    /// <param name="maxAge">The maximum age of metrics to keep.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CleanupOldMetricsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overall system performance statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Overall system performance statistics.</returns>
    Task<SystemPerformanceStats> GetSystemStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents performance metrics for a specific request type.
/// </summary>
public sealed record PerformanceMetrics
{
    /// <summary>
    /// Gets the name of the request type.
    /// </summary>
    public required string RequestName { get; init; }

    /// <summary>
    /// Gets the total number of requests executed.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public long SuccessfulRequests { get; init; }

    /// <summary>
    /// Gets the number of failed requests.
    /// </summary>
    public long FailedRequests { get; init; }

    /// <summary>
    /// Gets the average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTime { get; init; }

    /// <summary>
    /// Gets the minimum execution time in milliseconds.
    /// </summary>
    public long MinExecutionTime { get; init; }

    /// <summary>
    /// Gets the maximum execution time in milliseconds.
    /// </summary>
    public long MaxExecutionTime { get; init; }

    /// <summary>
    /// Gets the 95th percentile execution time in milliseconds.
    /// </summary>
    public long P95ExecutionTime { get; init; }

    /// <summary>
    /// Gets the 99th percentile execution time in milliseconds.
    /// </summary>
    public long P99ExecutionTime { get; init; }

    /// <summary>
    /// Gets the number of slow requests.
    /// </summary>
    public long SlowRequests { get; init; }

    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0.0;

    /// <summary>
    /// Gets the slow request rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double SlowRequestRate => TotalRequests > 0 ? (double)SlowRequests / TotalRequests : 0.0;

    /// <summary>
    /// Gets the timestamp of the first recorded request.
    /// </summary>
    public DateTime FirstRequestAt { get; init; }

    /// <summary>
    /// Gets the timestamp of the last recorded request.
    /// </summary>
    public DateTime LastRequestAt { get; init; }
}

/// <summary>
/// Represents a slow request alert.
/// </summary>
public sealed record SlowRequestAlert
{
    /// <summary>
    /// Gets the name of the request that was slow.
    /// </summary>
    public required string RequestName { get; init; }

    /// <summary>
    /// Gets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTime { get; init; }

    /// <summary>
    /// Gets the threshold that was exceeded.
    /// </summary>
    public long Threshold { get; init; }

    /// <summary>
    /// Gets the timestamp when the slow request occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets how much the execution time exceeded the threshold.
    /// </summary>
    public long ExcessTime => ExecutionTime - Threshold;

    /// <summary>
    /// Gets the multiplier by which the threshold was exceeded.
    /// </summary>
    public double ThresholdMultiplier => Threshold > 0 ? (double)ExecutionTime / Threshold : 0.0;
}

/// <summary>
/// Represents overall system performance statistics.
/// </summary>
public sealed record SystemPerformanceStats
{
    /// <summary>
    /// Gets the total number of requests across all types.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the total number of successful requests.
    /// </summary>
    public long SuccessfulRequests { get; init; }

    /// <summary>
    /// Gets the overall system success rate.
    /// </summary>
    public double OverallSuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0.0;

    /// <summary>
    /// Gets the average execution time across all requests.
    /// </summary>
    public double AverageExecutionTime { get; init; }

    /// <summary>
    /// Gets the number of request types being monitored.
    /// </summary>
    public int MonitoredRequestTypes { get; init; }

    /// <summary>
    /// Gets the total number of slow requests.
    /// </summary>
    public long TotalSlowRequests { get; init; }

    /// <summary>
    /// Gets the overall slow request rate.
    /// </summary>
    public double OverallSlowRequestRate => TotalRequests > 0 ? (double)TotalSlowRequests / TotalRequests : 0.0;

    /// <summary>
    /// Gets the timestamp when statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Gets the time window covered by these statistics.
    /// </summary>
    public TimeSpan TimeWindow { get; init; }
}
