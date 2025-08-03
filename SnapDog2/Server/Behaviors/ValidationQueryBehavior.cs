namespace SnapDog2.Server.Behaviors;

using Cortex.Mediator.Queries;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Pipeline behavior that validates queries using FluentValidation before processing.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class ValidationQueryBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IResult
{
    private readonly IEnumerable<IValidator<TQuery>> _validators;
    private readonly ILogger<ValidationQueryBehavior<TQuery, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationQueryBehavior{TQuery, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators for the query type.</param>
    /// <param name="logger">The logger instance.</param>
    public ValidationQueryBehavior(
        IEnumerable<IValidator<TQuery>> validators,
        ILogger<ValidationQueryBehavior<TQuery, TResponse>> logger
    )
    {
        this._validators = validators;
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

        if (this._validators.Any())
        {
            this.LogValidatingQuery(queryName);

            var context = new ValidationContext<TQuery>(query);
            var validationResults = await Task.WhenAll(
                    this._validators.Select(v => v.ValidateAsync(context, cancellationToken))
                )
                .ConfigureAwait(false);

            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                this.LogValidationFailed(queryName, failures.Count);
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }

    [LoggerMessage(2501, LogLevel.Debug, "Validating query {QueryName}")]
    private partial void LogValidatingQuery(string queryName);

    [LoggerMessage(2502, LogLevel.Warning, "Validation failed for query {QueryName} with {FailureCount} errors")]
    private partial void LogValidationFailed(string queryName, int failureCount);
}
