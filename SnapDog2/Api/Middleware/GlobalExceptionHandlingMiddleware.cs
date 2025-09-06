//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//

namespace SnapDog2.Api.Middleware;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using SnapDog2.Domain.Abstractions;

/// <summary>
/// Global exception handling middleware with error tracking integration.
/// </summary>
public partial class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IHostEnvironment environment,
    IErrorTrackingService errorTrackingService
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;
    private readonly IErrorTrackingService _errorTrackingService = errorTrackingService;
    private readonly bool _includeStackTraces = environment.IsDevelopment() || logger.IsEnabled(LogLevel.Debug);

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

        // Record the exception in the error tracking service
        this._errorTrackingService.RecordException(
            exception,
            "HttpPipeline",
            $"{requestMethod} {requestPath}"
        );

        // Determine response based on exception type
        var (statusCode, errorResponse) = exception switch
        {
            AddressInUseException => (
                HttpStatusCode.ServiceUnavailable,
                CreateErrorResponse(
                    "SERVICE_UNAVAILABLE",
                    "Service is temporarily unavailable due to port conflicts",
                    correlationId,
                    "The service cannot start because required network ports are in use. Please try again later."
                )
            ),

            SocketException => (
                HttpStatusCode.ServiceUnavailable,
                CreateErrorResponse(
                    "NETWORK_ERROR",
                    "Network connectivity issue",
                    correlationId,
                    "A network error occurred while processing your request."
                )
            ),

            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                CreateErrorResponse(
                    "TIMEOUT",
                    "Request timed out",
                    correlationId,
                    "The request took too long to process."
                )
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                CreateErrorResponse(
                    "ACCESS_DENIED",
                    "Access denied",
                    correlationId,
                    "You don't have permission to access this resource."
                )
            ),

            ArgumentException => (
                HttpStatusCode.BadRequest,
                CreateErrorResponse(
                    "INVALID_ARGUMENT",
                    "Invalid request parameters",
                    correlationId,
                    "One or more request parameters are invalid."
                )
            ),

            InvalidOperationException => (
                HttpStatusCode.Conflict,
                CreateErrorResponse(
                    "INVALID_OPERATION",
                    "Invalid operation",
                    correlationId,
                    "The requested operation cannot be performed in the current state."
                )
            ),

            NotImplementedException => (
                HttpStatusCode.NotImplemented,
                CreateErrorResponse(
                    "NOT_IMPLEMENTED",
                    "Feature not implemented",
                    correlationId,
                    "This feature is not yet implemented."
                )
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                CreateErrorResponse(
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

    private static ErrorResponse CreateErrorResponse(
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
        EventId = 5700,
        Level = LogLevel.Error,
        Message = "🚨 UNHANDLED EXCEPTION in {RequestMethod} {RequestPath} | CorrelationId: {CorrelationId} | RemoteIP: {RemoteIp} | UserAgent: {UserAgent}"
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
        EventId = 5701,
        Level = LogLevel.Error,
        Message = "🚨 UNHANDLED EXCEPTION in {RequestMethod} {RequestPath} | CorrelationId: {CorrelationId} | RemoteIP: {RemoteIp} | UserAgent: {UserAgent} | Error: {ExceptionType} - {ExceptionMessage}"
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
        EventId = 5702,
        Level = LogLevel.Information,
        Message = "🔄 Exception response sent: {StatusCode} {ErrorCode} | CorrelationId: {CorrelationId}"
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
