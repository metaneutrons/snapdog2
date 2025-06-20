using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Resilience;
using KnxCore = Knx.Falcon;
using KnxSdk = Knx.Falcon.Sdk;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Implementation of KNX/EIB bus communication and control operations using FalconSDK.
/// Provides methods for connecting to KNX gateways, reading and writing group values,
/// and handling KNX bus events within the building automation system.
/// </summary>
public class KnxService : IKnxService, IDisposable, IAsyncDisposable
{
    private readonly KnxConfiguration _config;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger<KnxService> _logger;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private KnxSdk.KnxBus? _knxBus;
    private bool _disposed;
    private readonly Dictionary<string, TaskCompletionSource<byte[]?>> _pendingReads = new();
    private readonly HashSet<string> _subscribedAddresses = new();

    /// <summary>
    /// Event raised when a group value is received from a subscribed address.
    /// </summary>
    public event EventHandler<KnxGroupValueEventArgs>? GroupValueReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxService"/> class.
    /// </summary>
    /// <param name="config">The KNX configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public KnxService(IOptions<KnxConfiguration> config, ILogger<KnxService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _resiliencePolicy = PolicyFactory.CreateFromConfiguration(
            retryAttempts: 3,
            circuitBreakerThreshold: 3,
            circuitBreakerDuration: TimeSpan.FromSeconds(30),
            defaultTimeout: TimeSpan.FromSeconds(_config.TimeoutSeconds),
            logger: _logger
        );
    }

    /// <summary>
    /// Establishes connection to the KNX gateway.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection was successful, false otherwise</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KnxService));
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_knxBus?.ConnectionState == KnxCore.BusConnectionState.Connected)
            {
                _logger.LogDebug("KNX bus is already connected");
                return true;
            }

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                try
                {
                    _logger.LogInformation(
                        "Connecting to KNX gateway at {Gateway}:{Port}",
                        _config.Gateway,
                        _config.Port
                    );

                    // Create connection string for IP tunneling
                    var connectionString = $"Type=tunneling;Host={_config.Gateway};Port={_config.Port}"; // Changed "Tunneling" to "tunneling"

                    _knxBus = new KnxSdk.KnxBus(connectionString);

                    // Subscribe to group message events
                    _knxBus.GroupMessageReceived += OnGroupMessageReceived;

                    // Connect to the KNX bus
                    await _knxBus.ConnectAsync(cancellationToken);

                    if (_knxBus.ConnectionState == KnxCore.BusConnectionState.Connected)
                    {
                        _logger.LogInformation("Successfully connected to KNX gateway");
                        return true;
                    }

                    _logger.LogWarning(
                        "Failed to connect to KNX gateway - connection state: {State}",
                        _knxBus.ConnectionState
                    );
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error connecting to KNX gateway at {Gateway}:{Port}",
                        _config.Gateway,
                        _config.Port
                    );
                    throw;
                }
            });
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Disconnects from the KNX gateway.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_knxBus?.ConnectionState == KnxCore.BusConnectionState.Connected)
            {
                _logger.LogInformation("Disconnecting from KNX gateway");

                // Unsubscribe from events
                _knxBus.GroupMessageReceived -= OnGroupMessageReceived;

                // Dispose the bus (which disconnects)
                if (_knxBus != null) // Added null check
                {
                    await _knxBus.DisposeAsync();
                }

                _logger.LogInformation("Disconnected from KNX gateway");
            }

            // Clear subscriptions and pending reads
            _subscribedAddresses.Clear();
            _pendingReads.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from KNX gateway");
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Writes a value to a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to write to</param>
    /// <param name="value">The value to write as byte array</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if write operation was successful, false otherwise</returns>
    public async Task<bool> WriteGroupValueAsync(
        SnapDog2.Core.Configuration.KnxAddress? address,
        byte[] value,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KnxService));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            throw new ArgumentException("Value cannot be an empty byte array.", nameof(value));
        }

        const int MaxKnxPayloadSize = 15; // Typical safe max for a single telegram payload
        if (value.Length > MaxKnxPayloadSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Value size ({value.Length} bytes) exceeds maximum KNX payload size ({MaxKnxPayloadSize} bytes)."
            );
        }

        if (!await EnsureConnectedAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot write group value: KNX client is not connected.");
            throw new InvalidOperationException("KNX client is not connected. Call ConnectAsync first.");
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            try
            {
                _logger.LogDebug(
                    "Writing value to KNX group address {Address}: {Value}",
                    address,
                    Convert.ToHexString(value)
                );

                // Convert our KnxAddress to Falcon GroupAddress
                var groupAddress = KnxCore.GroupAddress.Parse(address.Value.ToString());

                // Create group value and write it
                var groupValue = new KnxCore.GroupValue(value);
                await _knxBus!.WriteGroupValueAsync(
                    groupAddress,
                    groupValue,
                    KnxCore.MessagePriority.High,
                    cancellationToken
                );

                _logger.LogDebug("Successfully wrote value to KNX group address {Address}", address);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing value to KNX group address {Address}", address);
                throw;
            }
        });
    }

    /// <summary>
    /// Reads a value from a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The value read as byte array, or null if read failed</returns>
    public async Task<byte[]?> ReadGroupValueAsync(
        SnapDog2.Core.Configuration.KnxAddress? address,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KnxService));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (!await EnsureConnectedAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot read group value: KNX client is not connected.");
            throw new InvalidOperationException("KNX client is not connected. Call ConnectAsync first.");
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            try
            {
                _logger.LogDebug("Reading value from KNX group address {Address}", address);

                // Convert our KnxAddress to Falcon GroupAddress
                var groupAddress = KnxCore.GroupAddress.Parse(address.Value.ToString());

                // Create a task completion source for this read operation
                var addressKey = address.Value.ToString();
                var tcs = new TaskCompletionSource<byte[]?>();
                _pendingReads[addressKey] = tcs;

                try
                {
                    // Send read request
                    var timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
                    var groupValue = await _knxBus!.ReadGroupValueAsync(
                        groupAddress,
                        timeout,
                        KnxCore.MessagePriority.High,
                        cancellationToken
                    );

                    var result = groupValue?.Value ?? null;

                    _logger.LogDebug(
                        "Successfully read value from KNX group address {Address}: {Value}",
                        address,
                        result != null ? Convert.ToHexString(result) : "null"
                    );

                    return result;
                }
                finally
                {
                    _pendingReads.Remove(addressKey);
                }
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Reading value from KNX group address {Address} was cancelled", address);
                throw new OperationCanceledException("Operation was cancelled", ex, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout reading value from KNX group address {Address}", address);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading value from KNX group address {Address}", address);
                throw;
            }
        });
    }

    /// <summary>
    /// Subscribes to value changes on a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to monitor</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if subscription was successful, false otherwise</returns>
    public async Task<bool> SubscribeToGroupAsync(
        SnapDog2.Core.Configuration.KnxAddress? address,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KnxService));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (!await EnsureConnectedAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot subscribe to group: KNX client is not connected.");
            throw new InvalidOperationException("KNX client is not connected. Call ConnectAsync first.");
        }

        try
        {
            var addressKey = address.Value.ToString();
            if (_subscribedAddresses.Contains(addressKey))
            {
                _logger.LogDebug("Already subscribed to KNX group address {Address}", address);
                return true;
            }

            _subscribedAddresses.Add(addressKey);
            _logger.LogInformation("Subscribed to KNX group address {Address}", address);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to KNX group address {Address}", address);
            return false;
        }
    }

    /// <summary>
    /// Unsubscribes from value changes on a specific KNX group address.
    /// </summary>
    /// <param name="address">The KNX group address to stop monitoring</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unsubscription was successful, false otherwise</returns>
    public async Task<bool> UnsubscribeFromGroupAsync(
        SnapDog2.Core.Configuration.KnxAddress? address,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KnxService));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (!await EnsureConnectedAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot unsubscribe from group: KNX client is not connected.");
            throw new InvalidOperationException("KNX client is not connected. Call ConnectAsync first.");
        }

        // await Task.CompletedTask; // No longer needed if EnsureConnectedAsync makes it truly async

        try
        {
            var addressKey = address.Value.ToString();
            if (_subscribedAddresses.Remove(addressKey))
            {
                _logger.LogInformation("Unsubscribed from KNX group address {Address}", address);
                return true;
            }

            _logger.LogDebug("Was not subscribed to KNX group address {Address}", address);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from KNX group address {Address}", address);
            return false;
        }
    }

    /// <summary>
    /// Ensures the KNX bus is connected, attempting to connect if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connected, false otherwise</returns>
    private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_knxBus?.ConnectionState == KnxCore.BusConnectionState.Connected)
        {
            return true;
        }

        if (_config.AutoReconnect)
        {
            _logger.LogInformation("KNX bus not connected, attempting to reconnect");
            return await ConnectAsync(cancellationToken);
        }

        _logger.LogWarning("KNX bus not connected and auto-reconnect is disabled");
        return false;
    }

    /// <summary>
    /// Handles incoming group messages from the KNX bus.
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="e">The group event arguments</param>
    private void OnGroupMessageReceived(object? sender, KnxCore.GroupEventArgs e)
    {
        try
        {
            var addressString = e.DestinationAddress.ToString();
            var value = e.Value?.Value ?? Array.Empty<byte>();

            _logger.LogDebug(
                "Received KNX group message from {Address}: {Value}",
                addressString,
                Convert.ToHexString(value)
            );

            // Handle pending read requests
            if (_pendingReads.TryGetValue(addressString, out var tcs))
            {
                tcs.SetResult(value);
            }

            // Notify subscribers if this address is subscribed
            if (_subscribedAddresses.Contains(addressString))
            {
                if (SnapDog2.Core.Configuration.KnxAddress.TryParse(addressString, out var knxAddress))
                {
                    var eventArgs = new KnxGroupValueEventArgs
                    {
                        Address = knxAddress,
                        Value = value,
                        ReceivedAt = DateTime.UtcNow,
                    };

                    GroupValueReceived?.Invoke(this, eventArgs);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling KNX group message");
        }
    }

    /// <summary>
    /// Disposes the KNX service and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KNX service disposal");
        }

        _knxBus?.Dispose();
        _connectionSemaphore.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Asynchronously disposes the KNX service and releases resources.
    /// </summary>
    /// <returns>A task representing the disposal operation</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KNX service async disposal");
        }

        _knxBus?.Dispose();
        _connectionSemaphore.Dispose();
        _disposed = true;
    }
}
