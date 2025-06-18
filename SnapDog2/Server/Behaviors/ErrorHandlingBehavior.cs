using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that provides global exception handling and error transformation.
/// Catches unhandled exceptions and converts them to appropriate Result responses.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ErrorHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ErrorHandlingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ErrorHandlingBehavior(ILogger<ErrorHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the error handling pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response, potentially with error handling applied.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred while processing request: {RequestName}. Exception: {ExceptionType}",
                requestName,
                ex.GetType().Name
            );

            return HandleException<TResponse>(ex, requestName);
        }
    }

    /// <summary>
    /// Handles different types of exceptions and converts them to appropriate responses.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="requestName">The name of the request that caused the exception.</param>
    /// <returns>An appropriate error response.</returns>
    private static T HandleException<T>(Exception exception, string requestName)
    {
        var errorMessage = CreateErrorMessage(exception, requestName);

        // Handle Result<T> responses
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(T).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), new[] { typeof(string) });

            if (failureMethod != null)
            {
                var failureResult = failureMethod.Invoke(null, new object[] { errorMessage });
                return (T)failureResult!;
            }
        }

        // Handle non-generic Result responses
        if (typeof(T) == typeof(Result))
        {
            var failureResult = Result.Failure(errorMessage);
            return (T)(object)failureResult;
        }

        // For other response types, re-throw the exception
        // This should not happen in a well-designed system using Result patterns
        throw new InvalidOperationException(
            $"Cannot handle exception for response type {typeof(T).Name}. "
                + "Consider using Result or Result<T> for consistent error handling.",
            exception
        );
    }

    /// <summary>
    /// Creates an appropriate error message based on the exception type.
    /// </summary>
    /// <param name="exception">The exception to create a message for.</param>
    /// <param name="requestName">The name of the request that caused the exception.</param>
    /// <returns>A user-friendly error message.</returns>
    private static string CreateErrorMessage(Exception exception, string requestName)
    {
        return exception switch
        {
            ValidationException validationEx => CreateValidationErrorMessage(validationEx),
            ArgumentNullException => "Required parameter is missing.",
            ArgumentException argEx => $"Invalid argument: {argEx.Message}",
            InvalidOperationException invalidOpEx => $"Invalid operation: {invalidOpEx.Message}",
            UnauthorizedAccessException => "Access denied. You do not have permission to perform this operation.",
            TimeoutException => "The operation timed out. Please try again later.",
            TaskCanceledException => "The operation was cancelled.",
            OperationCanceledException => "The operation was cancelled.",
            NotSupportedException notSupportedEx => $"Operation not supported: {notSupportedEx.Message}",
            NotImplementedException => "This functionality is not yet implemented.",
            KeyNotFoundException => "The requested resource was not found.",
            FormatException formatEx => $"Invalid format: {formatEx.Message}",
            OverflowException => "A numeric overflow occurred.",
            DivideByZeroException => "Division by zero error.",
            OutOfMemoryException => "Insufficient memory to complete the operation.",
            StackOverflowException => "Stack overflow error occurred.",
            FileNotFoundException fileNotFoundEx => $"Required file not found: {fileNotFoundEx.FileName}",
            DirectoryNotFoundException => "Required directory not found.",
            IOException ioEx => $"Input/output error: {ioEx.Message}",
            HttpRequestException httpEx => CreateHttpErrorMessage(httpEx),
            _ => CreateGenericErrorMessage(exception, requestName),
        };
    }

    /// <summary>
    /// Creates an error message for validation exceptions.
    /// </summary>
    /// <param name="validationException">The validation exception.</param>
    /// <returns>A formatted validation error message.</returns>
    private static string CreateValidationErrorMessage(ValidationException validationException)
    {
        if (validationException.Errors?.Any() == true)
        {
            var errors = validationException.Errors.Select(e => e.ErrorMessage);
            return $"Validation failed: {string.Join("; ", errors)}";
        }

        return $"Validation failed: {validationException.Message}";
    }

    /// <summary>
    /// Creates an error message for HTTP-related exceptions.
    /// </summary>
    /// <param name="httpException">The HTTP exception.</param>
    /// <returns>A user-friendly HTTP error message.</returns>
    private static string CreateHttpErrorMessage(HttpRequestException httpException)
    {
        var baseMessage = "External service communication error";

        if (httpException.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return $"{baseMessage}: Request timed out.";
        }

        if (
            httpException.Message.Contains("404", StringComparison.OrdinalIgnoreCase)
            || httpException.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase)
        )
        {
            return $"{baseMessage}: Service endpoint not found.";
        }

        if (
            httpException.Message.Contains("401", StringComparison.OrdinalIgnoreCase)
            || httpException.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
        )
        {
            return $"{baseMessage}: Authentication failed.";
        }

        if (
            httpException.Message.Contains("403", StringComparison.OrdinalIgnoreCase)
            || httpException.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
        )
        {
            return $"{baseMessage}: Access forbidden.";
        }

        if (
            httpException.Message.Contains("500", StringComparison.OrdinalIgnoreCase)
            || httpException.Message.Contains("Internal Server Error", StringComparison.OrdinalIgnoreCase)
        )
        {
            return $"{baseMessage}: External service error.";
        }

        return $"{baseMessage}: {httpException.Message}";
    }

    /// <summary>
    /// Creates a generic error message for unhandled exception types.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="requestName">The request name.</param>
    /// <returns>A generic error message.</returns>
    private static string CreateGenericErrorMessage(Exception exception, string requestName)
    {
        // Include exception type in development/debug scenarios
        var exceptionType = exception.GetType().Name.Replace("Exception", "", StringComparison.OrdinalIgnoreCase);

        // Provide different messages based on whether it's a command or query
        var operationType = requestName.Contains("Command", StringComparison.OrdinalIgnoreCase)
            ? "operation"
            : "request";

        return $"An error occurred while processing the {operationType}. "
            + $"Error type: {exceptionType}. "
            + "Please try again or contact support if the problem persists.";
    }
}
