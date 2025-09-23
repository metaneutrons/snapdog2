using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Polly;
using SnapDog2.Infrastructure.Resilience;
using SnapDog2.Shared.Configuration;

namespace SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc;

public partial class SnapcastJsonRpcClient : IDisposable
{
    private readonly string _webSocketUrl;
    private readonly ILogger<SnapcastJsonRpcClient> _logger;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly ResiliencePipeline _operationPipeline;
    private ClientWebSocket? _webSocket;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _receiveTask;
    private int _requestId = 0;
    private volatile bool _isConnected = false;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    public event Action<string, JsonElement>? NotificationReceived;
    public event Action? ConnectionLost;
    public event Action? ConnectionRestored;

    public SnapcastJsonRpcClient(string webSocketUrl, ILogger<SnapcastJsonRpcClient> logger, ResilienceConfig? resilience = null)
    {
        _webSocketUrl = webSocketUrl;
        _logger = logger;

        // Use provided resilience config or defaults
        var config = resilience ?? new ResilienceConfig();
        _connectionPipeline = ResiliencePolicyFactory.CreateConnectionPipeline(config, "Snapcast");
        _operationPipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "Snapcast");
    }

    public async Task ConnectAsync()
    {
        await _connectionSemaphore.WaitAsync(_cancellationTokenSource.Token);
        try
        {
            if (_isConnected && _webSocket?.State == WebSocketState.Open)
            {
                return;
            }

            await _connectionPipeline.ExecuteAsync(async (ct) =>
            {
                await ConnectInternalAsync(ct);
            }, _cancellationTokenSource.Token);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        // Clean up existing connection
        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
            }
            _webSocket.Dispose();
        }

        _webSocket = new ClientWebSocket();

        // Configure WebSocket options for better reliability
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        LogConnecting(_webSocketUrl);
        await _webSocket.ConnectAsync(new Uri(_webSocketUrl), cancellationToken);

        _isConnected = true;
        LogConnected(_webSocketUrl);

        // Start message handling with automatic reconnection
        _receiveTask = Task.Run(() => HandleIncomingMessagesWithReconnection(), _cancellationTokenSource.Token);

        ConnectionRestored?.Invoke();
    }

    public async Task<T> SendRequestAsync<T>(string method, object? parameters = null)
    {
        return await _operationPipeline.ExecuteAsync(async (ct) =>
        {
            // Ensure connection is established
            if (!_isConnected || _webSocket?.State != WebSocketState.Open)
            {
                await ConnectAsync();
            }

            var requestId = Interlocked.Increment(ref _requestId).ToString();
            var request = new
            {
                id = requestId,
                jsonrpc = "2.0",
                method,
                @params = parameters
            };

            var tcs = new TaskCompletionSource<JsonElement>();
            _pendingRequests[requestId] = tcs;

            try
            {
                var json = JsonSerializer.Serialize(request);
                var bytes = Encoding.UTF8.GetBytes(json);

                await _webSocket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    ct);

                LogRequestSent(method, requestId);

                // Use cancellation token for timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cancellationTokenSource.Token);
                var response = await tcs.Task.WaitAsync(timeoutCts.Token);

                return JsonSerializer.Deserialize<T>(response.GetRawText())!;
            }
            catch (WebSocketException ex)
            {
                LogWebSocketError(ex, method);
                _isConnected = false;
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }, _cancellationTokenSource.Token);
    }

    private async Task HandleIncomingMessagesWithReconnection()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await HandleIncomingMessages();
            }
            catch (Exception ex) when (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                LogConnectionLost(ex);
                _isConnected = false;
                ConnectionLost?.Invoke();

                // Clear pending requests with connection error
                foreach (var kvp in _pendingRequests.ToArray())
                {
                    if (_pendingRequests.TryRemove(kvp.Key, out var tcs))
                    {
                        tcs.SetException(new WebSocketException("Connection lost"));
                    }
                }

                // Wait before attempting reconnection
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
                    await ConnectAsync();
                }
                catch (Exception reconnectEx)
                {
                    LogReconnectionFailed(reconnectEx);
                }
            }
        }
    }

    private async Task HandleIncomingMessages()
    {
        var buffer = new byte[8192]; // Increased buffer size
        var messageBuilder = new StringBuilder();

        while (_webSocket!.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                _cancellationTokenSource.Token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                LogConnectionClosed(result.CloseStatus, result.CloseStatusDescription);
                throw new WebSocketException("Connection closed by remote party");
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    var message = messageBuilder.ToString();
                    messageBuilder.Clear();

                    ProcessMessage(message);
                }
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (root.TryGetProperty("id", out var idElement))
            {
                // Response to a request
                var id = idElement.GetString()!;
                if (_pendingRequests.TryRemove(id, out var tcs))
                {
                    if (root.TryGetProperty("result", out var result))
                    {
                        // Clone the JsonElement to avoid disposal issues
                        var clonedResult = result.Clone();
                        tcs.SetResult(clonedResult);
                    }
                    else if (root.TryGetProperty("error", out var error))
                    {
                        var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                        tcs.SetException(new Exception($"JSON-RPC Error: {errorMessage}"));
                    }
                }
            }
            else if (root.TryGetProperty("method", out var methodElement))
            {
                // Notification
                var method = methodElement.GetString()!;
                var parameters = root.TryGetProperty("params", out var paramsElement) ? paramsElement.Clone() : default;

                LogNotificationReceived(method);
                NotificationReceived?.Invoke(method, parameters);
            }
        }
        catch (Exception ex)
        {
            LogMessageProcessingError(ex, message);
        }
    }

    public async Task DisconnectAsync()
    {
        _cancellationTokenSource.Cancel();

        await _connectionSemaphore.WaitAsync();
        try
        {
            _isConnected = false;

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            }

            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token is triggered
                }
            }

            // Clear any remaining pending requests
            foreach (var kvp in _pendingRequests.ToArray())
            {
                if (_pendingRequests.TryRemove(kvp.Key, out var tcs))
                {
                    tcs.SetCanceled();
                }
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore disposal errors
        }

        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
        _connectionSemaphore.Dispose();
    }

    // LoggerMessage patterns
    [LoggerMessage(EventId = 15113, Level = LogLevel.Debug, Message = "Connecting to Snapcast WebSocket: {Url}")]
    private partial void LogConnecting(string url);

    [LoggerMessage(EventId = 15114, Level = LogLevel.Information, Message = "Connected → Snapcast WebSocket: {Url}")]
    private partial void LogConnected(string url);

    [LoggerMessage(EventId = 15115, Level = LogLevel.Debug, Message = "Sent request: {Method} with ID: {RequestId}")]
    private partial void LogRequestSent(string method, string requestId);

    [LoggerMessage(EventId = 15116, Level = LogLevel.Warning, Message = "Connection lost, attempting reconnection")]
    private partial void LogConnectionLost(Exception ex);

    [LoggerMessage(EventId = 15117, Level = LogLevel.Debug, Message = "Received notification: {Method}")]
    private partial void LogNotificationReceived(string method);

    [LoggerMessage(EventId = 15118, Level = LogLevel.Error, Message = "Error processing message: {Message}")]
    private partial void LogMessageProcessingError(Exception ex, string message);

    [LoggerMessage(EventId = 15119, Level = LogLevel.Error, Message = "WebSocket error during {Method}")]
    private partial void LogWebSocketError(Exception ex, string method);

    [LoggerMessage(EventId = 15120, Level = LogLevel.Warning, Message = "Connection closed: {CloseStatus} - {CloseDescription}")]
    private partial void LogConnectionClosed(WebSocketCloseStatus? closeStatus, string? closeDescription);

    [LoggerMessage(EventId = 15121, Level = LogLevel.Error, Message = "Reconnection attempt failed")]
    private partial void LogReconnectionFailed(Exception ex);
}
