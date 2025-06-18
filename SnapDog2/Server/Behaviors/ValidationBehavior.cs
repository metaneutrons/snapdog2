using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests using FluentValidation.
/// Executes validation before the request is handled and returns validation errors if validation fails.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The collection of validators for the request type.</param>
    /// <param name="logger">The logger.</param>
    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger
    )
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request validation pipeline.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response, potentially with validation errors.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogDebug("Validating request: {RequestName}", requestName);

        var validators = _validators.ToArray();
        if (validators.Length == 0)
        {
            _logger.LogDebug("No validators found for request: {RequestName}", requestName);
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            var errors = failures.Select(f => f.ErrorMessage).ToList();
            _logger.LogWarning(
                "Validation failed for request: {RequestName}. Errors: {ValidationErrors}",
                requestName,
                string.Join("; ", errors)
            );

            // Handle Result<T> responses
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(valueType)
                    .GetMethod(nameof(Result<object>.Failure), new[] { typeof(IEnumerable<string>) });

                if (failureMethod != null)
                {
                    var failureResult = failureMethod.Invoke(null, new object[] { errors });
                    return (TResponse)failureResult!;
                }
            }

            // Handle non-generic Result responses
            if (typeof(TResponse) == typeof(Result))
            {
                var failureResult = Result.Failure(errors);
                return (TResponse)(object)failureResult;
            }

            // For other response types, throw validation exception
            throw new ValidationException(failures);
        }

        _logger.LogDebug("Validation passed for request: {RequestName}", requestName);
        return await next();
    }
}
