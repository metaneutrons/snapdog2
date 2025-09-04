using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc;

public class SnapcastJsonRpcClient : IDisposable
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
        _logger.LogInformation("Connected to Snapcast WebSocket: {Url}", _webSocketUrl);

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

            _logger.LogDebug("Sent request: {Method} with ID: {RequestId}", method, requestId);

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

                        await ProcessMessage(message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message handling loop");
        }
    }

    private async Task ProcessMessage(string message)
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
                        tcs.SetResult(result);
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
                var parameters = root.TryGetProperty("params", out var paramsElement) ? paramsElement : default;

                _logger.LogDebug("Received notification: {Method}", method);
                NotificationReceived?.Invoke(method, parameters);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", message);
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
}
