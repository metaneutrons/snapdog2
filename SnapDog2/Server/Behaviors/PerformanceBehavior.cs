using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Server.Monitoring;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that monitors request performance and tracks execution metrics.
/// Records execution times, identifies slow requests, and provides performance alerting.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="performanceMonitor">The performance monitor.</param>
    /// <param name="logger">The logger.</param>
    public PerformanceBehavior(
        IPerformanceMonitor performanceMonitor,
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger
    )
    {
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the performance monitoring pipeline.
    /// </summary>
    /// <param name="request">The request to monitor.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response with performance monitoring applied.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        TResponse response;
        bool success = false;
        Exception? exception = null;

        try
        {
            _logger.LogTrace("Starting performance monitoring for request: {RequestName}", requestName);

            response = await next();
            success = DetermineSuccess(response);

            _logger.LogTrace(
                "Completed performance monitoring for request: {RequestName} (Success: {Success})",
                requestName,
                success
            );

            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            success = false;

            _logger.LogTrace(
                ex,
                "Exception occurred during performance monitoring for request: {RequestName}",
                requestName
            );

            throw;
        }
        finally
        {
            stopwatch.Stop();
            var executionTime = stopwatch.ElapsedMilliseconds;

            // Record the performance metrics
            try
            {
                await _performanceMonitor.RecordExecutionTimeAsync(
                    requestName,
                    executionTime,
                    success,
                    cancellationToken
                );

                // Log performance information
                LogPerformanceResult(requestName, executionTime, success, exception);
            }
            catch (Exception monitorException)
            {
                _logger.LogError(
                    monitorException,
                    "Failed to record performance metrics for request: {RequestName}",
                    requestName
                );
            }
        }
    }

    /// <summary>
    /// Determines if the response indicates a successful operation.
    /// </summary>
    /// <param name="response">The response to evaluate.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    private static bool DetermineSuccess(TResponse response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _
                when typeof(TResponse).IsGenericType
                    && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>) => (bool)(
                typeof(TResponse).GetProperty("IsSuccess")?.GetValue(response) ?? false
            ),
            null => false,
            _ => true, // Non-Result types are considered successful if they return a value
        };
    }

    /// <summary>
    /// Logs performance results with appropriate log levels based on execution time and outcome.
    /// </summary>
    /// <param name="requestName">The name of the request.</param>
    /// <param name="executionTime">The execution time in milliseconds.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="exception">Any exception that occurred.</param>
    private void LogPerformanceResult(string requestName, long executionTime, bool success, Exception? exception)
    {
        var isSlowRequest = _performanceMonitor.IsSlowRequest(requestName, executionTime);
        var threshold = _performanceMonitor.GetSlowRequestThreshold(requestName);

        if (exception != null)
        {
            _logger.LogError(
                "Performance: {RequestName} failed after {ExecutionTime}ms with exception",
                requestName,
                executionTime
            );
        }
        else if (isSlowRequest)
        {
            _logger.LogWarning(
                "Performance: {RequestName} completed in {ExecutionTime}ms (threshold: {Threshold}ms) - SLOW REQUEST",
                requestName,
                executionTime,
                threshold
            );
        }
        else if (!success)
        {
            _logger.LogWarning(
                "Performance: {RequestName} completed unsuccessfully in {ExecutionTime}ms",
                requestName,
                executionTime
            );
        }
        else
        {
            var logLevel = GetLogLevelForExecutionTime(executionTime);
            _logger.Log(
                logLevel,
                "Performance: {RequestName} completed successfully in {ExecutionTime}ms",
                requestName,
                executionTime
            );
        }
    }

    /// <summary>
    /// Gets the appropriate log level based on execution time.
    /// </summary>
    /// <param name="executionTime">The execution time in milliseconds.</param>
    /// <returns>The appropriate log level.</returns>
    private static LogLevel GetLogLevelForExecutionTime(long executionTime)
    {
        return executionTime switch
        {
            >= 5000 => LogLevel.Warning, // 5+ seconds
            >= 2000 => LogLevel.Information, // 2-5 seconds
            >= 1000 => LogLevel.Debug, // 1-2 seconds
            _ => LogLevel.Trace, // < 1 second
        };
    }
}

/// <summary>
/// Marker interface for requests that should have custom performance monitoring behavior.
/// Implement this interface to provide custom performance thresholds or monitoring logic.
/// </summary>
public interface IPerformanceMonitored
{
    /// <summary>
    /// Gets the custom performance threshold for this request in milliseconds.
    /// </summary>
    /// <returns>The performance threshold, or null to use the default threshold.</returns>
    long? GetPerformanceThreshold() => null;

    /// <summary>
    /// Gets whether this request should be excluded from slow request alerting.
    /// </summary>
    /// <returns>True to exclude from slow request alerts; otherwise, false.</returns>
    bool ExcludeFromSlowRequestAlerts() => false;
}

/// <summary>
/// Marker interface for requests that should not be performance monitored.
/// Implement this interface on requests to exclude them from performance tracking.
/// </summary>
public interface IExcludeFromPerformanceMonitoring { }
