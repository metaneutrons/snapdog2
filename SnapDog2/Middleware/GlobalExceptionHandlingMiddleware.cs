using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;

namespace SnapDog2.Middleware;

/// <summary>
/// global exception handling middleware
/// </summary>
public partial class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly bool _includeStackTraces;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment
    )
    {
        this._next = next;
        this._logger = logger;
        this._environment = environment;

        // Include stack traces in development or when debug logging is enabled
        this._includeStackTraces = environment.IsDevelopment() || logger.IsEnabled(LogLevel.Debug);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this._next(context);
        }
        catch (Exception ex)
        {
            await this.HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Log the exception with comprehensive context
        if (this._includeStackTraces || IsUnexpectedException(exception))
        {
            this.LogUnhandledExceptionWithStackTrace(
                requestMethod,
                requestPath,
                correlationId,
                remoteIp,
                userAgent,
                exception
            );
        }
        else
        {
            this.LogUnhandledException(
                requestMethod,
                requestPath,
                correlationId,
                remoteIp,
                userAgent,
                exception.GetType().Name,
                exception.Message
            );
        }

        // Determine response based on exception type
        var (statusCode, errorResponse) = exception switch
        {
            AddressInUseException => (
                HttpStatusCode.ServiceUnavailable,
                this.CreateErrorResponse(
                    "SERVICE_UNAVAILABLE",
                    "Service is temporarily unavailable due to port conflicts",
                    correlationId,
                    "The service cannot start because required network ports are in use. Please try again later."
                )
            ),

            SocketException => (
                HttpStatusCode.ServiceUnavailable,
                this.CreateErrorResponse(
                    "NETWORK_ERROR",
                    "Network connectivity issue",
                    correlationId,
                    "A network error occurred while processing your request."
                )
            ),

            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                this.CreateErrorResponse(
                    "TIMEOUT",
                    "Request timed out",
                    correlationId,
                    "The request took too long to process."
                )
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                this.CreateErrorResponse(
                    "ACCESS_DENIED",
                    "Access denied",
                    correlationId,
                    "You don't have permission to access this resource."
                )
            ),

            ArgumentException => (
                HttpStatusCode.BadRequest,
                this.CreateErrorResponse(
                    "INVALID_ARGUMENT",
                    "Invalid request parameters",
                    correlationId,
                    "One or more request parameters are invalid."
                )
            ),

            InvalidOperationException => (
                HttpStatusCode.Conflict,
                this.CreateErrorResponse(
                    "INVALID_OPERATION",
                    "Invalid operation",
                    correlationId,
                    "The requested operation cannot be performed in the current state."
                )
            ),

            NotImplementedException => (
                HttpStatusCode.NotImplemented,
                this.CreateErrorResponse(
                    "NOT_IMPLEMENTED",
                    "Feature not implemented",
                    correlationId,
                    "This feature is not yet implemented."
                )
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                this.CreateErrorResponse(
                    "INTERNAL_ERROR",
                    "An unexpected error occurred",
                    correlationId,
                    "An internal server error occurred while processing your request."
                )
            ),
        };

        // Set response properties
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Add correlation ID header for tracking
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Response.Headers["X-Error-Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

        // Serialize and write response
        var jsonResponse = JsonSerializer.Serialize(
            errorResponse,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = this._environment.IsDevelopment(),
            }
        );

        await context.Response.WriteAsync(jsonResponse);

        // Log response details
        this.LogExceptionResponseSent((int)statusCode, errorResponse.ErrorCode, correlationId);
    }

    private ErrorResponse CreateErrorResponse(
        string errorCode,
        string message,
        string correlationId,
        string userMessage
    )
    {
        return new ErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            UserMessage = userMessage,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            SupportInfo = new SupportInfo
            {
                ContactEmail = "github@schmieder.eu",
                DocumentationUrl = "https://github.com/metaneutrons/snapdog2",
            },
        };
    }

    /// <summary>
    /// Determines if an exception is unexpected and should always include stack trace
    /// </summary>
    private static bool IsUnexpectedException(Exception exception)
    {
        // These are expected exceptions that don't need stack traces in production
        var expectedExceptionTypes = new[]
        {
            typeof(UnauthorizedAccessException),
            typeof(ArgumentException),
            typeof(InvalidOperationException),
            typeof(NotImplementedException),
            typeof(TimeoutException),
            typeof(SocketException),
            typeof(AddressInUseException),
        };

        return !expectedExceptionTypes.Contains(exception.GetType());
    }

    #region Logging

    [LoggerMessage(
        3001,
        LogLevel.Error,
        "🚨 UNHANDLED EXCEPTION in {RequestMethod} {RequestPath} | CorrelationId: {CorrelationId} | RemoteIP: {RemoteIp} | UserAgent: {UserAgent}"
    )]
    private partial void LogUnhandledExceptionWithStackTrace(
        string requestMethod,
        string requestPath,
        string correlationId,
        string remoteIp,
        string userAgent,
        Exception exception
    );

    [LoggerMessage(
        3002,
        LogLevel.Error,
        "🚨 UNHANDLED EXCEPTION in {RequestMethod} {RequestPath} | CorrelationId: {CorrelationId} | RemoteIP: {RemoteIp} | UserAgent: {UserAgent} | Error: {ExceptionType} - {ExceptionMessage}"
    )]
    private partial void LogUnhandledException(
        string requestMethod,
        string requestPath,
        string correlationId,
        string remoteIp,
        string userAgent,
        string exceptionType,
        string exceptionMessage
    );

    [LoggerMessage(
        3003,
        LogLevel.Information,
        "🔄 Exception response sent: {StatusCode} {ErrorCode} | CorrelationId: {CorrelationId}"
    )]
    private partial void LogExceptionResponseSent(int statusCode, string errorCode, string correlationId);

    #endregion
}

/// <summary>
/// Standardized error response model
/// </summary>
public class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public SupportInfo SupportInfo { get; set; } = new();
}

/// <summary>
/// Support information for error responses
/// </summary>
public class SupportInfo
{
    public string ContactEmail { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for registering the global exception handling middleware
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handling middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
