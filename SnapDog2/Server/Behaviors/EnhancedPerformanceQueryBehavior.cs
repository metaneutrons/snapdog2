namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Application;

/// <summary>
/// Enhanced pipeline behavior that measures query execution performance and records metrics.
/// Replaces the basic PerformanceQueryBehavior with enterprise-grade metrics collection.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class EnhancedPerformanceQueryBehavior<TQuery, TResponse>(
    ILogger<EnhancedPerformanceQueryBehavior<TQuery, TResponse>> logger,
    EnterpriseMetricsService metricsService
) : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<EnhancedPerformanceQueryBehavior<TQuery, TResponse>> _logger = logger;
    private readonly EnterpriseMetricsService _metricsService = metricsService;
    private const int SlowOperationThresholdMs = 200; // Queries should be faster than commands

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var queryName = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            success = response.IsSuccess;
            var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;

            // Record metrics
            _metricsService.RecordCortexMediatorRequestDuration(
                "Query",
                queryName,
                stopwatch.ElapsedMilliseconds,
                success
            );

            // Log slow operations
            if (stopwatch.ElapsedMilliseconds > SlowOperationThresholdMs)
            {
                this.LogSlowQuery(queryName, stopwatch.ElapsedMilliseconds);
            }

            // Record query-specific metrics
            RecordQuerySpecificMetrics(query, response, durationSeconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            success = false;

            // Record error metrics
            _metricsService.RecordCortexMediatorRequestDuration(
                "Query",
                queryName,
                stopwatch.ElapsedMilliseconds,
                false
            );
            _metricsService.RecordException(ex, "QueryPipeline", queryName);

            this.LogQueryException(queryName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    /// <summary>
    /// Records query-specific metrics based on the query type.
    /// </summary>
    private void RecordQuerySpecificMetrics(TQuery query, TResponse response, double durationSeconds)
    {
        try
        {
            var queryName = typeof(TQuery).Name;

            // Track business metrics for specific queries
            if (
                queryName.Contains("ZoneState", StringComparison.OrdinalIgnoreCase)
                || queryName.Contains("GetAllZones", StringComparison.OrdinalIgnoreCase)
            )
            {
                // These queries provide zone information that could be used for business metrics
                // In a full implementation, you'd extract the actual data from the response
                UpdateBusinessMetricsFromZoneQuery(response);
            }

            if (queryName.Contains("Client", StringComparison.OrdinalIgnoreCase))
            {
                // Client queries could provide client connection information
                UpdateBusinessMetricsFromClientQuery(response);
            }
        }
        catch (Exception ex)
        {
            // Don't let metrics recording failures affect query execution
            _logger.LogDebug(ex, "Failed to record query-specific metrics for {QueryName}", typeof(TQuery).Name);
        }
    }

    /// <summary>
    /// Updates business metrics based on zone query responses.
    /// </summary>
    private void UpdateBusinessMetricsFromZoneQuery(TResponse response)
    {
        try
        {
            // In a full implementation, you would:
            // 1. Extract zone data from the response
            // 2. Count active zones, playing tracks, etc.
            // 3. Update business metrics accordingly

            // For now, this is a placeholder that would be implemented based on your specific response types
            // Example:
            // if (response.Value is ZoneState zoneState)
            // {
            //     var businessMetrics = new BusinessMetricsState
            //     {
            //         ZonesActive = zoneState.IsActive ? 1 : 0,
            //         TracksPlaying = zoneState.IsPlaying ? 1 : 0
            //     };
            //     _metricsService.UpdateBusinessMetrics(businessMetrics);
            // }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to update business metrics from zone query");
        }
    }

    /// <summary>
    /// Updates business metrics based on client query responses.
    /// </summary>
    private void UpdateBusinessMetricsFromClientQuery(TResponse response)
    {
        try
        {
            // Similar to zone queries, this would extract client connection information
            // and update business metrics accordingly
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to update business metrics from client query");
        }
    }

    [LoggerMessage(2301, LogLevel.Warning, "Slow query detected: {QueryName} took {ElapsedMilliseconds}ms")]
    private partial void LogSlowQuery(string queryName, long elapsedMilliseconds);

    [LoggerMessage(2302, LogLevel.Error, "Query {QueryName} threw exception after {ElapsedMilliseconds}ms")]
    private partial void LogQueryException(string queryName, long elapsedMilliseconds, Exception ex);
}
