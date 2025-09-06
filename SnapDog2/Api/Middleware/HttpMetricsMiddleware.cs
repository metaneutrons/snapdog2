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

using System.Diagnostics;
using SnapDog2.Domain.Abstractions;

/// <summary>
/// Middleware that records comprehensive HTTP request metrics.
/// Tracks request duration, status codes, endpoints, and error rates.
/// </summary>
public partial class HttpMetricsMiddleware(
    RequestDelegate next,
    ILogger<HttpMetricsMiddleware> logger,
    IApplicationMetrics metricsService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var method = request.Method;
        var path = GetNormalizedPath(request.Path);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Record the exception but don't handle it - let it bubble up
            metricsService.RecordException(ex, "HttpPipeline", $"{method} {path}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;

            // Record HTTP metrics
            metricsService.RecordHttpRequest(method, path, statusCode, durationSeconds);

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000) // 1 second threshold
            {
                this.LogSlowHttpRequest(method, path, stopwatch.ElapsedMilliseconds, statusCode);
            }

            // Log error responses
            if (statusCode >= 400)
            {
                var errorType = GetErrorType(statusCode);
                metricsService.RecordError(errorType, "HttpPipeline", $"{method} {path}");

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
                // Replace numeric IDs with placeholders
                for (var i = 3; i < segments.Length; i++)
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

    [LoggerMessage(EventId = 113900, Level = LogLevel.Warning, Message = "Slow HTTP request: {Method} {Path} took {ElapsedMilliseconds}ms (Status: {StatusCode})"
)]
    private partial void LogSlowHttpRequest(string method, string path, long elapsedMilliseconds, int statusCode);

    [LoggerMessage(EventId = 113901, Level = LogLevel.Warning, Message = "HTTP error: {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds}ms"
)]
    private partial void LogHttpError(string method, string path, int statusCode, long elapsedMilliseconds);
}
