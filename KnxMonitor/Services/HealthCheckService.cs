using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Simple HTTP health check service for container health monitoring.
/// </summary>
public class HealthCheckService : IDisposable
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IKnxMonitorService _knxMonitorService;
    private HttpListener? _httpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;
    private bool _disposed;

    public HealthCheckService(ILogger<HealthCheckService> logger, IKnxMonitorService knxMonitorService)
    {
        _logger = logger;
        _knxMonitorService = knxMonitorService;
    }

    /// <summary>
    /// Starts the health check HTTP server.
    /// </summary>
    /// <param name="port">Port to listen on (default: 8080).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task StartAsync(int port = 8080, CancellationToken cancellationToken = default)
    {
        if (_httpListener != null)
        {
            return Task.CompletedTask; // Already started
        }

        try
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://+:{port}/");
            _httpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerTask = Task.Run(() => HandleRequestsAsync(_cancellationTokenSource.Token), cancellationToken);

            _logger.LogInformation("Health check service started on port {Port}", port);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start health check service on port {Port}", port);
            throw;
        }
    }

    /// <summary>
    /// Stops the health check HTTP server.
    /// </summary>
    public async Task StopAsync()
    {
        if (_httpListener == null)
        {
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
            _httpListener.Stop();
            _httpListener.Close();

            if (_listenerTask != null)
            {
                await _listenerTask;
            }

            _logger.LogInformation("Health check service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping health check service");
        }
        finally
        {
            _httpListener = null;
            _listenerTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task HandleRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener != null)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Expected when stopping
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling health check request");
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.Url?.AbsolutePath == "/health")
            {
                await HandleHealthCheckAsync(response);
            }
            else if (request.Url?.AbsolutePath == "/ready")
            {
                await HandleReadinessCheckAsync(response);
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponseAsync(response, "Not Found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check request");
        }
    }

    private async Task HandleHealthCheckAsync(HttpListenerResponse response)
    {
        var healthStatus = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            knx = new
            {
                connected = _knxMonitorService.IsConnected,
                connectionStatus = _knxMonitorService.ConnectionStatus,
                messagesReceived = _knxMonitorService.MessageCount,
            },
        };

        response.StatusCode = 200;
        response.ContentType = "application/json";
        await WriteJsonResponseAsync(response, healthStatus);
    }

    private async Task HandleReadinessCheckAsync(HttpListenerResponse response)
    {
        var isReady = _knxMonitorService.IsConnected;
        var statusCode = isReady ? 200 : 503;

        var readinessStatus = new
        {
            status = isReady ? "ready" : "not ready",
            timestamp = DateTime.UtcNow,
            knx = new
            {
                connected = _knxMonitorService.IsConnected,
                connectionStatus = _knxMonitorService.ConnectionStatus,
            },
        };

        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        await WriteJsonResponseAsync(response, readinessStatus);
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await WriteResponseAsync(response, json);
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAsync().GetAwaiter().GetResult();
        _disposed = true;
    }
}
