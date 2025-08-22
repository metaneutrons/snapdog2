namespace SnapDog2.Middleware;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Application;

/// <summary>
/// Middleware that records comprehensive HTTP request metrics.
/// Tracks request duration, status codes, endpoints, and error rates.
/// </summary>
public partial class HttpMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpMetricsMiddleware> _logger;
    private readonly EnterpriseMetricsService _metricsService;

    public HttpMetricsMiddleware(
        RequestDelegate next,
        ILogger<HttpMetricsMiddleware> logger,
        EnterpriseMetricsService metricsService
    )
    {
        _next = next;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var method = request.Method;
        var path = GetNormalizedPath(request.Path);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Record the exception but don't handle it - let it bubble up
            _metricsService.RecordException(ex, "HttpPipeline", $"{method} {path}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;

            // Record HTTP metrics
            _metricsService.RecordHttpRequest(method, path, statusCode, durationSeconds);

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000) // 1 second threshold
            {
                this.LogSlowHttpRequest(method, path, stopwatch.ElapsedMilliseconds, statusCode);
            }

            // Log error responses
            if (statusCode >= 400)
            {
                var errorType = GetErrorType(statusCode);
                _metricsService.RecordError(errorType, "HttpPipeline", $"{method} {path}");

                this.LogHttpError(method, path, statusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Normalizes the request path for metrics to avoid high cardinality.
    /// Replaces dynamic segments with placeholders.
    /// </summary>
    private static string GetNormalizedPath(PathString path)
    {
        var pathValue = path.Value ?? "/";

        // Normalize API paths to reduce cardinality
        if (pathValue.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 3)
            {
                var controller = segments[2]; // e.g., "zones", "clients"

                // Replace numeric IDs with placeholders
                for (int i = 3; i < segments.Length; i++)
                {
                    if (int.TryParse(segments[i], out _))
                    {
                        segments[i] = "{index}";
                    }
                }

                return "/" + string.Join("/", segments);
            }
        }

        // Handle health check endpoints
        if (pathValue.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/health";
        }

        // Handle swagger/openapi endpoints
        if (
            pathValue.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || pathValue.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "/swagger";
        }

        // Default normalization - remove query parameters and fragments
        var questionMarkIndex = pathValue.IndexOf('?');
        if (questionMarkIndex >= 0)
        {
            pathValue = pathValue.Substring(0, questionMarkIndex);
        }

        return pathValue;
    }

    /// <summary>
    /// Gets the error type based on HTTP status code.
    /// </summary>
    private static string GetErrorType(int statusCode)
    {
        return statusCode switch
        {
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            405 => "MethodNotAllowed",
            408 => "RequestTimeout",
            409 => "Conflict",
            422 => "UnprocessableEntity",
            429 => "TooManyRequests",
            500 => "InternalServerError",
            501 => "NotImplemented",
            502 => "BadGateway",
            503 => "ServiceUnavailable",
            504 => "GatewayTimeout",
            _ when statusCode >= 400 && statusCode < 500 => "ClientError",
            _ when statusCode >= 500 => "ServerError",
            _ => "Unknown",
        };
    }

    [LoggerMessage(
        3001,
        LogLevel.Warning,
        "Slow HTTP request: {Method} {Path} took {ElapsedMilliseconds}ms (Status: {StatusCode})"
    )]
    private partial void LogSlowHttpRequest(string method, string path, long elapsedMilliseconds, int statusCode);

    [LoggerMessage(
        3002,
        LogLevel.Warning,
        "HTTP error: {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds}ms"
    )]
    private partial void LogHttpError(string method, string path, int statusCode, long elapsedMilliseconds);
}
