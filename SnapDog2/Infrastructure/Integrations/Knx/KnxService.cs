namespace SnapDog2.Infrastructure.Integrations.Knx;

using System.Collections.Concurrent;
using System.Linq;
using Cortex.Mediator.Notifications;
using global::Knx.Falcon;
using global::Knx.Falcon.Configuration;
using global::Knx.Falcon.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Helpers;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Shared.Notifications;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

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
        this._config = config.Services.Knx;
        this._zones = config.Zones;
        this._clients = config.Clients;
        this._serviceProvider = serviceProvider;
        this._logger = logger;
        this._groupAddressCache = new ConcurrentDictionary<string, string>();
        this._connectionSemaphore = new SemaphoreSlim(1, 1);

        // Configure resilience policies
        this._connectionPolicy = this.CreateConnectionPolicy();
        this._operationPolicy = this.CreateOperationPolicy();

        // Initialize reconnect timer (disabled initially)
        this._reconnectTimer = new Timer(this.OnReconnectTimer, null, Timeout.Infinite, Timeout.Infinite);

        this.LogServiceCreated(this._config.Gateway, this._config.Port, this._config.Enabled);
    }

    /// <inheritdoc />
    public bool IsConnected => this._knxBus?.ConnectionState == BusConnectionState.Connected;

    /// <inheritdoc />
    public ServiceStatus Status =>
        this._isInitialized switch
        {
            false => ServiceStatus.Stopped,
            true when this.IsConnected => ServiceStatus.Running,
            true => ServiceStatus.Error,
        };

    /// <inheritdoc />
    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!this._config.Enabled)
        {
            this.LogServiceDisabled();
            return Result.Success();
        }

        await this._connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (this._isInitialized)
            {
                this.LogAlreadyInitialized();
                return Result.Success();
            }

            this.LogInitializationStarted();

            // Log first attempt before Polly execution
            var config = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);
            this.LogConnectionRetryAttempt(
                this._config.Gateway ?? "USB",
                this._config.Port,
                1,
                config.MaxRetries + 1,
                "Initial attempt"
            );

            try
            {
                var result = await this._connectionPolicy.ExecuteAsync(
                    async (ct) =>
                    {
                        return await this.ConnectToKnxBusAsync(ct);
                    },
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    this._isInitialized = true;
                    this.LogInitializationCompleted();
                }
                else
                {
                    this.LogInitializationFailed(result.ErrorMessage ?? "Unknown error");
                    this.StartReconnectTimer();
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"KNX connection failed: {ex.Message}";
                this.LogInitializationFailed(errorMessage);
                this.StartReconnectTimer();
                return Result.Failure(errorMessage);
            }
        }
        finally
        {
            this._connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result> StopAsync(CancellationToken cancellationToken = default)
    {
        await this._connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            this.LogStoppingService();

            this.StopReconnectTimer();

            if (this._knxBus != null)
            {
                try
                {
                    if (this._knxBus.ConnectionState == BusConnectionState.Connected)
                    {
                        await this._knxBus.DisposeAsync();
                    }
                }
                catch (Exception ex)
                {
                    this.LogDisconnectionError(ex);
                }
                finally
                {
                    this._knxBus.Dispose();
                    this._knxBus = null;
                }
            }

            this._isInitialized = false;
            this.LogServiceStopped();
            return Result.Success();
        }
        finally
        {
            this._connectionSemaphore.Release();
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
        if (!this.IsConnected)
        {
            this.LogNotConnected("SendStatusAsync");
            return Result.Failure("KNX service is not connected");
        }

        try
        {
            var groupAddress = this.GetStatusGroupAddress(statusId, targetId);
            if (string.IsNullOrEmpty(groupAddress))
            {
                this.LogGroupAddressNotFoundWithContext(statusId, targetId);
                return Result.Failure(
                    $"No KNX group address configured for status {statusId} on {GetTargetTypeDescription(statusId)} {targetId}"
                );
            }

            return await this.WriteToKnxAsync(groupAddress, value, cancellationToken);
        }
        catch (Exception ex)
        {
            this.LogSendStatusErrorWithContext(statusId, targetId, ex);
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
        if (!this.IsConnected)
        {
            this.LogNotConnected("WriteGroupValueAsync");
            return Result.Failure("KNX service is not connected");
        }

        return await this.WriteToKnxAsync(groupAddress, value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<object>> ReadGroupValueAsync(
        string groupAddress,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            this.LogNotConnected("ReadGroupValueAsync");
            return Result<object>.Failure("KNX service is not connected");
        }

        try
        {
            var result = await this._operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var ga = new GroupAddress(groupAddress);
                    var value = await this._knxBus!.ReadGroupValueAsync(ga);
                    return value;
                },
                cancellationToken
            );

            this.LogGroupValueRead(groupAddress, result);
            return Result<object>.Success(result);
        }
        catch (Exception ex)
        {
            this.LogReadGroupValueError(groupAddress, ex);
            return Result<object>.Failure($"Failed to read group value: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> PublishClientStatusAsync<T>(
        string clientIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            this.LogNotConnected("PublishClientStatusAsync");
            return Result.Failure("KNX service is not connected");
        }

        try
        {
            // Map event type to KNX status ID and convert payload to appropriate KNX value
            var (statusId, knxValue) = MapClientEventToKnxStatus(eventType, payload);
            if (statusId == null)
            {
                this.LogDebug($"No KNX mapping for client event type {eventType}");
                return Result.Success();
            }

            // For KNX, we need to convert client ID to integer if possible
            if (int.TryParse(clientIndex, out var clientIndexInt))
            {
                return await this.SendStatusAsync(statusId, clientIndexInt, knxValue, cancellationToken);
            }
            else
            {
                this.LogInvalidTargetId(statusId, clientIndex);
                return Result.Failure($"Invalid client ID for KNX: {clientIndex}");
            }
        }
        catch (Exception ex)
        {
            this.LogCommandExecutionError($"PublishClientStatus-{eventType}", ex);
            return Result.Failure($"Failed to publish client status: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> PublishZoneStatusAsync<T>(
        int zoneIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            this.LogNotConnected("PublishZoneStatusAsync");
            return Result.Failure("KNX service is not connected");
        }

        try
        {
            // Map event type to KNX status ID and convert payload to appropriate KNX value
            var (statusId, knxValue) = MapZoneEventToKnxStatus(eventType, payload);
            if (statusId == null)
            {
                this.LogDebug($"No KNX mapping for zone event type {eventType}");
                return Result.Success();
            }

            return await this.SendStatusAsync(statusId, zoneIndex, knxValue, cancellationToken);
        }
        catch (Exception ex)
        {
            this.LogCommandExecutionError($"PublishZoneStatus-{eventType}", ex);
            return Result.Failure($"Failed to publish zone status: {ex.Message}");
        }
    }

    /// <summary>
    /// Publishes global system status updates to KNX group addresses.
    /// Currently, KNX implementation does not support global status publishing
    /// as there are no global group addresses configured in the blueprint.
    /// </summary>
    public async Task<Result> PublishGlobalStatusAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        // KNX implementation does not currently support global status publishing
        // as per the command framework blueprint (Section 14.2.3 only covers MQTT)
        this.LogKnxGlobalStatusPublishingNotImplemented(eventType);
        return await Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task Handle(StatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (!this.IsConnected || !this._config.Enabled)
        {
            return;
        }

        try
        {
            if (int.TryParse(notification.TargetId, out var targetId))
            {
                await this.SendStatusAsync(notification.StatusType, targetId, notification.Value, cancellationToken);
            }
            else
            {
                this.LogInvalidTargetId(notification.StatusType, notification.TargetId);
            }
        }
        catch (Exception ex)
        {
            if (int.TryParse(notification.TargetId, out var targetIdInt))
            {
                this.LogStatusNotificationError(notification.StatusType, targetIdInt, ex);
            }
            else
            {
                this.LogStatusNotificationError(notification.StatusType, -1, ex);
            }
        }
    }

    private async Task<Result> ConnectToKnxBusAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create connector parameters based on configuration
            var connectorParams = this.CreateConnectorParameters();
            if (connectorParams == null)
            {
                throw new InvalidOperationException("Failed to create KNX connector parameters");
            }

            // Create and configure KNX bus
            this._knxBus = new KnxBus(connectorParams);

            // Subscribe to events before connecting
            this._knxBus.GroupMessageReceived += this.OnGroupMessageReceived;

            // Connect to KNX bus - this should throw an exception if it fails
            await this._knxBus.ConnectAsync();

            if (this._knxBus.ConnectionState != BusConnectionState.Connected)
            {
                throw new InvalidOperationException($"KNX connection failed - state: {this._knxBus.ConnectionState}");
            }

            this.LogConnectionEstablished(this._config.Gateway ?? "USB", this._config.Port);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // For KNX connection errors, only log the message without stack trace to reduce noise
            if (ex is global::Knx.Falcon.KnxIpConnectorException)
            {
                this.LogConnectionErrorMessage(ex.Message);
            }
            else
            {
                this.LogConnectionError(ex);
            }

            // Re-throw the exception so Polly can handle retries
            throw;
        }
    }

    private ConnectorParameters? CreateConnectorParameters()
    {
        try
        {
            return this._config.ConnectionType switch
            {
                KnxConnectionType.Tunnel => this.CreateTunnelingConnectorParameters(),
                KnxConnectionType.Router => this.CreateRoutingConnectorParameters(),
                KnxConnectionType.Usb => this.CreateUsbConnectorParameters(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(this._config.ConnectionType),
                    this._config.ConnectionType,
                    "Unsupported KNX connection type"
                ),
            };
        }
        catch (Exception ex)
        {
            this.LogConnectorParametersError(ex);
            return null;
        }
    }

    private ConnectorParameters? CreateTunnelingConnectorParameters()
    {
        if (string.IsNullOrEmpty(this._config.Gateway))
        {
            this.LogGatewayRequired("IP Tunneling");
            return null;
        }

        this.LogUsingIpTunneling(this._config.Gateway, this._config.Port);
        return new IpTunnelingConnectorParameters(this._config.Gateway, this._config.Port);
    }

    private ConnectorParameters? CreateRoutingConnectorParameters()
    {
        var multicastAddress = this._config.MulticastAddress;

        try
        {
            // Try to parse as IP address first
            if (System.Net.IPAddress.TryParse(multicastAddress, out var ipAddress))
            {
                this.LogUsingIpRouting(multicastAddress);
                return new IpRoutingConnectorParameters(ipAddress);
            }

            // If not an IP address, resolve hostname to IP address
            var hostEntry = System.Net.Dns.GetHostEntry(multicastAddress);
            var resolvedIp = hostEntry.AddressList.FirstOrDefault(addr =>
                addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            );

            if (resolvedIp == null)
            {
                this.LogMulticastAddressResolutionFailed(multicastAddress);
                return null;
            }

            this.LogUsingIpRouting($"{multicastAddress} ({resolvedIp})");
            return new IpRoutingConnectorParameters(resolvedIp);
        }
        catch (Exception ex)
        {
            this.LogMulticastAddressError(multicastAddress, ex.Message);
            return null;
        }
    }

    private ConnectorParameters? CreateUsbConnectorParameters()
    {
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            this.LogNoUsbDevicesFound();
            return null;
        }

        // If a specific USB device is configured, try to find it
        if (!string.IsNullOrEmpty(this._config.UsbDevice))
        {
            var specificDevice = usbDevices.FirstOrDefault(d =>
                d.ToString().Contains(this._config.UsbDevice, StringComparison.OrdinalIgnoreCase)
            );

            if (specificDevice != null)
            {
                this.LogUsingSpecificUsbDevice(this._config.UsbDevice, specificDevice.ToString());
                return UsbConnectorParameters.FromDiscovery(specificDevice);
            }
            else
            {
                this.LogSpecificUsbDeviceNotFound(this._config.UsbDevice);
                // Fall back to first available device
            }
        }

        // Use first available USB device
        this.LogUsingUsbDevice(usbDevices[0].ToString());
        return UsbConnectorParameters.FromDiscovery(usbDevices[0]);
    }

    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        try
        {
            var address = e.DestinationAddress.ToString();
            var value = e.Value;

            this.LogGroupValueReceived(address, value);

            // Map group address to command
            var command = this.MapGroupAddressToCommand(address, value);
            if (command != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.ExecuteCommandAsync(command, CancellationToken.None);
                        this.LogCommandMapped(address, command.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        this.LogCommandExecutionError(command.GetType().Name, ex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            this.LogGroupValueProcessingError("unknown", ex);
        }
    }

    private async Task<Result> ExecuteCommandAsync(object command, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = this._serviceProvider.CreateScope();

            return command switch
            {
                SetZoneVolumeCommand cmd =>
                    await this.GetHandler<Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetZoneMuteCommand cmd =>
                    await this.GetHandler<Server.Features.Zones.Handlers.SetZoneMuteCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                PlayCommand cmd => await this.GetHandler<Server.Features.Zones.Handlers.PlayCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                PauseCommand cmd => await this.GetHandler<Server.Features.Zones.Handlers.PauseCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                StopCommand cmd => await this.GetHandler<Server.Features.Zones.Handlers.StopCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                NextTrackCommand cmd => await this.GetHandler<Server.Features.Zones.Handlers.NextTrackCommandHandler>(
                        scope
                    )
                    .Handle(cmd, cancellationToken),
                PreviousTrackCommand cmd =>
                    await this.GetHandler<Server.Features.Zones.Handlers.PreviousTrackCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetClientVolumeCommand cmd =>
                    await this.GetHandler<Server.Features.Clients.Handlers.SetClientVolumeCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetClientMuteCommand cmd =>
                    await this.GetHandler<Server.Features.Clients.Handlers.SetClientMuteCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                _ => Result.Failure($"Unknown command type: {command.GetType().Name}"),
            };
        }
        catch (Exception ex)
        {
            this.LogCommandExecutionError(command.GetType().Name, ex);
            return Result.Failure($"Failed to execute command: {ex.Message}");
        }
    }

    private object? MapGroupAddressToCommand(string groupAddress, object value)
    {
        // Check zones for matching group addresses
        for (int i = 0; i < this._zones.Count; i++)
        {
            var zone = this._zones[i];
            var zoneIndex = i + 1; // 1-based zone ID

            if (!zone.Knx.Enabled)
                continue;

            var knxConfig = zone.Knx;

            // Volume commands
            if (groupAddress == knxConfig.Volume && value is int volumeValue)
            {
                return new SetZoneVolumeCommand
                {
                    ZoneIndex = zoneIndex,
                    Volume = volumeValue,
                    Source = CommandSource.Knx,
                };
            }

            // Mute commands
            if (groupAddress == knxConfig.Mute && value is bool muteValue)
            {
                return new SetZoneMuteCommand
                {
                    ZoneIndex = zoneIndex,
                    Enabled = muteValue,
                    Source = CommandSource.Knx,
                };
            }

            // Playback commands
            if (groupAddress == knxConfig.Play && value is bool playValue && playValue)
            {
                return new PlayCommand { ZoneIndex = zoneIndex, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.Pause && value is bool pauseValue && pauseValue)
            {
                return new PauseCommand { ZoneIndex = zoneIndex, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.Stop && value is bool stopValue && stopValue)
            {
                return new StopCommand { ZoneIndex = zoneIndex, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.TrackNext && value is bool nextValue && nextValue)
            {
                return new NextTrackCommand { ZoneIndex = zoneIndex, Source = CommandSource.Knx };
            }

            if (groupAddress == knxConfig.TrackPrevious && value is bool prevValue && prevValue)
            {
                return new PreviousTrackCommand { ZoneIndex = zoneIndex, Source = CommandSource.Knx };
            }
        }

        // Check clients for matching group addresses
        for (int i = 0; i < this._clients.Count; i++)
        {
            var client = this._clients[i];
            var clientIndex = i + 1; // 1-based client ID

            if (!client.Knx.Enabled)
                continue;

            var knxConfig = client.Knx;

            // Client volume commands
            if (groupAddress == knxConfig.Volume && value is int clientVolumeValue)
            {
                return new SetClientVolumeCommand
                {
                    ClientIndex = clientIndex,
                    Volume = clientVolumeValue,
                    Source = CommandSource.Knx,
                };
            }

            // Client mute commands
            if (groupAddress == knxConfig.Mute && value is bool clientMuteValue)
            {
                return new SetClientMuteCommand
                {
                    ClientIndex = clientIndex,
                    Enabled = clientMuteValue,
                    Source = CommandSource.Knx,
                };
            }
        }

        return null;
    }

    private string? GetStatusGroupAddress(string statusId, int targetId)
    {
        // Handle zone statuses (1-based indexing)
        if (targetId > 0 && targetId <= this._zones.Count)
        {
            var zone = this._zones[targetId - 1];
            if (zone.Knx.Enabled)
            {
                return statusId switch
                {
                    "VOLUME_STATUS" => zone.Knx.VolumeStatus,
                    "MUTE_STATUS" => zone.Knx.MuteStatus,
                    "PLAYBACK_STATE" => zone.Knx.ControlStatus,
                    "TRACK_INDEX" => zone.Knx.TrackStatus,
                    "PLAYLIST_INDEX" => zone.Knx.PlaylistStatus,
                    "TRACK_REPEAT_STATUS" => zone.Knx.TrackRepeatStatus,
                    "PLAYLIST_REPEAT_STATUS" => zone.Knx.RepeatStatus,
                    "PLAYLIST_SHUFFLE_STATUS" => zone.Knx.ShuffleStatus,
                    _ => null,
                };
            }
        }

        // Handle client statuses (1-based indexing)
        if (targetId > 0 && targetId <= this._clients.Count)
        {
            var client = this._clients[targetId - 1];
            if (client.Knx.Enabled)
            {
                return statusId switch
                {
                    "CLIENT_VOLUME_STATUS" => client.Knx.VolumeStatus,
                    "CLIENT_MUTE_STATUS" => client.Knx.MuteStatus,
                    "CLIENT_CONNECTED" => client.Knx.ConnectedStatus,
                    "CLIENT_LATENCY_STATUS" => client.Knx.LatencyStatus,
                    "CLIENT_ZONE_STATUS" => client.Knx.ZoneStatus,
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
            var result = await this._operationPolicy.ExecuteAsync(
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

                    await this._knxBus!.WriteGroupValueAsync(ga, groupValue);
                    return Result.Success();
                },
                cancellationToken
            );

            this.LogGroupValueWritten(groupAddress, value);
            return result;
        }
        catch (Exception ex)
        {
            this.LogWriteGroupValueError(groupAddress, value, ex);
            return Result.Failure($"Failed to write group value: {ex.Message}");
        }
    }

    private ResiliencePipeline CreateConnectionPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);

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
                        this.LogConnectionRetryAttempt(
                            this._config.Gateway ?? "USB",
                            this._config.Port,
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
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Operation);
        return ResiliencePolicyFactory.CreatePipeline(validatedConfig, "KNX-Operation");
    }

    private void StartReconnectTimer()
    {
        if (this._config.AutoReconnect)
        {
            var interval = TimeSpan.FromSeconds(30); // Reconnect every 30 seconds
            this._reconnectTimer.Change(interval, interval);
            this.LogReconnectTimerStarted();
        }
    }

    private void StopReconnectTimer()
    {
        this._reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async void OnReconnectTimer(object? state)
    {
        if (!this._isInitialized || this.IsConnected)
        {
            return;
        }

        this.LogAttemptingReconnection();
        var result = await this.InitializeAsync();
        if (result.IsSuccess)
        {
            this.StopReconnectTimer();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        await this.StopAsync();

        this._reconnectTimer.Dispose();
        this._connectionSemaphore.Dispose();

        this._disposed = true;
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

    /// <summary>
    /// Maps client event types to KNX status IDs and converts payloads to KNX-compatible values.
    /// </summary>
    private static (string? statusId, object knxValue) MapClientEventToKnxStatus<T>(string eventType, T payload)
    {
        return eventType.ToUpperInvariant() switch
        {
            "CLIENT_VOLUME_STATUS" when payload is int volume => ("CLIENT_VOLUME_STATUS", Math.Clamp(volume, 0, 100)),
            "CLIENT_MUTE_STATUS" when payload is bool mute => ("CLIENT_MUTE_STATUS", mute ? 1 : 0),
            "CLIENT_LATENCY_STATUS" when payload is int latency => (
                "CLIENT_LATENCY_STATUS",
                Math.Clamp(latency, 0, 65535)
            ),
            "CLIENT_CONNECTED" when payload is bool connected => ("CLIENT_CONNECTED", connected ? 1 : 0),
            "CLIENT_ZONE_STATUS" when payload is int zone => ("CLIENT_ZONE_STATUS", Math.Clamp(zone, 0, 255)),
            _ => (null, payload!),
        };
    }

    /// <summary>
    /// Maps zone event types to KNX status IDs and converts payloads to KNX-compatible values.
    /// </summary>
    private static (string? statusId, object knxValue) MapZoneEventToKnxStatus<T>(string eventType, T payload)
    {
        return eventType.ToUpperInvariant() switch
        {
            "VOLUME_STATUS" when payload is int volume => ("VOLUME_STATUS", Math.Clamp(volume, 0, 100)),
            "MUTE_STATUS" when payload is bool mute => ("MUTE_STATUS", mute ? 1 : 0),
            "PLAYBACK_STATE" when payload is string state => ("PLAYBACK_STATE", MapPlaybackStateToKnx(state)),
            "TRACK_INDEX" when payload is int track => ("TRACK_INDEX", Math.Clamp(track, 0, 255)),
            "PLAYLIST_INDEX" when payload is int playlist => ("PLAYLIST_INDEX", Math.Clamp(playlist, 0, 255)),
            "TRACK_REPEAT_STATUS" when payload is bool trackRepeat => ("TRACK_REPEAT_STATUS", trackRepeat ? 1 : 0),
            "PLAYLIST_REPEAT_STATUS" when payload is bool playlistRepeat => (
                "PLAYLIST_REPEAT_STATUS",
                playlistRepeat ? 1 : 0
            ),
            "PLAYLIST_SHUFFLE_STATUS" when payload is bool shuffle => ("PLAYLIST_SHUFFLE_STATUS", shuffle ? 1 : 0),
            _ => (null, payload!),
        };
    }

    /// <summary>
    /// Maps playback state strings to KNX values.
    /// </summary>
    private static int MapPlaybackStateToKnx(string state)
    {
        return state.ToLowerInvariant() switch
        {
            "stopped" => 0,
            "playing" => 1,
            "paused" => 2,
            _ => 0,
        };
    }

    /// <summary>
    /// Helper method for debug logging.
    /// </summary>
    private void LogDebug(string message)
    {
        this.LogKnxDebugMessage(message);
    }

    /// <summary>
    /// Determines the target type (zone or client) based on the status ID.
    /// </summary>
    private static string GetTargetTypeDescription(string statusId)
    {
        return statusId switch
        {
            // Client-specific status IDs
            "CLIENT_VOLUME_STATUS"
            or "CLIENT_MUTE_STATUS"
            or "CLIENT_LATENCY_STATUS"
            or "CLIENT_ZONE_STATUS"
            or "CLIENT_CONNECTED"
            or "CLIENT_STATE" => "client",

            // Zone-specific status IDs (everything else)
            _ => "zone",
        };
    }

    /// <summary>
    /// Logs group address not found with contextual target type information.
    /// </summary>
    private void LogGroupAddressNotFoundWithContext(string statusId, int targetId)
    {
        var targetType = GetTargetTypeDescription(statusId);
        var targetDescription = targetType == "zone" ? $"zone {targetId}" : $"client {targetId}";

        this.LogNoKnxGroupAddressConfigured(statusId, targetDescription);
    }

    /// <summary>
    /// Logs send status error with contextual target type information.
    /// </summary>
    private void LogSendStatusErrorWithContext(string statusId, int targetId, Exception exception)
    {
        var targetType = GetTargetTypeDescription(statusId);
        var targetDescription = targetType == "zone" ? $"zone {targetId}" : $"client {targetId}";

        this.LogErrorSendingKnxStatus(exception, statusId, targetDescription);
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

    [LoggerMessage(8012, LogLevel.Error, "KNX connection error: {ErrorMessage}")]
    private partial void LogConnectionErrorMessage(string errorMessage);

    [LoggerMessage(
        8013,
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

    [LoggerMessage(8014, LogLevel.Debug, "Using KNX IP tunneling connection to {Gateway}:{Port}")]
    private partial void LogUsingIpTunneling(string gateway, int port);

    [LoggerMessage(8015, LogLevel.Debug, "Using KNX IP routing connection to {Gateway}")]
    private partial void LogUsingIpRouting(string gateway);

    [LoggerMessage(8016, LogLevel.Debug, "Using KNX USB device: {Device}")]
    private partial void LogUsingUsbDevice(string device);

    [LoggerMessage(8017, LogLevel.Error, "Gateway address is required for {ConnectionType} connection")]
    private partial void LogGatewayRequired(string connectionType);

    [LoggerMessage(8018, LogLevel.Warning, "No KNX USB devices found")]
    private partial void LogNoUsbDevicesFound();

    [LoggerMessage(8019, LogLevel.Error, "Error creating KNX connector parameters")]
    private partial void LogConnectorParametersError(Exception exception);

    [LoggerMessage(8020, LogLevel.Error, "Failed to resolve multicast address '{MulticastAddress}' to IPv4 address")]
    private partial void LogMulticastAddressResolutionFailed(string multicastAddress);

    [LoggerMessage(8021, LogLevel.Error, "Error resolving multicast address '{MulticastAddress}': {ErrorMessage}")]
    private partial void LogMulticastAddressError(string multicastAddress, string errorMessage);

    [LoggerMessage(8022, LogLevel.Debug, "Using specific KNX USB device '{ConfiguredDevice}': {ActualDevice}")]
    private partial void LogUsingSpecificUsbDevice(string configuredDevice, string actualDevice);

    [LoggerMessage(
        8023,
        LogLevel.Warning,
        "Configured USB device '{ConfiguredDevice}' not found, using first available device"
    )]
    private partial void LogSpecificUsbDeviceNotFound(string configuredDevice);

    [LoggerMessage(8024, LogLevel.Debug, "KNX group value received: {GroupAddress} = {Value}")]
    private partial void LogGroupValueReceived(string groupAddress, object value);

    [LoggerMessage(8025, LogLevel.Debug, "KNX command mapped: {GroupAddress} -> {CommandType}")]
    private partial void LogCommandMapped(string groupAddress, string commandType);

    [LoggerMessage(8026, LogLevel.Error, "Error processing KNX group value from {GroupAddress}")]
    private partial void LogGroupValueProcessingError(string groupAddress, Exception exception);

    [LoggerMessage(8027, LogLevel.Warning, "KNX service not connected for operation: {Operation}")]
    private partial void LogNotConnected(string operation);

    [LoggerMessage(8028, LogLevel.Warning, "No KNX group address found for status {StatusId} on target {TargetId}")]
    private partial void LogGroupAddressNotFound(string statusId, int targetId);

    [LoggerMessage(8029, LogLevel.Error, "Error sending KNX status {StatusId} to target {TargetId}")]
    private partial void LogSendStatusError(string statusId, int targetId, Exception exception);

    [LoggerMessage(8030, LogLevel.Debug, "KNX group value written: {GroupAddress} = {Value}")]
    private partial void LogGroupValueWritten(string groupAddress, object value);

    [LoggerMessage(8031, LogLevel.Error, "Error writing KNX group value {GroupAddress} = {Value}")]
    private partial void LogWriteGroupValueError(string groupAddress, object value, Exception exception);

    [LoggerMessage(8032, LogLevel.Debug, "KNX group value read: {GroupAddress} = {Value}")]
    private partial void LogGroupValueRead(string groupAddress, object value);

    [LoggerMessage(8033, LogLevel.Error, "Error reading KNX group value from {GroupAddress}")]
    private partial void LogReadGroupValueError(string groupAddress, Exception exception);

    [LoggerMessage(8034, LogLevel.Error, "Error handling KNX status notification {StatusId} for target {TargetId}")]
    private partial void LogStatusNotificationError(string statusId, int targetId, Exception exception);

    [LoggerMessage(8035, LogLevel.Debug, "KNX reconnect timer started")]
    private partial void LogReconnectTimerStarted();

    [LoggerMessage(8036, LogLevel.Debug, "Attempting KNX reconnection")]
    private partial void LogAttemptingReconnection();

    [LoggerMessage(8037, LogLevel.Warning, "Invalid target ID '{TargetId}' for status '{StatusId}' - expected integer")]
    private partial void LogInvalidTargetId(string statusId, string targetId);

    [LoggerMessage(8038, LogLevel.Error, "Error executing KNX command {CommandType}")]
    private partial void LogCommandExecutionError(string commandType, Exception exception);

    #endregion
}
