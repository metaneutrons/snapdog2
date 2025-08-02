namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Pipeline behavior that logs query handling details, duration, and success/failure status.
/// Creates OpenTelemetry Activities for tracing.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class LoggingQueryBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<LoggingQueryBehavior<TQuery, TResponse>> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingQueryBehavior{TQuery, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingQueryBehavior(ILogger<LoggingQueryBehavior<TQuery, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var queryName = typeof(TQuery).Name;
        using var activity = ActivitySource.StartActivity($"CortexMediator.Query.{queryName}");

        LogQueryStarting(queryName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            LogQueryCompleted(queryName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogQueryFailed(queryName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(2101, LogLevel.Information, "Starting query {QueryName}")]
    private partial void LogQueryStarting(string queryName);

    [LoggerMessage(2102, LogLevel.Information, "Completed query {QueryName} in {ElapsedMilliseconds}ms")]
    private partial void LogQueryCompleted(string queryName, long elapsedMilliseconds);

    [LoggerMessage(2103, LogLevel.Error, "Query {QueryName} failed after {ElapsedMilliseconds}ms")]
    private partial void LogQueryFailed(string queryName, long elapsedMilliseconds, Exception ex);
}
