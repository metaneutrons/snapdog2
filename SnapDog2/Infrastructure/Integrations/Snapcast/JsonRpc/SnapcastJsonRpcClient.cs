using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc;

public partial class SnapcastJsonRpcClient : IDisposable
{
    private readonly string _webSocketUrl;
    private readonly ILogger<SnapcastJsonRpcClient> _logger;
    private ClientWebSocket? _webSocket;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _receiveTask;
    private int _requestId = 0;

    public event Action<string, JsonElement>? NotificationReceived;

    public SnapcastJsonRpcClient(string webSocketUrl, ILogger<SnapcastJsonRpcClient> logger)
    {
        _webSocketUrl = webSocketUrl;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            return;
        }

        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        await _webSocket.ConnectAsync(new Uri(_webSocketUrl), _cancellationTokenSource.Token);
        LogConnected(_webSocketUrl);

        _receiveTask = Task.Run(HandleIncomingMessages, _cancellationTokenSource.Token);
    }

    public async Task<T> SendRequestAsync<T>(string method, object? parameters = null)
    {
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
                _cancellationTokenSource.Token);

            LogRequestSent(method, requestId);

            var response = await tcs.Task;
            return JsonSerializer.Deserialize<T>(response.GetRawText())!;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private async Task HandleIncomingMessages()
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket!.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource.Token);

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
        catch (Exception ex)
        {
            LogMessageHandlingError(ex);
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

        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
        }

        if (_receiveTask != null)
        {
            await _receiveTask;
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
    }

    // LoggerMessage patterns
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Connected to Snapcast WebSocket: {Url}")]
    private partial void LogConnected(string url);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug, Message = "Sent request: {Method} with ID: {RequestId}")]
    private partial void LogRequestSent(string method, string requestId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "Error in message handling loop")]
    private partial void LogMessageHandlingError(Exception ex);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Debug, Message = "Received notification: {Method}")]
    private partial void LogNotificationReceived(string method);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Error, Message = "Error processing message: {Message}")]
    private partial void LogMessageProcessingError(Exception ex, string message);
}
