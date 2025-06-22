using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Api.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with correlation ID support.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly ApiConfiguration.ApiLoggingSettings _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestResponseLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The logging options.</param>
    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        ApiConfiguration.ApiLoggingSettings options
    )
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Skip logging for excluded paths
        if (ShouldSkipLogging(context))
        {
            await _next(context);
            return;
        }

        // Set correlation ID
        var correlationId = GetOrSetCorrelationId(context);

        // Log request
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await LogRequestAsync(context, correlationId);

        // Capture response
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogException(context, correlationId, ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy response body back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
    }

    /// <summary>
    /// Determines if logging should be skipped for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if logging should be skipped; otherwise, false.</returns>
    private bool ShouldSkipLogging(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        return _options.ExcludedPaths.Any(excludedPath => path?.StartsWith(excludedPath.ToLowerInvariant()) == true);
    }

    /// <summary>
    /// Gets or sets the correlation ID for the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID.</returns>
    private string GetOrSetCorrelationId(HttpContext context)
    {
        // Check if correlation ID is already in headers
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var correlationId))
        {
            var id = correlationId.ToString();
            if (!string.IsNullOrEmpty(id))
            {
                context.Response.Headers.Append(_options.CorrelationIdHeader, id);
                return id;
            }
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.Headers.Append(_options.CorrelationIdHeader, newCorrelationId);
        return newCorrelationId;
    }

    /// <summary>
    /// Logs the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>The request body content.</returns>
    private async Task<string> LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        var requestBody = string.Empty;

        // Read request body if enabled and conditions are met
        if (_options.LogRequestBody && ShouldLogRequestBody(context))
        {
            request.EnableBuffering();

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            // Sanitize sensitive data
            requestBody = SanitizeSensitiveData(requestBody);
        }

        // Build request log
        var requestLog = new StringBuilder();
        requestLog.AppendLine($"HTTP Request [{correlationId}]");
        requestLog.AppendLine($"Method: {request.Method}");
        requestLog.AppendLine($"Path: {request.Path}{request.QueryString}");
        requestLog.AppendLine($"Protocol: {request.Protocol}");
        requestLog.AppendLine($"Host: {request.Host}");
        requestLog.AppendLine($"Content-Type: {request.ContentType}");
        requestLog.AppendLine($"Content-Length: {request.ContentLength}");
        requestLog.AppendLine($"User-Agent: {request.Headers.UserAgent}");
        requestLog.AppendLine($"Remote IP: {GetClientIpAddress(context)}");

        // Log headers if enabled
        if (_options.LogHeaders)
        {
            requestLog.AppendLine("Headers:");
            foreach (var header in request.Headers)
            {
                if (!IsHeaderSensitive(header.Key))
                {
                    requestLog.AppendLine($"  {header.Key}: {string.Join(", ", header.Value.AsEnumerable())}");
                }
                else
                {
                    requestLog.AppendLine($"  {header.Key}: [REDACTED]");
                }
            }
        }

        // Log body if available
        if (!string.IsNullOrEmpty(requestBody))
        {
            requestLog.AppendLine($"Body: {requestBody}");
        }

        _logger.LogInformation(requestLog.ToString());
        return requestBody;
    }

    /// <summary>
    /// Logs the HTTP response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var response = context.Response;
        var responseBody = string.Empty;

        // Read response body if enabled and conditions are met
        if (_options.LogResponseBody && ShouldLogResponseBody(context))
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            // Sanitize sensitive data
            responseBody = SanitizeSensitiveData(responseBody);
        }

        // Build response log
        var responseLog = new StringBuilder();
        responseLog.AppendLine($"HTTP Response [{correlationId}]");
        responseLog.AppendLine($"Status: {response.StatusCode}");
        responseLog.AppendLine($"Content-Type: {response.ContentType}");
        responseLog.AppendLine($"Content-Length: {response.ContentLength}");
        responseLog.AppendLine($"Duration: {elapsedMilliseconds}ms");

        // Log headers if enabled
        if (_options.LogHeaders)
        {
            responseLog.AppendLine("Headers:");
            foreach (var header in response.Headers)
            {
                if (!IsHeaderSensitive(header.Key))
                {
                    responseLog.AppendLine($"  {header.Key}: {string.Join(", ", header.Value.AsEnumerable())}");
                }
                else
                {
                    responseLog.AppendLine($"  {header.Key}: [REDACTED]");
                }
            }
        }

        // Log body if available
        if (!string.IsNullOrEmpty(responseBody))
        {
            responseLog.AppendLine($"Body: {responseBody}");
        }

        var logLevel = GetLogLevel(response.StatusCode);
        _logger.Log(logLevel, responseLog.ToString());
    }

    /// <summary>
    /// Logs exceptions that occur during request processing.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    private void LogException(HttpContext context, string correlationId, Exception exception, long elapsedMilliseconds)
    {
        _logger.LogError(
            exception,
            "HTTP Request [{CorrelationId}] failed after {Duration}ms. Method: {Method}, Path: {Path}, Status: {StatusCode}",
            correlationId,
            elapsedMilliseconds,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode
        );
    }

    /// <summary>
    /// Determines if the request body should be logged.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if the request body should be logged; otherwise, false.</returns>
    private bool ShouldLogRequestBody(HttpContext context)
    {
        var contentType = context.Request.ContentType?.ToLowerInvariant();
        var contentLength = context.Request.ContentLength ?? 0;

        // Don't log large bodies
        if (contentLength > _options.MaxBodySize)
        {
            return false;
        }

        // Only log text-based content types
        return contentType != null
            && (
                contentType.Contains("application/json")
                || contentType.Contains("application/xml")
                || contentType.Contains("text/")
                || contentType.Contains("application/x-www-form-urlencoded")
            );
    }

    /// <summary>
    /// Determines if the response body should be logged.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if the response body should be logged; otherwise, false.</returns>
    private bool ShouldLogResponseBody(HttpContext context)
    {
        var contentType = context.Response.ContentType?.ToLowerInvariant();
        var contentLength = context.Response.ContentLength ?? 0;

        // Don't log large bodies
        if (contentLength > _options.MaxBodySize)
        {
            return false;
        }

        // Only log text-based content types
        return contentType != null
            && (
                contentType.Contains("application/json")
                || contentType.Contains("application/xml")
                || contentType.Contains("text/")
            );
    }

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client IP address.</returns>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedHeader))
        {
            var forwardedIps = forwardedHeader.ToString().Split(',');
            if (forwardedIps.Length > 0)
            {
                return forwardedIps[0].Trim();
            }
        }

        // Check for X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIpHeader))
        {
            return realIpHeader.ToString();
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Determines if a header contains sensitive information.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <returns>True if the header is sensitive; otherwise, false.</returns>
    private bool IsHeaderSensitive(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "authorization",
            "x-api-key",
            "cookie",
            "set-cookie",
            "authentication",
            "x-auth-token",
        };

        return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
    }

    /// <summary>
    /// Sanitizes sensitive data from the content.
    /// </summary>
    /// <param name="content">The content to sanitize.</param>
    /// <returns>The sanitized content.</returns>
    private string SanitizeSensitiveData(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Simple regex patterns to redact common sensitive fields
        var patterns = new Dictionary<string, string>
        {
            { "\"password\"\\s*:\\s*\"[^\"]*\"", "\"password\": \"[REDACTED]\"" },
            { "\"apiKey\"\\s*:\\s*\"[^\"]*\"", "\"apiKey\": \"[REDACTED]\"" },
            { "\"token\"\\s*:\\s*\"[^\"]*\"", "\"token\": \"[REDACTED]\"" },
            { "\"secret\"\\s*:\\s*\"[^\"]*\"", "\"secret\": \"[REDACTED]\"" },
            { "\"key\"\\s*:\\s*\"[^\"]*\"", "\"key\": \"[REDACTED]\"" },
        };

        var sanitized = content;
        foreach (var pattern in patterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                pattern.Key,
                pattern.Value,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        return sanitized;
    }

    /// <summary>
    /// Gets the appropriate log level for the response status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The log level.</returns>
    private LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }
}

/* This file is now obsolete. All request/response logging configuration has been merged into ApiConfiguration. */
