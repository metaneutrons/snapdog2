namespace SnapDog2.Server.Behaviors;

using Cortex.Mediator.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Pipeline behavior that validates commands using FluentValidation before processing.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class ValidationCommandBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly IEnumerable<IValidator<TCommand>> _validators;
    private readonly ILogger<ValidationCommandBehavior<TCommand, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationCommandBehavior{TCommand, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators for the command type.</param>
    /// <param name="logger">The logger instance.</param>
    public ValidationCommandBehavior(
        IEnumerable<IValidator<TCommand>> validators,
        ILogger<ValidationCommandBehavior<TCommand, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(TCommand command, CommandHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;

        if (_validators.Any())
        {
            LogValidatingCommand(commandName);

            var context = new ValidationContext<TCommand>(command);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                LogValidationFailed(commandName, failures.Count);
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }

    [LoggerMessage(2401, LogLevel.Debug, "Validating command {CommandName}")]
    private partial void LogValidatingCommand(string commandName);

    [LoggerMessage(2402, LogLevel.Warning, "Validation failed for command {CommandName} with {FailureCount} errors")]
    private partial void LogValidationFailed(string commandName, int failureCount);
}
