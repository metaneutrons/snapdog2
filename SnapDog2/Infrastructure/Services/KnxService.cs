namespace SnapDog2.Infrastructure.Services;

using System.Collections.Concurrent;
using System.Linq;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Helpers;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;
using SnapDog2.Server.Features.Shared.Notifications;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Enterprise-grade KNX integration service using Knx.Falcon.Sdk.
/// Provides bi-directional KNX communication with automatic reconnection and command mapping.
/// Updated to use IServiceProvider to resolve scoped IMediator.
/// </summary>
public partial class KnxService : IKnxService, INotificationHandler<StatusChangedNotification>
{
    private readonly KnxConfig _config;
    private readonly List<ZoneConfig> _zones;
    private readonly List<ClientConfig> _clients;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnxService> _logger;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;
    private readonly ConcurrentDictionary<string, string> _groupAddressCache;
    private readonly Timer _reconnectTimer;
    private readonly SemaphoreSlim _connectionSemaphore;

    private KnxBus? _knxBus;
    private bool _isInitialized;
    private bool _disposed;

    public KnxService(
        IOptions<SnapDogConfiguration> configuration,
        IServiceProvider serviceProvider,
        ILogger<KnxService> logger
    )
    {
        var config = configuration.Value;
        _config = config.Services.Knx;
        _zones = config.Zones;
        _clients = config.Clients;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _groupAddressCache = new ConcurrentDictionary<string, string>();
        _connectionSemaphore = new SemaphoreSlim(1, 1);

        // Configure resilience policies
        _connectionPolicy = CreateConnectionPolicy();
        _operationPolicy = CreateOperationPolicy();

        // Initialize reconnect timer (disabled initially)
        _reconnectTimer = new Timer(OnReconnectTimer, null, Timeout.Infinite, Timeout.Infinite);

        LogServiceCreated(_config.Gateway, _config.Port, _config.Enabled);
    }

    /// <inheritdoc />
    public bool IsConnected => _knxBus?.ConnectionState == BusConnectionState.Connected;

    /// <inheritdoc />
    public ServiceStatus Status =>
        _isInitialized switch
        {
            false => ServiceStatus.Stopped,
            true when IsConnected => ServiceStatus.Running,
            true => ServiceStatus.Error,
        };

    /// <inheritdoc />
    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            LogServiceDisabled();
            return Result.Success();
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                LogAlreadyInitialized();
                return Result.Success();
            }

            LogInitializationStarted();

            // Log first attempt before Polly execution
            var config = ResiliencePolicyFactory.ValidateAndNormalize(_config.Resilience.Connection);
            LogConnectionRetryAttempt(
                _config.Gateway ?? "USB",
                _config.Port,
                1,
                config.MaxRetries + 1,
                "Initial attempt"
            );

            try
            {
                var result = await _connectionPolicy.ExecuteAsync(
                    async (ct) =>
                    {
                        return await ConnectToKnxBusAsync(ct);
                    },
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    _isInitialized = true;
                    LogInitializationCompleted();
                }
                else
                {
                    LogInitializationFailed(result.ErrorMessage ?? "Unknown error");
                    StartReconnectTimer();
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"KNX connection failed: {ex.Message}";
                LogInitializationFailed(errorMessage);
                StartReconnectTimer();
                return Result.Failure(errorMessage);
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result> StopAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            LogStoppingService();

            StopReconnectTimer();

            if (_knxBus != null)
            {
                try
                {
                    if (_knxBus.ConnectionState == BusConnectionState.Connected)
                    {
                        await _knxBus.DisposeAsync();
                    }
                }
                catch (Exception ex)
                {
                    LogDisconnectionError(ex);
                }
                finally
                {
                    _knxBus.Dispose();
                    _knxBus = null;
                }
            }

            _isInitialized = false;
            LogServiceStopped();
            return Result.Success();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result> SendStatusAsync(
        string statusId,
        int targetId,
        object value,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected)
        {
            LogNotConnected("SendStatusAsync");
            return Result.Failure("KNX service is not connected");
        }

        try
        {
            var groupAddress = GetStatusGroupAddress(statusId, targetId);
            if (string.IsNullOrEmpty(groupAddress))
            {
                LogGroupAddressNotFound(statusId, targetId);
                return Result.Failure($"No KNX group address configured for status {statusId} on target {targetId}");
            }

            return await WriteToKnxAsync(groupAddress, value, cancellationToken);
        }
        catch (Exception ex)
        {
            LogSendStatusError(statusId, targetId, ex);
            return Result.Failure($"Failed to send status: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteGroupValueAsync(
        string groupAddress,
        object value,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected)
        {
            LogNotConnected("WriteGroupValueAsync");
            return Result.Failure("KNX service is not connected");
        }

        return await WriteToKnxAsync(groupAddress, value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<object>> ReadGroupValueAsync(
        string groupAddress,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConnected)
        {
            LogNotConnected("ReadGroupValueAsync");
            return Result<object>.Failure("KNX service is not connected");
        }

        try
        {
            var result = await _operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var ga = new GroupAddress(groupAddress);
                    var value = await _knxBus!.ReadGroupValueAsync(ga);
                    return value;
                },
                cancellationToken
            );

            LogGroupValueRead(groupAddress, result);
            return Result<object>.Success(result);
        }
        catch (Exception ex)
        {
            LogReadGroupValueError(groupAddress, ex);
            return Result<object>.Failure($"Failed to read group value: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task Handle(StatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConnected || !_config.Enabled)
        {
            return;
        }

        try
        {
            if (int.TryParse(notification.TargetId, out var targetId))
            {
                await SendStatusAsync(notification.StatusType, targetId, notification.Value, cancellationToken);
            }
            else
            {
                LogInvalidTargetId(notification.StatusType, notification.TargetId);
            }
        }
        catch (Exception ex)
        {
            if (int.TryParse(notification.TargetId, out var targetIdInt))
            {
                LogStatusNotificationError(notification.StatusType, targetIdInt, ex);
            }
            else
            {
                LogStatusNotificationError(notification.StatusType, -1, ex);
            }
        }
    }

    private async Task<Result> ConnectToKnxBusAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create connector parameters based on configuration
            var connectorParams = CreateConnectorParameters();
            if (connectorParams == null)
            {
                throw new InvalidOperationException("Failed to create KNX connector parameters");
            }

            // Create and configure KNX bus
            _knxBus = new KnxBus(connectorParams);

            // Subscribe to events before connecting
            _knxBus.GroupMessageReceived += OnGroupMessageReceived;

            // Connect to KNX bus - this should throw an exception if it fails
            await _knxBus.ConnectAsync();

            if (_knxBus.ConnectionState != BusConnectionState.Connected)
            {
                throw new InvalidOperationException($"KNX connection failed - state: {_knxBus.ConnectionState}");
            }

            LogConnectionEstablished(_config.Gateway ?? "USB", _config.Port);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // For KNX connection errors, only log the message without stack trace to reduce noise
            if (ex is Knx.Falcon.KnxIpConnectorException)
            {
                LogConnectionErrorMessage(ex.Message);
            }
            else
            {
                LogConnectionError(ex);
            }

            // Re-throw the exception so Polly can handle retries
            throw;
        }
    }

    private ConnectorParameters? CreateConnectorParameters()
    {
        try
        {
            return _config.ConnectionType switch
            {
                KnxConnectionType.Tunnel => CreateTunnelingConnectorParameters(),
                KnxConnectionType.Router => CreateRoutingConnectorParameters(),
                KnxConnectionType.Usb => CreateUsbConnectorParameters(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(_config.ConnectionType),
                    _config.ConnectionType,
                    "Unsupported KNX connection type"
                ),
            };
        }
        catch (Exception ex)
        {
            LogConnectorParametersError(ex);
            return null;
        }
    }

    private ConnectorParameters? CreateTunnelingConnectorParameters()
    {
        if (string.IsNullOrEmpty(_config.Gateway))
        {
            LogGatewayRequired("IP Tunneling");
            return null;
        }

        LogUsingIpTunneling(_config.Gateway, _config.Port);
        return new IpTunnelingConnectorParameters(_config.Gateway, _config.Port);
    }

    private ConnectorParameters? CreateRoutingConnectorParameters()
    {
        if (string.IsNullOrEmpty(_config.Gateway))
        {
            LogGatewayRequired("IP Routing");
            return null;
        }

        try
        {
            // Try to parse as IP address first
            if (System.Net.IPAddress.TryParse(_config.Gateway, out var ipAddress))
            {
                LogUsingIpRouting(_config.Gateway);
                return new IpRoutingConnectorParameters(ipAddress);
            }

            // If not an IP address, resolve hostname to IP address
            var hostEntry = System.Net.Dns.GetHostEntry(_config.Gateway);
            var resolvedIp = hostEntry.AddressList.FirstOrDefault(addr =>
                addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            );

            if (resolvedIp == null)
            {
                LogGatewayRequired($"IP Routing (failed to resolve hostname '{_config.Gateway}' to IPv4 address)");
                return null;
            }

            LogUsingIpRouting($"{_config.Gateway} ({resolvedIp})");
            return new IpRoutingConnectorParameters(resolvedIp);
        }
        catch (Exception ex)
        {
            LogGatewayRequired($"IP Routing (error resolving '{_config.Gateway}': {ex.Message})");
            return null;
        }
    }

    private ConnectorParameters? CreateUsbConnectorParameters()
    {
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            LogNoUsbDevicesFound();
            return null;
        }

        LogUsingUsbDevice(usbDevices[0].ToString());
        return UsbConnectorParameters.FromDiscovery(usbDevices[0]);
    }

    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        try
        {
            var address = e.DestinationAddress.ToString();
            var value = e.Value;

            LogGroupValueReceived(address, value);

            // Map group address to command
            var command = MapGroupAddressToCommand(address, value);
            if (command != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteCommandAsync(command, CancellationToken.None);
                        LogCommandMapped(address, command.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        LogCommandExecutionError(command.GetType().Name, ex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            LogGroupValueProcessingError("unknown", ex);
        }
    }

    private async Task<Result> ExecuteCommandAsync(object command, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            return command switch
            {
                SetZoneVolumeCommand cmd =>
                    await GetHandler<Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetZoneMuteCommand cmd => await GetHandler<Server.Features.Zones.Handlers.SetZoneMuteCommandHandler>(
                        scope
                    )
                    .Handle(cmd, cancellationToken),
                PlayCommand cmd => await GetHandler<Server.Features.Zones.Handlers.PlayCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                PauseCommand cmd => await GetHandler<Server.Features.Zones.Handlers.PauseCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                StopCommand cmd => await GetHandler<Server.Features.Zones.Handlers.StopCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                NextTrackCommand cmd => await GetHandler<Server.Features.Zones.Handlers.NextTrackCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                PreviousTrackCommand cmd =>
                    await GetHandler<Server.Features.Zones.Handlers.PreviousTrackCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetClientVolumeCommand cmd =>
                    await GetHandler<Server.Features.Clients.Handlers.SetClientVolumeCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetClientMuteCommand cmd =>
                    await GetHandler<Server.Features.Clients.Handlers.SetClientMuteCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                _ => Result.Failure($"Unknown command type: {command.GetType().Name}"),
            };
        }
        catch (Exception ex)
        {
            LogCommandExecutionError(command.GetType().Name, ex);
            return Result.Failure($"Failed to execute command: {ex.Message}");
        }
    }

    private object? MapGroupAddressToCommand(string groupAddress, object value)
    {
        // Check zones for matching group addresses
        for (int i = 0; i < _zones.Count; i++)
        {
            var zone = _zones[i];
            var zoneId = i + 1; // 1-based zone ID

            if (!zone.Knx.Enabled)
                continue;

            var knxConfig = zone.Knx;

            // Volume commands
            if (groupAddress == knxConfig.Volume && value is int volumeValue)
            {
                return new SetZoneVolumeCommand
                {
                    ZoneId = zoneId,
                    Volume = volumeValue,
                    Source = CommandSource.Knx,
                };
            }

            // Mute commands
            if (groupAddress == knxConfig.Mute && value is bool muteValue)
            {
                return new SetZoneMuteCommand
                {
                    ZoneId = zoneId,
                    Enabled = muteValue,
                    Source = CommandSource.Knx,
                };
            }

            // Playback commands
            if (groupAddress == knxConfig.Play && value is bool playValue && playValue)
            {
                return new PlayCommand { ZoneId = zoneId, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.Pause && value is bool pauseValue && pauseValue)
            {
                return new PauseCommand { ZoneId = zoneId, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.Stop && value is bool stopValue && stopValue)
            {
                return new StopCommand { ZoneId = zoneId, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.TrackNext && value is bool nextValue && nextValue)
            {
                return new NextTrackCommand { ZoneId = zoneId, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.TrackPrevious && value is bool prevValue && prevValue)
            {
                return new PreviousTrackCommand { ZoneId = zoneId, Source = CommandSource.Knx };
            }
        }

        // Check clients for matching group addresses
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];
            var clientId = i + 1; // 1-based client ID

            if (!client.Knx.Enabled)
                continue;

            var knxConfig = client.Knx;

            // Client volume commands
            if (groupAddress == knxConfig.Volume && value is int clientVolumeValue)
            {
                return new SetClientVolumeCommand
                {
                    ClientId = clientId,
                    Volume = clientVolumeValue,
                    Source = CommandSource.Knx,
                };
            }

            // Client mute commands
            if (groupAddress == knxConfig.Mute && value is bool clientMuteValue)
            {
                return new SetClientMuteCommand
                {
                    ClientId = clientId,
                    Enabled = clientMuteValue,
                    Source = CommandSource.Knx,
                };
            }
        }

        return null;
    }

    private string? GetStatusGroupAddress(string statusId, int targetId)
    {
        // Check if it's a zone status (1-based ID)
        if (targetId > 0 && targetId <= _zones.Count)
        {
            var zone = _zones[targetId - 1]; // Convert to 0-based index
            if (zone.Knx.Enabled)
            {
                return statusId switch
                {
                    "VOLUME" => zone.Knx.VolumeStatus,
                    "MUTE" => zone.Knx.MuteStatus,
                    "PLAYING" => zone.Knx.ControlStatus,
                    _ => null,
                };
            }
        }

        // Check if it's a client status (1-based ID)
        if (targetId > 0 && targetId <= _clients.Count)
        {
            var client = _clients[targetId - 1]; // Convert to 0-based index
            if (client.Knx.Enabled)
            {
                return statusId switch
                {
                    "VOLUME" => client.Knx.VolumeStatus,
                    "MUTE" => client.Knx.MuteStatus,
                    "CONNECTED" => client.Knx.ConnectedStatus,
                    _ => null,
                };
            }
        }

        return null;
    }

    private async Task<Result> WriteToKnxAsync(string groupAddress, object value, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var ga = new GroupAddress(groupAddress);

                    // Convert value to appropriate type for GroupValue
                    GroupValue groupValue = value switch
                    {
                        bool boolValue => new GroupValue(boolValue),
                        byte byteValue => new GroupValue(byteValue),
                        int intValue when intValue >= 0 && intValue <= 255 => new GroupValue((byte)intValue),
                        _ => throw new ArgumentException($"Unsupported value type: {value?.GetType()}"),
                    };

                    await _knxBus!.WriteGroupValueAsync(ga, groupValue);
                    return Result.Success();
                },
                cancellationToken
            );

            LogGroupValueWritten(groupAddress, value);
            return result;
        }
        catch (Exception ex)
        {
            LogWriteGroupValueError(groupAddress, value, ex);
            return Result.Failure($"Failed to write group value: {ex.Message}");
        }
    }

    private ResiliencePipeline CreateConnectionPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(_config.Resilience.Connection);

        var builder = new ResiliencePipelineBuilder();

        // Add retry policy with logging
        if (validatedConfig.MaxRetries > 0)
        {
            builder.AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = validatedConfig.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(validatedConfig.RetryDelayMs),
                    BackoffType = validatedConfig.BackoffType?.ToLowerInvariant() switch
                    {
                        "linear" => DelayBackoffType.Linear,
                        "constant" => DelayBackoffType.Constant,
                        _ => DelayBackoffType.Exponential,
                    },
                    UseJitter = validatedConfig.UseJitter,
                    // Explicitly handle all exceptions - KNX connection issues should be retried
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        LogConnectionRetryAttempt(
                            _config.Gateway ?? "USB",
                            _config.Port,
                            args.AttemptNumber + 1,
                            validatedConfig.MaxRetries + 1,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            );
        }

        // Add timeout policy
        if (validatedConfig.TimeoutSeconds > 0)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(validatedConfig.TimeoutSeconds));
        }

        return builder.Build();
    }

    private ResiliencePipeline CreateOperationPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(_config.Resilience.Operation);
        return ResiliencePolicyFactory.CreatePipeline(validatedConfig, "KNX-Operation");
    }

    private void StartReconnectTimer()
    {
        if (_config.AutoReconnect)
        {
            var interval = TimeSpan.FromSeconds(30); // Reconnect every 30 seconds
            _reconnectTimer.Change(interval, interval);
            LogReconnectTimerStarted();
        }
    }

    private void StopReconnectTimer()
    {
        _reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async void OnReconnectTimer(object? state)
    {
        if (!_isInitialized || IsConnected)
        {
            return;
        }

        LogAttemptingReconnection();
        var result = await InitializeAsync();
        if (result.IsSuccess)
        {
            StopReconnectTimer();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await StopAsync();

        _reconnectTimer.Dispose();
        _connectionSemaphore.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private T GetHandler<T>(IServiceScope scope)
        where T : class
    {
        var handler = scope.ServiceProvider.GetService<T>();
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler {typeof(T).Name} not found in DI container");
        }
        return handler;
    }

    #region Logging

    [LoggerMessage(
        8001,
        LogLevel.Debug,
        "KNX service created with gateway: {Gateway}, port: {Port}, enabled: {Enabled}"
    )]
    private partial void LogServiceCreated(string? gateway, int port, bool enabled);

    [LoggerMessage(8002, LogLevel.Information, "KNX service is disabled via configuration")]
    private partial void LogServiceDisabled();

    [LoggerMessage(8003, LogLevel.Debug, "KNX service already initialized")]
    private partial void LogAlreadyInitialized();

    [LoggerMessage(8004, LogLevel.Information, "🚀 Starting KNX service initialization")]
    private partial void LogInitializationStarted();

    [LoggerMessage(8005, LogLevel.Information, "KNX service initialization completed successfully")]
    private partial void LogInitializationCompleted();

    [LoggerMessage(8006, LogLevel.Error, "KNX service initialization failed: {Error}")]
    private partial void LogInitializationFailed(string error);

    [LoggerMessage(8007, LogLevel.Debug, "Stopping KNX service")]
    private partial void LogStoppingService();

    [LoggerMessage(8008, LogLevel.Debug, "KNX service stopped successfully")]
    private partial void LogServiceStopped();

    [LoggerMessage(8009, LogLevel.Warning, "Error during KNX disconnection")]
    private partial void LogDisconnectionError(Exception exception);

    [LoggerMessage(8010, LogLevel.Information, "KNX connection established to {Gateway}:{Port}")]
    private partial void LogConnectionEstablished(string gateway, int port);

    [LoggerMessage(8011, LogLevel.Error, "KNX connection error")]
    private partial void LogConnectionError(Exception exception);

    [LoggerMessage(8031, LogLevel.Error, "KNX connection error: {ErrorMessage}")]
    private partial void LogConnectionErrorMessage(string errorMessage);

    [LoggerMessage(
        8033,
        LogLevel.Information,
        "🚀 Attempting KNX connection to {Gateway}:{Port} (attempt {AttemptNumber}/{MaxAttempts}: {ErrorMessage})"
    )]
    private partial void LogConnectionRetryAttempt(
        string gateway,
        int port,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    [LoggerMessage(8012, LogLevel.Debug, "Using KNX IP tunneling connection to {Gateway}:{Port}")]
    private partial void LogUsingIpTunneling(string gateway, int port);

    [LoggerMessage(8013, LogLevel.Debug, "Using KNX IP routing connection to {Gateway}")]
    private partial void LogUsingIpRouting(string gateway);

    [LoggerMessage(8014, LogLevel.Debug, "Using KNX USB device: {Device}")]
    private partial void LogUsingUsbDevice(string device);

    [LoggerMessage(8015, LogLevel.Error, "Gateway address is required for {ConnectionType} connection")]
    private partial void LogGatewayRequired(string connectionType);

    [LoggerMessage(8014, LogLevel.Warning, "No KNX USB devices found")]
    private partial void LogNoUsbDevicesFound();

    [LoggerMessage(8015, LogLevel.Error, "Error creating KNX connector parameters")]
    private partial void LogConnectorParametersError(Exception exception);

    [LoggerMessage(8016, LogLevel.Debug, "KNX group value received: {GroupAddress} = {Value}")]
    private partial void LogGroupValueReceived(string groupAddress, object value);

    [LoggerMessage(8017, LogLevel.Debug, "KNX command mapped: {GroupAddress} -> {CommandType}")]
    private partial void LogCommandMapped(string groupAddress, string commandType);

    [LoggerMessage(8018, LogLevel.Error, "Error processing KNX group value from {GroupAddress}")]
    private partial void LogGroupValueProcessingError(string groupAddress, Exception exception);

    [LoggerMessage(8019, LogLevel.Warning, "KNX service not connected for operation: {Operation}")]
    private partial void LogNotConnected(string operation);

    [LoggerMessage(8020, LogLevel.Warning, "No KNX group address found for status {StatusId} on target {TargetId}")]
    private partial void LogGroupAddressNotFound(string statusId, int targetId);

    [LoggerMessage(8021, LogLevel.Error, "Error sending KNX status {StatusId} to target {TargetId}")]
    private partial void LogSendStatusError(string statusId, int targetId, Exception exception);

    [LoggerMessage(8022, LogLevel.Debug, "KNX group value written: {GroupAddress} = {Value}")]
    private partial void LogGroupValueWritten(string groupAddress, object value);

    [LoggerMessage(8023, LogLevel.Error, "Error writing KNX group value {GroupAddress} = {Value}")]
    private partial void LogWriteGroupValueError(string groupAddress, object value, Exception exception);

    [LoggerMessage(8024, LogLevel.Debug, "KNX group value read: {GroupAddress} = {Value}")]
    private partial void LogGroupValueRead(string groupAddress, object value);

    [LoggerMessage(8025, LogLevel.Error, "Error reading KNX group value from {GroupAddress}")]
    private partial void LogReadGroupValueError(string groupAddress, Exception exception);

    [LoggerMessage(8026, LogLevel.Error, "Error handling KNX status notification {StatusId} for target {TargetId}")]
    private partial void LogStatusNotificationError(string statusId, int targetId, Exception exception);

    [LoggerMessage(8027, LogLevel.Debug, "KNX reconnect timer started")]
    private partial void LogReconnectTimerStarted();

    [LoggerMessage(8028, LogLevel.Debug, "Attempting KNX reconnection")]
    private partial void LogAttemptingReconnection();

    [LoggerMessage(8029, LogLevel.Warning, "Invalid target ID '{TargetId}' for status '{StatusId}' - expected integer")]
    private partial void LogInvalidTargetId(string statusId, string targetId);

    [LoggerMessage(8030, LogLevel.Error, "Error executing KNX command {CommandType}")]
    private partial void LogCommandExecutionError(string commandType, Exception exception);

    #endregion
}
