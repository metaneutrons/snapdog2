namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Pipeline behavior that measures query execution performance and logs slow operations.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="PerformanceQueryBehavior{TQuery, TResponse}"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class PerformanceQueryBehavior<TQuery, TResponse>(
    ILogger<PerformanceQueryBehavior<TQuery, TResponse>> logger
) : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<PerformanceQueryBehavior<TQuery, TResponse>> _logger = logger;
    private const int SlowOperationThresholdMs = 500;

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var queryName = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowOperationThresholdMs)
            {
                this.LogSlowQuery(queryName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.LogQueryException(queryName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(2301, LogLevel.Warning, "Slow query detected: {QueryName} took {ElapsedMilliseconds}ms")]
    private partial void LogSlowQuery(string queryName, long elapsedMilliseconds);

    [LoggerMessage(2302, LogLevel.Error, "Query {QueryName} threw exception after {ElapsedMilliseconds}ms")]
    private partial void LogQueryException(string queryName, long elapsedMilliseconds, Exception ex);
}
