namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Pipeline behavior that measures command execution performance and logs slow operations.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class PerformanceCommandBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<PerformanceCommandBehavior<TCommand, TResponse>> _logger;
    private const int SlowOperationThresholdMs = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceCommandBehavior{TCommand, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PerformanceCommandBehavior(ILogger<PerformanceCommandBehavior<TCommand, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(TCommand command, CommandHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowOperationThresholdMs)
            {
                LogSlowCommand(commandName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogCommandException(commandName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(2201, LogLevel.Warning, "Slow command detected: {CommandName} took {ElapsedMilliseconds}ms")]
    private partial void LogSlowCommand(string commandName, long elapsedMilliseconds);

    [LoggerMessage(2202, LogLevel.Error, "Command {CommandName} threw exception after {ElapsedMilliseconds}ms")]
    private partial void LogCommandException(string commandName, long elapsedMilliseconds, Exception ex);
}
