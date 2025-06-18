using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that provides comprehensive request/response logging with performance metrics.
/// Logs request details, execution time, and response information for all MediatR requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request logging pipeline.
    /// </summary>
    /// <param name="request">The request to log.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response with comprehensive logging.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting request {RequestName} [{RequestId}] at {Timestamp}",
            requestName,
            requestId,
            DateTime.UtcNow
        );

        // Log request details for commands (but not sensitive data)
        if (IsCommand(requestName))
        {
            _logger.LogDebug(
                "Request {RequestName} [{RequestId}] details: {RequestType}",
                requestName,
                requestId,
                typeof(TRequest).FullName
            );
        }

        TResponse response;
        Exception? exception = null;

        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            exception = ex;
            _logger.LogError(
                ex,
                "Request {RequestName} [{RequestId}] failed after {ElapsedMs}ms with exception: {ExceptionType}",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name
            );
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        // Log response details
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var logLevel = GetLogLevel(elapsedMs, response, exception);

        _logger.Log(
            logLevel,
            "Completed request {RequestName} [{RequestId}] in {ElapsedMs}ms with result: {ResultType}",
            requestName,
            requestId,
            elapsedMs,
            GetResultDescription(response)
        );

        // Log performance warnings for slow requests
        if (elapsedMs > 1000)
        {
            _logger.LogWarning(
                "Slow request detected: {RequestName} [{RequestId}] took {ElapsedMs}ms",
                requestName,
                requestId,
                elapsedMs
            );
        }

        return response;
    }

    /// <summary>
    /// Determines if the request is a command (write operation).
    /// </summary>
    /// <param name="requestName">The request name.</param>
    /// <returns>True if it's a command; otherwise, false.</returns>
    private static bool IsCommand(string requestName)
    {
        return requestName.Contains("Command", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the appropriate log level based on the response and performance.
    /// </summary>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    /// <param name="response">The response.</param>
    /// <param name="exception">Any exception that occurred.</param>
    /// <returns>The appropriate log level.</returns>
    private static LogLevel GetLogLevel(long elapsedMs, TResponse response, Exception? exception)
    {
        if (exception != null)
        {
            return LogLevel.Error;
        }

        // Check if response indicates failure (for Result types)
        if (IsFailureResult(response))
        {
            return LogLevel.Warning;
        }

        // Performance-based logging
        return elapsedMs switch
        {
            > 5000 => LogLevel.Warning, // Very slow
            > 1000 => LogLevel.Information, // Slow
            _ => LogLevel.Debug, // Normal
        };
    }

    /// <summary>
    /// Checks if the response indicates a failure (for Result types).
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <returns>True if the response indicates failure; otherwise, false.</returns>
    private static bool IsFailureResult(TResponse response)
    {
        return response switch
        {
            Result result => result.IsFailure,
            _
                when typeof(TResponse).IsGenericType
                    && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>) => (bool)(
                typeof(TResponse).GetProperty("IsFailure")?.GetValue(response) ?? false
            ),
            _ => false,
        };
    }

    /// <summary>
    /// Gets a description of the result for logging.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>A string description of the result.</returns>
    private static string GetResultDescription(TResponse response)
    {
        return response switch
        {
            Result result => result.IsSuccess ? "Success" : $"Failure: {result.Error}",
            _
                when typeof(TResponse).IsGenericType
                    && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>) => GetGenericResultDescription(
                response
            ),
            _ => typeof(TResponse).Name,
        };
    }

    /// <summary>
    /// Gets a description for generic Result&lt;T&gt; types.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>A string description of the generic result.</returns>
    private static string GetGenericResultDescription(TResponse response)
    {
        var isSuccess = (bool)(typeof(TResponse).GetProperty("IsSuccess")?.GetValue(response) ?? false);
        if (isSuccess)
        {
            return "Success";
        }

        var error = typeof(TResponse).GetProperty("Error")?.GetValue(response) as string;
        return $"Failure: {error}";
    }
}
