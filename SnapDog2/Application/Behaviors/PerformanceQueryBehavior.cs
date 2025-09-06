//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Application.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Queries;
using SnapDog2.Domain.Services;
using SnapDog2.Shared.Models;

/// <summary>
/// Enhanced pipeline behavior that measures query execution performance and records metrics.
/// Replaces the basic PerformanceQueryBehavior with metrics collection.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class PerformanceQueryBehavior<TQuery, TResponse>(
    ILogger<PerformanceQueryBehavior<TQuery, TResponse>> logger,
    EnterpriseMetricsService metricsService
) : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<PerformanceQueryBehavior<TQuery, TResponse>> _logger = logger;
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
        bool success;

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            success = response.IsSuccess;
            var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;

            // Record metrics
            this._metricsService.RecordCortexMediatorRequestDuration(
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
            this.RecordQuerySpecificMetrics(query, response, durationSeconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            this._metricsService.RecordCortexMediatorRequestDuration(
                "Query",
                queryName,
                stopwatch.ElapsedMilliseconds,
                false
            );
            this._metricsService.RecordException(ex, "QueryPipeline", queryName);

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
                this.UpdateBusinessMetricsFromZoneQuery(response);
            }

            if (queryName.Contains("Client", StringComparison.OrdinalIgnoreCase))
            {
                // Client queries could provide client connection information
                this.UpdateBusinessMetricsFromClientQuery(response);
            }
        }
        catch (Exception ex)
        {
            // Don't let metrics recording failures affect query execution
            this.LogQueryMetricsFailure(typeof(TQuery).Name, ex);
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
            this.LogZoneMetricsFailure(ex);
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
            this.LogClientMetricsFailure(ex);
        }
    }

    [LoggerMessage(EventId = 118100, Level = LogLevel.Warning, Message = "Slow query detected: {QueryName} took {ElapsedMilliseconds}ms"
)]
    private partial void LogSlowQuery(string queryName, long elapsedMilliseconds);

    [LoggerMessage(EventId = 118101, Level = LogLevel.Error, Message = "Query {QueryName} threw exception after {ElapsedMilliseconds}ms"
)]
    private partial void LogQueryException(string queryName, long elapsedMilliseconds, Exception ex);

    [LoggerMessage(EventId = 118102, Level = LogLevel.Debug, Message = "Failed → record query-specific metrics for {QueryName}"
)]
    private partial void LogQueryMetricsFailure(string queryName, Exception ex);

    [LoggerMessage(EventId = 118103, Level = LogLevel.Debug, Message = "Failed → update business metrics from zone query"
)]
    private partial void LogZoneMetricsFailure(Exception ex);

    [LoggerMessage(EventId = 118104, Level = LogLevel.Debug, Message = "Failed → update business metrics from client query"
)]
    private partial void LogClientMetricsFailure(Exception ex);
}
