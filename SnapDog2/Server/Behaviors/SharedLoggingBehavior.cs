namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Commands;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Command pipeline behavior with shared logging implementation.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class SharedLoggingCommandBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<SharedLoggingCommandBehavior<TCommand, TResponse>> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedLoggingCommandBehavior{TCommand, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SharedLoggingCommandBehavior(ILogger<SharedLoggingCommandBehavior<TCommand, TResponse>> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var commandName = typeof(TCommand).Name;
        using var activity = ActivitySource.StartActivity($"CortexMediator.Command.{commandName}");

        this._logger.LogInformation("Starting Command {CommandName}", commandName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            this._logger.LogInformation(
                "Completed Command {CommandName} in {ElapsedMilliseconds}ms",
                commandName,
                stopwatch.ElapsedMilliseconds
            );
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this._logger.LogError(
                ex,
                "Command {CommandName} failed after {ElapsedMilliseconds}ms",
                commandName,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }
}

/// <summary>
/// Query pipeline behavior with shared logging implementation.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class SharedLoggingQueryBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<SharedLoggingQueryBehavior<TQuery, TResponse>> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedLoggingQueryBehavior{TQuery, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SharedLoggingQueryBehavior(ILogger<SharedLoggingQueryBehavior<TQuery, TResponse>> logger)
    {
        this._logger = logger;
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

        this._logger.LogInformation("Starting Query {QueryName}", queryName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            this._logger.LogInformation(
                "Completed Query {QueryName} in {ElapsedMilliseconds}ms",
                queryName,
                stopwatch.ElapsedMilliseconds
            );
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this._logger.LogError(
                ex,
                "Query {QueryName} failed after {ElapsedMilliseconds}ms",
                queryName,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }
}
