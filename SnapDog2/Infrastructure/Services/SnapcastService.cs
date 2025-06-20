using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Resilience;
using SnapDog2.Infrastructure.Services.Models;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Implementation of Snapcast server communication and control operations.
/// Provides methods for monitoring server status, managing groups and clients,
/// and controlling audio streaming within the Snapcast ecosystem using JSON-RPC over TCP.
/// </summary>
public class SnapcastService : ISnapcastService, IDisposable, IAsyncDisposable
{
    private readonly SnapcastConfiguration _config;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger<SnapcastService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private int _requestId;
    private bool _disposed;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapcastService"/> class.
    /// </summary>
    /// <param name="config">The Snapcast configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public SnapcastService(IOptions<SnapcastConfiguration> config, ILogger<SnapcastService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _resiliencePolicy = PolicyFactory.CreateFromConfiguration(
            retryAttempts: 3,
            circuitBreakerThreshold: 3,
            circuitBreakerDuration: TimeSpan.FromSeconds(_config.ReconnectIntervalSeconds),
            defaultTimeout: TimeSpan.FromSeconds(_config.TimeoutSeconds),
            logger: _logger
        );

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        _logger.LogDebug(
            "SnapcastService initialized for {Host}:{Port} with timeout {Timeout}s",
            _config.Host,
            _config.Port,
            _config.TimeoutSeconds
        );
    }

    /// <summary>
    /// Checks if the Snapcast server is available and responding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if server is available, false otherwise</returns>
    public async Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Checking Snapcast server availability at {Host}:{Port}", _config.Host, _config.Port);

            await EnsureConnectedAsync(cancellationToken);

            // Try to get server status as a health check
            var status = await GetServerStatusAsync(cancellationToken);
            var isAvailable = !string.IsNullOrEmpty(status);

            _logger.LogDebug("Snapcast server availability check result: {IsAvailable}", isAvailable);
            return isAvailable;
        }
        catch (OperationCanceledException) // Allow OperationCanceledException to propagate
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Snapcast server availability check failed");
            return false;
        }
    }

    /// <summary>
    /// Retrieves the current status of the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Server status information as JSON string</returns>
    public async Task<string> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Retrieving Snapcast server status");

                await EnsureConnectedAsync(cancellationToken);

                var request = new SnapcastJsonRpcRequest
                {
                    Id = Interlocked.Increment(ref _requestId),
                    Method = "Server.GetStatus",
                };

                var response = await SendRequestAsync<SnapcastServerStatus>(request, cancellationToken);
                var jsonResult = JsonSerializer.Serialize(response, _jsonOptions);

                _logger.LogDebug("Successfully retrieved Snapcast server status");
                return jsonResult;
            },
            "GetServerStatus",
            cancellationToken
        );
    }

    /// <summary>
    /// Retrieves all groups configured on the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of group identifiers</returns>
    public async Task<IEnumerable<string>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Retrieving Snapcast groups");

                var statusJson = await GetServerStatusAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<SnapcastServerStatus>(statusJson, _jsonOptions);

                var groupIds = status?.Server?.Groups?.Select(g => g.Id) ?? Enumerable.Empty<string>();
                var groupList = groupIds.ToList();

                _logger.LogDebug("Retrieved {GroupCount} Snapcast groups", groupList.Count);
                return groupList;
            },
            "GetGroups",
            cancellationToken
        );
    }

    /// <summary>
    /// Retrieves all clients connected to the Snapcast server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of client identifiers</returns>
    public async Task<IEnumerable<string>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Retrieving Snapcast clients");

                var statusJson = await GetServerStatusAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<SnapcastServerStatus>(statusJson, _jsonOptions);

                var clientIds =
                    status?.Server?.Groups?.SelectMany(g => g.Clients).Select(c => c.Id) ?? Enumerable.Empty<string>();
                var clientList = clientIds.ToList();

                _logger.LogDebug("Retrieved {ClientCount} Snapcast clients", clientList.Count);
                return clientList;
            },
            "GetClients",
            cancellationToken
        );
    }

    /// <summary>
    /// Sets the volume level for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was successful, false otherwise</returns>
    public async Task<bool> SetClientVolumeAsync(
        string clientId,
        int volume,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client ID cannot be null or whitespace.", nameof(clientId));
        }

        if (volume < 0 || volume > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100");
        }

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Setting volume for client {ClientId} to {Volume}%", clientId, volume);

                await EnsureConnectedAsync(cancellationToken);

                var request = new SnapcastJsonRpcRequest
                {
                    Id = Interlocked.Increment(ref _requestId),
                    Method = "Client.SetVolume",
                    Params = new { id = clientId, volume = new { percent = volume } },
                };

                var response = await SendRequestAsync<object>(request, cancellationToken);
                var success = response != null;

                _logger.LogDebug(
                    "Set volume for client {ClientId} to {Volume}%: {Success}",
                    clientId,
                    volume,
                    success ? "Success" : "Failed"
                );

                return success;
            },
            $"SetClientVolume({clientId}, {volume})",
            cancellationToken
        );
    }

    /// <summary>
    /// Sets the mute state for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client</param>
    /// <param name="muted">True to mute, false to unmute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was successful, false otherwise</returns>
    public async Task<bool> SetClientMuteAsync(
        string clientId,
        bool muted,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(clientId);

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Setting mute state for client {ClientId} to {Muted}", clientId, muted);

                await EnsureConnectedAsync(cancellationToken);

                var request = new SnapcastJsonRpcRequest
                {
                    Id = Interlocked.Increment(ref _requestId),
                    Method = "Client.SetVolume",
                    Params = new { id = clientId, volume = new { muted } },
                };

                var response = await SendRequestAsync<object>(request, cancellationToken);
                var success = response != null;

                _logger.LogDebug(
                    "Set mute state for client {ClientId} to {Muted}: {Success}",
                    clientId,
                    muted,
                    success ? "Success" : "Failed"
                );

                return success;
            },
            $"SetClientMute({clientId}, {muted})",
            cancellationToken
        );
    }

    /// <summary>
    /// Assigns a specific stream to a group.
    /// </summary>
    /// <param name="groupId">Unique identifier of the group</param>
    /// <param name="streamId">Unique identifier of the stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation was successful, false otherwise</returns>
    public async Task<bool> SetGroupStreamAsync(
        string groupId,
        string streamId,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new ArgumentException("Group ID cannot be null or whitespace.", nameof(groupId));
        }

        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Setting stream {StreamId} for group {GroupId}", streamId, groupId);

                await EnsureConnectedAsync(cancellationToken);

                var request = new SnapcastJsonRpcRequest
                {
                    Id = Interlocked.Increment(ref _requestId),
                    Method = "Group.SetStream",
                    Params = new { id = groupId, stream_id = streamId },
                };

                var response = await SendRequestAsync<object>(request, cancellationToken);
                var success = response != null;

                _logger.LogDebug(
                    "Set stream {StreamId} for group {GroupId}: {Success}",
                    streamId,
                    groupId,
                    success ? "Success" : "Failed"
                );

                return success;
            },
            $"SetGroupStream({groupId}, {streamId})",
            cancellationToken
        );
    }

    /// <summary>
    /// Ensures a connection to the Snapcast server is established.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_tcpClient?.Connected == true && _networkStream != null)
            {
                return;
            }

            await DisconnectAsync();

            _logger.LogDebug("Connecting to Snapcast server at {Host}:{Port}", _config.Host, _config.Port);

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_config.Host, _config.Port, cancellationToken);
            _networkStream = _tcpClient.GetStream();

            _logger.LogInformation("Connected to Snapcast server at {Host}:{Port}", _config.Host, _config.Port);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Sends a JSON-RPC request to the Snapcast server and returns the response.
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response object</returns>
    private async Task<T?> SendRequestAsync<T>(SnapcastJsonRpcRequest request, CancellationToken cancellationToken)
        where T : class
    {
        if (_networkStream == null)
        {
            throw new InvalidOperationException("Not connected to Snapcast server");
        }

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\r\n");

        _logger.LogTrace("Sending Snapcast request: {Request}", requestJson);

        await _networkStream.WriteAsync(requestBytes, cancellationToken);

        // Read response
        var buffer = new byte[8192];
        var responseBuilder = new StringBuilder();
        var timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            while (!timeoutCts.Token.IsCancellationRequested)
            {
                var bytesRead = await _networkStream.ReadAsync(buffer, timeoutCts.Token);
                if (bytesRead == 0)
                {
                    break;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                responseBuilder.Append(chunk);

                // Check if we have a complete JSON response
                var responseText = responseBuilder.ToString();
                if (responseText.Contains('\n'))
                {
                    var lines = responseText.Split('\n');
                    var completeLine = lines[0];

                    _logger.LogTrace("Received Snapcast response: {Response}", completeLine);

                    var response = JsonSerializer.Deserialize<SnapcastJsonRpcResponse<T>>(completeLine, _jsonOptions);

                    if (response?.Error != null)
                    {
                        _logger.LogError(
                            "Snapcast server returned error {Code}: {Message}",
                            response.Error.Code,
                            response.Error.Message
                        );
                        throw new InvalidOperationException(
                            $"Snapcast error {response.Error.Code}: {response.Error.Message}"
                        );
                    }

                    return response?.Result;
                }
            }
        }
        finally
        {
            timeoutCts.Dispose();
        }

        throw new TimeoutException($"Snapcast request timed out after {timeout.TotalSeconds} seconds");
    }

    /// <summary>
    /// Executes an operation with the configured resilience strategy.
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    private async Task<T> ExecuteWithResilienceAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Executing Snapcast operation: {OperationName}", operationName);

            var result = await _resiliencePolicy.ExecuteAsync(async () => await operation());

            _logger.LogDebug("Snapcast operation {OperationName} completed successfully", operationName);
            return result;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Snapcast operation {OperationName} was cancelled", operationName);
            throw new OperationCanceledException("Operation was cancelled", ex, cancellationToken);
        }
        catch (Polly.Timeout.TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Snapcast operation {OperationName} timed out", operationName);

            // Disconnect on timeout to force reconnection on next operation
            if (_config.AutoReconnect)
            {
                await DisconnectAsync();
            }

            throw new InvalidOperationException($"Snapcast server operation timed out: {operationName}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Snapcast operation {OperationName} failed after resilience policies", operationName);

            // Disconnect on error to force reconnection on next operation
            if (_config.AutoReconnect)
            {
                await DisconnectAsync();
            }

            // Convert connection-related exceptions to InvalidOperationException
            if (ex is SocketException || ex.Message.Contains("unreachable") || ex.Message.Contains("connection"))
            {
                throw new InvalidOperationException($"Snapcast server is unreachable: {operationName}", ex);
            }

            throw;
        }
    }

    /// <summary>
    /// Disconnects from the Snapcast server.
    /// </summary>
    private async Task DisconnectAsync()
    {
        try
        {
            if (_networkStream != null)
            {
                await _networkStream.DisposeAsync();
                _networkStream = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }

            _logger.LogDebug("Disconnected from Snapcast server");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Snapcast server disconnection");
        }
    }

    /// <summary>
    /// Throws ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SnapcastService));
        }
    }

    /// <summary>
    /// Disposes of the Snapcast service resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes of the Snapcast service resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the Snapcast service resources.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _connectionSemaphore?.Dispose();
            _disposed = true;
            _logger.LogDebug("SnapcastService disposed");
        }
    }

    /// <summary>
    /// Core async disposal logic.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            await DisconnectAsync();
            _connectionSemaphore?.Dispose();
            _disposed = true;
            _logger.LogDebug("SnapcastService disposed asynchronously");
        }
    }
}
