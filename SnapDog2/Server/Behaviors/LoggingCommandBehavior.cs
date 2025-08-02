namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Pipeline behavior that logs command handling details, duration, and success/failure status.
/// Creates OpenTelemetry Activities for tracing.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class LoggingCommandBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<LoggingCommandBehavior<TCommand, TResponse>> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingCommandBehavior{TCommand, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingCommandBehavior(ILogger<LoggingCommandBehavior<TCommand, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(TCommand command, CommandHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        using var activity = ActivitySource.StartActivity($"CortexMediator.Command.{commandName}");
        
        LogCommandStarting(commandName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();
            
            LogCommandCompleted(commandName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogCommandFailed(commandName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(2001, LogLevel.Information, "Starting command {CommandName}")]
    private partial void LogCommandStarting(string commandName);

    [LoggerMessage(2002, LogLevel.Information, "Completed command {CommandName} in {ElapsedMilliseconds}ms")]
    private partial void LogCommandCompleted(string commandName, long elapsedMilliseconds);

    [LoggerMessage(2003, LogLevel.Error, "Command {CommandName} failed after {ElapsedMilliseconds}ms")]
    private partial void LogCommandFailed(string commandName, long elapsedMilliseconds, Exception ex);
}
