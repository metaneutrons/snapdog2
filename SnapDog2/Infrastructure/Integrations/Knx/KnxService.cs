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
namespace SnapDog2.Infrastructure.Integrations.Knx;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cortex.Mediator.Commands;
using global::Knx.Falcon;
using global::Knx.Falcon.Configuration;
using global::Knx.Falcon.Sdk;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Resilience;
using SnapDog2.Server.Clients.Commands.Config;
using SnapDog2.Server.Clients.Commands.Volume;
using SnapDog2.Server.Clients.Handlers;
using SnapDog2.Server.Shared.Factories;
using SnapDog2.Server.Zones.Commands.Playback;
using SnapDog2.Server.Zones.Commands.Playlist;
using SnapDog2.Server.Zones.Commands.Track;
using SnapDog2.Server.Zones.Commands.Volume;
using SnapDog2.Server.Zones.Handlers;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Constants;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;
using ClientVolumeDownCommandHandler = SnapDog2.Server.Clients.Handlers.ClientVolumeDownCommandHandler;
using ClientVolumeUpCommandHandler = SnapDog2.Server.Clients.Handlers.ClientVolumeUpCommandHandler;

/// <summary>
/// Enterprise-grade KNX integration service using Knx.Falcon.Sdk.
/// Provides bi-directional KNX communication with automatic reconnection and command mapping.
/// Updated to use IServiceProvider to resolve scoped IMediator.
/// </summary>
public partial class KnxService : IKnxService
{
    private readonly KnxConfig _config;
    private readonly List<ZoneConfig> _zones;
    private readonly List<ClientConfig> _clients;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnxService> _logger;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;
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
        new ConcurrentDictionary<string, string>();
        this._connectionSemaphore = new SemaphoreSlim(1, 1);

        // Configure resilience policies
        this._connectionPolicy = this.CreateConnectionPolicy();
        this._operationPolicy = this.CreateOperationPolicy();

        // Initialize reconnect timer (disabled initially)
        this._reconnectTimer = new Timer(this.OnReconnectTimer, null, Timeout.Infinite, Timeout.Infinite);

        this.LogServiceCreated(this._config.Gateway, this._config.Port, this._config.Enabled);
    }

    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            try
            {
                var isConnected = this._knxBus?.ConnectionState == BusConnectionState.Connected;
                this.LogKnxDebugMessage($"🔍 IsConnected check: _knxBus={this._knxBus != null}, ConnectionState={this._knxBus?.ConnectionState}, Result={isConnected}");
                return isConnected;
            }
            catch (ObjectDisposedException)
            {
                this.LogKnxDebugMessage("⚠️ IsConnected check failed: KNX bus object disposed");
                return false;
            }
        }
    }

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
                    async ct =>
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
        if (this._disposed)
        {
            this.LogKnxDebugMessage("⚠️ StopAsync called on already disposed service - skipping");
            return Result.Success();
        }

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
                catch (ObjectDisposedException)
                {
                    this.LogKnxDebugMessage("⚠️ KNX bus already disposed during stop");
                }
                catch (Exception ex)
                {
                    this.LogDisconnectionError(ex);
                }
                finally
                {
                    try
                    {
                        this._knxBus?.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
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
                // No group address configured means "not interested in this status" - this is normal and not an error
                this.LogGroupAddressNotConfigured(statusId, targetId);
                return Result.Success(); // Changed from Failure to Success
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
                async ct =>
                {
                    var ga = new GroupAddress(groupAddress);
                    var value = await this._knxBus!.ReadGroupValueAsync(ga, cancellationToken: ct);
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

            // For KNX, we need to convert client Index to integer if possible
            if (int.TryParse(clientIndex, out var clientIndexInt))
            {
                return await this.SendStatusAsync(statusId, clientIndexInt, knxValue, cancellationToken);
            }

            this.LogInvalidTargetId(statusId, clientIndex);
            return Result.Failure($"Invalid client Index for KNX: {clientIndex}");
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
        // as per the command framework blueprint
        this.LogKnxGlobalStatusPublishingNotImplemented(eventType);
        return await Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Connects to the KNX bus with automatic retries using Polly.
    /// </summary>
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
            await this._knxBus.ConnectAsync(cancellationToken);

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
            if (ex is KnxIpConnectorException)
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
            if (IPAddress.TryParse(multicastAddress, out var ipAddress))
            {
                this.LogUsingIpRouting(multicastAddress);
                return new IpRoutingConnectorParameters(ipAddress);
            }

            // If not an IP address, resolve hostname to IP address
            var hostEntry = Dns.GetHostEntry(multicastAddress);
            var resolvedIp = hostEntry.AddressList.FirstOrDefault(addr =>
                addr.AddressFamily == AddressFamily.InterNetwork
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

            this.LogSpecificUsbDeviceNotFound(this._config.UsbDevice);
            // Fall back to first available device
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

            // Check if this is a read request (typically indicated by null value or specific pattern)
            if (this.IsReadRequest(e))
            {
                // Handle read request asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.HandleReadRequestAsync(address, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        this.LogReadRequestError(address, ex);
                    }
                });
                return;
            }

            // Map group address to command (existing logic)
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

    /// <summary>
    /// Determines if the received KNX message is a read request.
    /// In KNX, read requests typically have null values or specific patterns.
    /// </summary>
    private bool IsReadRequest(GroupEventArgs e)
    {
        // In Falcon SDK, read requests typically have null values
        // or the value might be a specific type indicating a read request
        if (e.Value == null)
        {
            return true;
        }

        // Additional heuristics: if the address is a status address (not a control address)
        // and we receive a message, it's likely a read request
        var address = e.DestinationAddress.ToString();
        return this.IsStatusAddress(address);
    }

    /// <summary>
    /// Determines if the given group address is a status address (vs control address).
    /// Status addresses are used for reading current state, control addresses for commands.
    /// </summary>
    private bool IsStatusAddress(string groupAddress)
    {
        // Check if this address matches any configured status addresses
        // Status addresses typically end with different numbers than control addresses

        // Check zone status addresses
        foreach (var zone in this._zones)
        {
            if (!zone.Knx.Enabled)
            {
                continue;
            }

            if (groupAddress == zone.Knx.VolumeStatus ||
                groupAddress == zone.Knx.MuteStatus ||
                groupAddress == zone.Knx.TrackStatus ||
                groupAddress == zone.Knx.PlaylistStatus ||
                groupAddress == zone.Knx.TrackRepeatStatus ||
                groupAddress == zone.Knx.RepeatStatus ||
                groupAddress == zone.Knx.ShuffleStatus ||
                groupAddress == zone.Knx.TrackTitleStatus ||
                groupAddress == zone.Knx.TrackArtistStatus ||
                groupAddress == zone.Knx.TrackAlbumStatus ||
                groupAddress == zone.Knx.TrackProgressStatus ||
                groupAddress == zone.Knx.TrackPlayingStatus)
            {
                return true;
            }
        }

        // Check client status addresses
        foreach (var client in this._clients)
        {
            if (!client.Knx.Enabled)
            {
                continue;
            }

            if (groupAddress == client.Knx.VolumeStatus ||
                groupAddress == client.Knx.MuteStatus ||
                groupAddress == client.Knx.ConnectedStatus ||
                groupAddress == client.Knx.LatencyStatus ||
                groupAddress == client.Knx.ZoneStatus)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Handles KNX read requests by responding with current state values.
    /// Maps status addresses to current entity state and sends response via KNX bus.
    /// </summary>
    private async Task HandleReadRequestAsync(string groupAddress, CancellationToken cancellationToken)
    {
        this.LogReadRequestReceived(groupAddress);

        try
        {
            // Map group address to status information
            var statusInfo = this.MapGroupAddressToStatusInfo(groupAddress);
            if (statusInfo == null)
            {
                this.LogUnmappedReadRequest(groupAddress);
                return;
            }

            // Get current state value
            var currentValue = this.GetCurrentStateValue(statusInfo);
            if (currentValue == null)
            {
                this.LogReadRequestStateNotAvailable(groupAddress, statusInfo.EntityType, statusInfo.EntityId);
                return;
            }

            // Convert to KNX-compatible value
            var knxValue = ConvertToKnxValue(currentValue);

            // Send response via KNX bus
            await this._knxBus!.WriteGroupValueAsync(groupAddress, knxValue, cancellationToken: cancellationToken);

            this.LogReadResponseSent(groupAddress, knxValue, statusInfo.StatusId, statusInfo.EntityId);
        }
        catch (Exception ex)
        {
            this.LogReadRequestError(groupAddress, ex);
        }
    }

    /// <summary>
    /// Information about a status address mapping.
    /// </summary>
    private record StatusInfo(string StatusId, string EntityType, string EntityId, int EntityIndex);

    /// <summary>
    /// Maps a group address to status information for read requests.
    /// This is the reverse of the existing GetStatusGroupAddress method.
    /// </summary>
    private StatusInfo? MapGroupAddressToStatusInfo(string groupAddress)
    {
        // Check zone status addresses
        for (var i = 0; i < this._zones.Count; i++)
        {
            var zone = this._zones[i];
            var zoneIndex = i + 1; // 1-based zone ID

            if (!zone.Knx.Enabled)
            {
                continue;
            }

            // Map zone status addresses to StatusIds
            if (groupAddress == zone.Knx.VolumeStatus)
            {
                return new StatusInfo(StatusIds.VolumeStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.MuteStatus)
            {
                return new StatusInfo(StatusIds.MuteStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackStatus)
            {
                return new StatusInfo(StatusIds.TrackIndex, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.PlaylistStatus)
            {
                return new StatusInfo(StatusIds.PlaylistIndex, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackRepeatStatus)
            {
                return new StatusInfo(StatusIds.TrackRepeatStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.RepeatStatus)
            {
                return new StatusInfo(StatusIds.PlaylistRepeatStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.ShuffleStatus)
            {
                return new StatusInfo(StatusIds.PlaylistShuffleStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackTitleStatus)
            {
                return new StatusInfo(StatusIds.TrackMetadataTitle, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackArtistStatus)
            {
                return new StatusInfo(StatusIds.TrackMetadataArtist, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackAlbumStatus)
            {
                return new StatusInfo(StatusIds.TrackMetadataAlbum, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackProgressStatus)
            {
                return new StatusInfo(StatusIds.TrackProgressStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }

            if (groupAddress == zone.Knx.TrackPlayingStatus)
            {
                return new StatusInfo(StatusIds.TrackPlayingStatus, "Zone", zoneIndex.ToString(), zoneIndex);
            }
        }

        // Check client status addresses
        for (var i = 0; i < this._clients.Count; i++)
        {
            var client = this._clients[i];
            var clientIndex = i + 1; // 1-based client Index

            if (!client.Knx.Enabled)
            {
                continue;
            }

            // Map client status addresses to StatusIds
            if (groupAddress == client.Knx.VolumeStatus)
            {
                return new StatusInfo(StatusIds.ClientVolumeStatus, "Client", clientIndex.ToString(), clientIndex);
            }

            if (groupAddress == client.Knx.MuteStatus)
            {
                return new StatusInfo(StatusIds.ClientMuteStatus, "Client", clientIndex.ToString(), clientIndex);
            }

            if (groupAddress == client.Knx.ConnectedStatus)
            {
                return new StatusInfo(StatusIds.ClientConnected, "Client", clientIndex.ToString(), clientIndex);
            }

            if (groupAddress == client.Knx.LatencyStatus)
            {
                return new StatusInfo(StatusIds.ClientLatencyStatus, "Client", clientIndex.ToString(), clientIndex);
            }

            if (groupAddress == client.Knx.ZoneStatus)
            {
                return new StatusInfo(StatusIds.ClientZoneStatus, "Client", clientIndex.ToString(), clientIndex);
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves the current state value for the specified status information.
    /// Uses existing state stores to get current entity state.
    /// </summary>
    private object? GetCurrentStateValue(StatusInfo statusInfo)
    {
        try
        {
            using var scope = this._serviceProvider.CreateScope();

            if (statusInfo.EntityType == "Client")
            {
                var clientStateStore = scope.ServiceProvider.GetService<IClientStateStore>();
                if (clientStateStore == null)
                {
                    return null;
                }

                var clientState = clientStateStore.GetClientState(statusInfo.EntityIndex);
                if (clientState == null)
                {
                    return null;
                }

                return statusInfo.StatusId switch
                {
                    var x when x == StatusIds.ClientVolumeStatus => clientState.Volume,
                    var x when x == StatusIds.ClientMuteStatus => clientState.Mute,
                    var x when x == StatusIds.ClientConnected => clientState.Connected,
                    var x when x == StatusIds.ClientLatencyStatus => clientState.LatencyMs,
                    var x when x == StatusIds.ClientZoneStatus => clientState.ZoneIndex,
                    _ => null
                };
            }

            if (statusInfo.EntityType == "Zone")
            {
                var zoneStateStore = scope.ServiceProvider.GetService<IZoneStateStore>();
                if (zoneStateStore == null)
                {
                    return null;
                }

                var zoneState = zoneStateStore.GetZoneState(statusInfo.EntityIndex);
                if (zoneState == null)
                {
                    return null;
                }

                return statusInfo.StatusId switch
                {
                    var x when x == StatusIds.VolumeStatus => zoneState.Volume,
                    var x when x == StatusIds.MuteStatus => zoneState.Mute,
                    var x when x == StatusIds.TrackRepeatStatus => zoneState.TrackRepeat,
                    var x when x == StatusIds.PlaylistRepeatStatus => zoneState.PlaylistRepeat,
                    var x when x == StatusIds.PlaylistShuffleStatus => zoneState.PlaylistShuffle,
                    var x when x == StatusIds.TrackPlayingStatus => zoneState.PlaybackState == PlaybackState.Playing,
                    // For properties not available in ZoneState, return default values
                    var x when x == StatusIds.TrackIndex => 0,
                    var x when x == StatusIds.PlaylistIndex => 0,
                    var x when x == StatusIds.TrackMetadataTitle => "",
                    var x when x == StatusIds.TrackMetadataArtist => "",
                    var x when x == StatusIds.TrackMetadataAlbum => "",
                    var x when x == StatusIds.TrackProgressStatus => 0,
                    _ => null
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            this.LogCurrentStateRetrievalError(statusInfo.EntityType, statusInfo.EntityId, statusInfo.StatusId, ex);
            return null;
        }
    }

    private async Task<Result> ExecuteCommandAsync(ICommand<Result> command, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = this._serviceProvider.CreateScope();

            return command switch
            {
                // Zone Volume Commands
                SetZoneVolumeCommand cmd =>
                    await GetHandler<SetZoneVolumeCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                VolumeUpCommand cmd => await GetHandler<VolumeUpCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                VolumeDownCommand cmd => await GetHandler<VolumeDownCommandHandler>(
                        scope
                    )
                    .Handle(cmd, cancellationToken),
                SetZoneMuteCommand cmd => await GetHandler<SetZoneMuteCommandHandler>(
                        scope
                    )
                    .Handle(cmd, cancellationToken),
                ToggleZoneMuteCommand cmd =>
                    await GetHandler<ToggleZoneMuteCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),

                // Zone Playback Commands
                PlayCommand cmd => await GetHandler<PlayCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                PauseCommand cmd => await GetHandler<PauseCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                StopCommand cmd => await GetHandler<StopCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),

                // Zone Track Commands
                SetTrackCommand cmd => await GetHandler<SetTrackCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                NextTrackCommand cmd => await GetHandler<NextTrackCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                PreviousTrackCommand cmd =>
                    await GetHandler<PreviousTrackCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetTrackRepeatCommand cmd =>
                    await GetHandler<SetTrackRepeatCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                ToggleTrackRepeatCommand cmd =>
                    await GetHandler<ToggleTrackRepeatCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                PlayTrackByIndexCommand cmd =>
                    await GetHandler<PlayTrackByIndexCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),

                // Zone Playlist Commands
                SetPlaylistCommand cmd => await GetHandler<SetPlaylistCommandHandler>(
                        scope
                    )
                    .Handle(cmd, cancellationToken),
                NextPlaylistCommand cmd => await GetHandler<NextPlaylistCommandHandler>(
                        scope
                    )
                    .Handle(cmd, cancellationToken),
                PreviousPlaylistCommand cmd =>
                    await GetHandler<PreviousPlaylistCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetPlaylistRepeatCommand cmd =>
                    await GetHandler<SetPlaylistRepeatCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                TogglePlaylistRepeatCommand cmd =>
                    await GetHandler<TogglePlaylistRepeatCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                SetPlaylistShuffleCommand cmd =>
                    await GetHandler<SetPlaylistShuffleCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                TogglePlaylistShuffleCommand cmd =>
                    await GetHandler<TogglePlaylistShuffleCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),

                // Client Volume Commands
                SetClientVolumeCommand cmd =>
                    await GetHandler<SetClientVolumeCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                ClientVolumeUpCommand cmd => await GetHandler<ClientVolumeUpCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                ClientVolumeDownCommand cmd => await GetHandler<ClientVolumeDownCommandHandler>(scope)
                    .Handle(cmd, cancellationToken),
                SetClientMuteCommand cmd =>
                    await GetHandler<SetClientMuteCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),
                ToggleClientMuteCommand cmd =>
                    await GetHandler<ToggleClientMuteCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),

                // Client Configuration Commands
                AssignClientToZoneCommand cmd =>
                    await GetHandler<AssignClientToZoneCommandHandler>(scope)
                        .Handle(cmd, cancellationToken),

                // Intentionally excluded commands (see comments in MapGroupAddressToCommand):
                // - SeekPositionCommand, SeekProgressCommand, PlayUrlCommand
                // - SetClientLatencyCommand, SetClientNameCommand

                _ => Result.Failure($"Unknown command type: {command.GetType().Name}"),
            };
        }
        catch (Exception ex)
        {
            this.LogCommandExecutionError(command.GetType().Name, ex);
            return Result.Failure($"Failed to execute command: {ex.Message}");
        }
    }

    private ICommand<Result>? MapGroupAddressToCommand(string groupAddress, object value)
    {
        // NOTE: The following commands are INTENTIONALLY NOT IMPLEMENTED in KNX:
        // - SeekPositionCommand (TRACK_POSITION): KNX lacks precision for millisecond-based seeking
        // - SeekProgressCommand (TRACK_PROGRESS): KNX lacks precision for percentage-based seeking
        // - PlayUrlCommand (TRACK_PLAY_URL): KNX cannot transmit URL strings effectively
        // - SetClientLatencyCommand (CLIENT_LATENCY): KNX latency adjustment not practical via bus
        // - SetClientNameCommand (CLIENT_NAME): KNX cannot transmit string names effectively

        // Check zones for matching group addresses
        for (var i = 0; i < this._zones.Count; i++)
        {
            var zone = this._zones[i];
            var zoneIndex = i + 1; // 1-based zone ID

            if (!zone.Knx.Enabled)
            {
                continue;
            }

            var knxConfig = zone.Knx;

            // Zone Volume Commands
            if (groupAddress == knxConfig.Volume && value is int volumeValue)
            {
                return CommandFactory.CreateSetZoneVolumeCommand(zoneIndex, volumeValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.VolumeUp && value is bool volumeUpValue && volumeUpValue)
            {
                return CommandFactory.CreateVolumeUpCommand(zoneIndex, 5, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.VolumeDown && value is bool volumeDownValue && volumeDownValue)
            {
                return CommandFactory.CreateVolumeDownCommand(zoneIndex, 5, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.Mute && value is bool muteValue)
            {
                return CommandFactory.CreateSetZoneMuteCommand(zoneIndex, muteValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.MuteToggle && value is bool muteToggleValue && muteToggleValue)
            {
                return CommandFactory.CreateToggleZoneMuteCommand(zoneIndex, CommandSource.Knx);
            }

            // Zone Playback Commands
            if (groupAddress == knxConfig.Play && value is bool playValue && playValue)
            {
                return CommandFactory.CreatePlayCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.Pause && value is bool pauseValue && pauseValue)
            {
                return CommandFactory.CreatePauseCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.Stop && value is bool stopValue && stopValue)
            {
                return CommandFactory.CreateStopCommand(zoneIndex, CommandSource.Knx);
            }

            // Zone Track Commands
            if (groupAddress == knxConfig.Track && value is int trackValue)
            {
                return CommandFactory.CreateSetTrackCommand(zoneIndex, trackValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.TrackNext && value is bool nextValue && nextValue)
            {
                return CommandFactory.CreateNextTrackCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.TrackPrevious && value is bool prevValue && prevValue)
            {
                return CommandFactory.CreatePreviousTrackCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.TrackRepeat && value is bool trackRepeatValue)
            {
                return CommandFactory.CreateSetTrackRepeatCommand(zoneIndex, trackRepeatValue, CommandSource.Knx);
            }

            if (
                groupAddress == knxConfig.TrackRepeatToggle
                && value is bool trackRepeatToggleValue
                && trackRepeatToggleValue
            )
            {
                return CommandFactory.CreateToggleTrackRepeatCommand(zoneIndex, CommandSource.Knx);
            }

            // Zone Playlist Commands
            if (groupAddress == knxConfig.Playlist && value is int playlistValue)
            {
                return CommandFactory.CreateSetPlaylistCommand(zoneIndex, playlistValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.PlaylistNext && value is bool playlistNextValue && playlistNextValue)
            {
                return CommandFactory.CreateNextPlaylistCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.PlaylistPrevious && value is bool playlistPrevValue && playlistPrevValue)
            {
                return CommandFactory.CreatePreviousPlaylistCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.Repeat && value is bool repeatValue)
            {
                return CommandFactory.CreateSetPlaylistRepeatCommand(zoneIndex, repeatValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.RepeatToggle && value is bool repeatToggleValue && repeatToggleValue)
            {
                return CommandFactory.CreateTogglePlaylistRepeatCommand(zoneIndex, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.Shuffle && value is bool shuffleValue)
            {
                return CommandFactory.CreateSetPlaylistShuffleCommand(zoneIndex, shuffleValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.ShuffleToggle && value is bool shuffleToggleValue && shuffleToggleValue)
            {
                return CommandFactory.CreateTogglePlaylistShuffleCommand(zoneIndex, CommandSource.Knx);
            }
        }

        // Check clients for matching group addresses
        for (var i = 0; i < this._clients.Count; i++)
        {
            var client = this._clients[i];
            var clientIndex = i + 1; // 1-based client Index

            if (!client.Knx.Enabled)
            {
                continue;
            }

            var knxConfig = client.Knx;

            // Client Volume Commands
            if (groupAddress == knxConfig.Volume && value is int clientVolumeValue)
            {
                return CommandFactory.CreateSetClientVolumeCommand(clientIndex, clientVolumeValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.VolumeUp && value is bool clientVolumeUpValue && clientVolumeUpValue)
            {
                return CommandFactory.CreateClientVolumeUpCommand(clientIndex, 5, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.VolumeDown && value is bool clientVolumeDownValue && clientVolumeDownValue)
            {
                return CommandFactory.CreateClientVolumeDownCommand(clientIndex, 5, CommandSource.Knx);
            }

            // Client Mute Commands
            if (groupAddress == knxConfig.Mute && value is bool clientMuteValue)
            {
                return CommandFactory.CreateSetClientMuteCommand(clientIndex, clientMuteValue, CommandSource.Knx);
            }

            if (groupAddress == knxConfig.MuteToggle && value is bool clientMuteToggleValue && clientMuteToggleValue)
            {
                return CommandFactory.CreateToggleClientMuteCommand(clientIndex, CommandSource.Knx);
            }

            // Client Configuration Commands
            if (groupAddress == knxConfig.Zone && value is int zoneValue)
            {
                return CommandFactory.CreateAssignClientToZoneCommand(clientIndex, zoneValue, CommandSource.Knx);
            }

            // Note: CLIENT_LATENCY and CLIENT_NAME are intentionally excluded (see comments above)
        }

        return null;
    }

    private string? GetStatusGroupAddress(string statusId, int targetId)
    {
        // First determine if this is a zone or client StatusId based on the StatusId itself
        var isClientStatusId =
            statusId == StatusIds.ClientVolumeStatus
            || statusId == StatusIds.ClientMuteStatus
            || statusId == StatusIds.ClientConnected
            || statusId == StatusIds.ClientLatencyStatus
            || statusId == StatusIds.ClientZoneStatus;

        if (isClientStatusId)
        {
            // Handle client statuses (1-based indexing)
            if (targetId > 0 && targetId <= this._clients.Count)
            {
                var client = this._clients[targetId - 1];

                if (client.Knx.Enabled)
                {
                    return statusId switch
                    {
                        var x when x == StatusIds.ClientVolumeStatus => client.Knx.VolumeStatus,
                        var x when x == StatusIds.ClientMuteStatus => client.Knx.MuteStatus,
                        var x when x == StatusIds.ClientConnected => client.Knx.ConnectedStatus,
                        var x when x == StatusIds.ClientLatencyStatus => client.Knx.LatencyStatus,
                        var x when x == StatusIds.ClientZoneStatus => client.Knx.ZoneStatus,
                        _ => null,
                    };
                }
            }
        }
        else
        {
            // Handle zone statuses (1-based indexing)
            if (targetId > 0 && targetId <= this._zones.Count)
            {
                var zone = this._zones[targetId - 1];
                if (zone.Knx.Enabled)
                {
                    return statusId switch
                    {
                        var x when x == StatusIds.VolumeStatus => zone.Knx.VolumeStatus,
                        var x when x == StatusIds.MuteStatus => zone.Knx.MuteStatus,
                        // Replaced PLAYBACK_STATE with more granular TRACK_PLAYING_STATUS for better KNX alignment
                        var x when x == StatusIds.TrackPlayingStatus => zone.Knx.TrackPlayingStatus,
                        var x when x == StatusIds.TrackIndex => zone.Knx.TrackStatus,
                        var x when x == StatusIds.PlaylistIndex => zone.Knx.PlaylistStatus,
                        var x when x == StatusIds.TrackRepeatStatus => zone.Knx.TrackRepeatStatus,
                        var x when x == StatusIds.PlaylistRepeatStatus => zone.Knx.RepeatStatus,
                        var x when x == StatusIds.PlaylistShuffleStatus => zone.Knx.ShuffleStatus,
                        // New track metadata status IDs (KNX DPT 16.001 - 14-byte strings)
                        var x when x == StatusIds.TrackMetadataTitle => zone.Knx.TrackTitleStatus,
                        var x when x == StatusIds.TrackMetadataArtist => zone.Knx.TrackArtistStatus,
                        var x when x == StatusIds.TrackMetadataAlbum => zone.Knx.TrackAlbumStatus,
                        // New track playback status IDs
                        var x when x == StatusIds.TrackProgressStatus => zone.Knx.TrackProgressStatus,
                        _ => null,
                    };
                }
            }
        }

        return null;
    }

    private async Task<Result> WriteToKnxAsync(string groupAddress, object value, CancellationToken cancellationToken)
    {
        try
        {
            var result = await this._operationPolicy.ExecuteAsync(
                async ct =>
                {
                    var ga = new GroupAddress(groupAddress);

                    // Convert value to appropriate type for GroupValue
                    var groupValue = value switch
                    {
                        // DPT 1.001 - Boolean (1-bit)
                        bool boolValue => new GroupValue(boolValue),

                        // DPT 5.001 - Percentage (0-100%) and DPT 5.010 - Counter (0-255)
                        byte byteValue => new GroupValue(byteValue),
                        int intValue when intValue >= 0 && intValue <= 255 => new GroupValue((byte)intValue),

                        // DPT 5.001 - Percentage (0-100%) - convert float percentage to byte
                        float floatValue when floatValue >= 0.0f && floatValue <= 1.0f => new GroupValue(
                            (byte)(floatValue * 100)
                        ), // Convert 0.0-1.0 to 0-100
                        float floatValue when floatValue >= 0.0f && floatValue <= 100.0f => new GroupValue(
                            (byte)floatValue
                        ), // Already 0-100 range

                        // DPT 16.001 - 14-byte string (ASCII)
                        string stringValue when stringValue.Length <= 14 => new GroupValue(
                            Encoding.ASCII.GetBytes(stringValue.PadRight(14, '\0'))
                        ),
                        string stringValue => new GroupValue(
                            Encoding.ASCII.GetBytes(stringValue.Substring(0, 14))
                        ),

                        _ => throw new ArgumentException(
                            $"Unsupported value type: {value.GetType()} with value: {value}"
                        ),
                    };

                    await this._knxBus!.WriteGroupValueAsync(ga, groupValue, cancellationToken: ct);
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
                    BackoffType = validatedConfig.BackoffType.ToLowerInvariant() switch
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
    public void Dispose()
    {
        // For synchronous disposal, we need to block on the async dispose
        // This is not ideal but necessary for DI container compatibility
        try
        {
            this.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Log the exception but don't throw to avoid issues during disposal
            this.LogKnxDisposalError(ex.Message);
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

    private static T GetHandler<T>(IServiceScope scope)
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
            var x when x == StatusIds.ClientVolumeStatus && payload is int volume => (
                StatusIds.ClientVolumeStatus,
                Math.Clamp(volume, 0, 100)
            ),
            var x when x == StatusIds.ClientMuteStatus && payload is bool mute => (
                StatusIds.ClientMuteStatus,
                mute ? 1 : 0
            ),
            var x when x == StatusIds.ClientLatencyStatus && payload is int latency => (
                StatusIds.ClientLatencyStatus,
                Math.Clamp(latency, 0, 65535)
            ),
            var x when x == StatusIds.ClientConnected && payload is bool connected => (
                StatusIds.ClientConnected,
                connected ? 1 : 0
            ),
            var x when x == StatusIds.ClientZoneStatus && payload is int zone => (
                StatusIds.ClientZoneStatus,
                Math.Clamp(zone, 0, 255)
            ),
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
            var x when x == StatusIds.VolumeStatus && payload is int volume => (
                StatusIds.VolumeStatus,
                Math.Clamp(volume, 0, 100)
            ),
            var x when x == StatusIds.MuteStatus && payload is bool mute => (StatusIds.MuteStatus, mute ? 1 : 0),
            var x when x == StatusIds.PlaybackState && payload is PlaybackState state => (
                StatusIds.PlaybackState,
                MapPlaybackStateToKnx(state)
            ),
            var x when x == StatusIds.TrackIndex && payload is int track => (
                StatusIds.TrackIndex,
                Math.Clamp(track, 0, 255)
            ),
            var x when x == StatusIds.PlaylistIndex && payload is int playlist => (
                StatusIds.PlaylistIndex,
                Math.Clamp(playlist, 0, 255)
            ),
            var x when x == StatusIds.TrackRepeatStatus && payload is bool trackRepeat => (
                StatusIds.TrackRepeatStatus,
                trackRepeat ? 1 : 0
            ),
            var x when x == StatusIds.PlaylistRepeatStatus && payload is bool playlistRepeat => (
                StatusIds.PlaylistRepeatStatus,
                playlistRepeat ? 1 : 0
            ),
            var x when x == StatusIds.PlaylistShuffleStatus && payload is bool shuffle => (
                StatusIds.PlaylistShuffleStatus,
                shuffle ? 1 : 0
            ),
            _ => (null, payload!),
        };
    }

    /// <summary>
    /// Maps playback state enum to KNX values.
    /// </summary>
    private static int MapPlaybackStateToKnx(PlaybackState state)
    {
        return state switch
        {
            PlaybackState.Stopped => 0,
            PlaybackState.Playing => 1,
            PlaybackState.Paused => 2,
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
            _ when statusId == StatusIds.ClientVolumeStatus
                    || statusId == StatusIds.ClientMuteStatus
                    || statusId == StatusIds.ClientLatencyStatus
                    || statusId == StatusIds.ClientZoneStatus
                    || statusId == StatusIds.ClientConnected
                    || statusId == StatusIds.ClientState => "client",

            // Zone-specific status IDs (everything else)
            _ => "zone",
        };
    }

    /// <summary>
    /// Logs group address not found with contextual target type information.
    /// </summary>
    private void LogGroupAddressNotConfigured(string statusId, int targetId)
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

    /// <summary>
    /// Converts a state value to KNX-compatible GroupValue format.
    /// Reuses existing KNX value conversion logic.
    /// </summary>
    private static GroupValue ConvertToKnxValue(object value)
    {
        return value switch
        {
            // Boolean values (DPT 1.001)
            bool boolValue => new GroupValue(boolValue),

            // Integer values (DPT 5.001 - Percentage 0-100, DPT 5.010 - Counter 0-255)
            int intValue and >= 0 and <= 255 => new GroupValue((byte)intValue),

            // String values (DPT 16.001 - 14-byte ASCII)
            string stringValue when stringValue.Length <= 14 =>
                new GroupValue(Encoding.ASCII.GetBytes(stringValue.PadRight(14, '\0'))),
            string stringValue =>
                new GroupValue(Encoding.ASCII.GetBytes(stringValue.Substring(0, 14))),

            // Default: try to convert to byte for most KNX data types
            _ => new GroupValue(((byte)value))
        };
    }

    #region Logging

    [LoggerMessage(EventId = 115050, Level = LogLevel.Debug, Message = "KNX service created with gateway: {Gateway}, port: {Port}, enabled: {Enabled}"
)]
    private partial void LogServiceCreated(string? gateway, int port, bool enabled);

    [LoggerMessage(EventId = 115051, Level = LogLevel.Information, Message = "KNX service is disabled via configuration"
)]
    private partial void LogServiceDisabled();

    [LoggerMessage(EventId = 115052, Level = LogLevel.Debug, Message = "KNX service already initialized"
)]
    private partial void LogAlreadyInitialized();

    [LoggerMessage(EventId = 115053, Level = LogLevel.Information, Message = "🚀 Starting KNX service initialization"
)]
    private partial void LogInitializationStarted();

    [LoggerMessage(EventId = 115054, Level = LogLevel.Information, Message = "KNX service initialization completed successfully"
)]
    private partial void LogInitializationCompleted();

    [LoggerMessage(EventId = 115055, Level = LogLevel.Error, Message = "KNX service initialization failed: {Error}"
)]
    private partial void LogInitializationFailed(string error);

    [LoggerMessage(EventId = 115056, Level = LogLevel.Debug, Message = "Stopping KNX service"
)]
    private partial void LogStoppingService();

    [LoggerMessage(EventId = 115057, Level = LogLevel.Debug, Message = "KNX service stopped successfully"
)]
    private partial void LogServiceStopped();

    [LoggerMessage(EventId = 115058, Level = LogLevel.Warning, Message = "Error during KNX disconnection"
)]
    private partial void LogDisconnectionError(Exception exception);

    [LoggerMessage(EventId = 115059, Level = LogLevel.Information, Message = "KNX connection established → {Gateway}:{Port}"
)]
    private partial void LogConnectionEstablished(string gateway, int port);

    [LoggerMessage(EventId = 115060, Level = LogLevel.Error, Message = "KNX connection error"
)]
    private partial void LogConnectionError(Exception exception);

    [LoggerMessage(EventId = 115061, Level = LogLevel.Error, Message = "KNX connection error: {ErrorMessage}"
)]
    private partial void LogConnectionErrorMessage(string errorMessage);

    [LoggerMessage(EventId = 115062, Level = LogLevel.Information, Message = "🚀 Attempting KNX connection → {Gateway}:{Port} (attempt {AttemptNumber}/{MaxAttempts}: {ErrorMessage})"
)]
    private partial void LogConnectionRetryAttempt(
        string gateway,
        int port,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    [LoggerMessage(EventId = 115063, Level = LogLevel.Debug, Message = "Using KNX IP tunneling connection → {Gateway}:{Port}"
)]
    private partial void LogUsingIpTunneling(string gateway, int port);

    [LoggerMessage(EventId = 115064, Level = LogLevel.Debug, Message = "Using KNX IP routing connection → {Gateway}"
)]
    private partial void LogUsingIpRouting(string gateway);

    [LoggerMessage(EventId = 115065, Level = LogLevel.Debug, Message = "Using KNX USB device: {Device}"
)]
    private partial void LogUsingUsbDevice(string device);

    [LoggerMessage(EventId = 115066, Level = LogLevel.Error, Message = "Gateway address is required for {ConnectionType} connection"
)]
    private partial void LogGatewayRequired(string connectionType);

    [LoggerMessage(EventId = 115067, Level = LogLevel.Warning, Message = "No KNX USB devices found"
)]
    private partial void LogNoUsbDevicesFound();

    [LoggerMessage(EventId = 115068, Level = LogLevel.Error, Message = "Error creating KNX connector parameters"
)]
    private partial void LogConnectorParametersError(Exception exception);

    [LoggerMessage(EventId = 115069, Level = LogLevel.Error, Message = "Failed → resolve multicast address '{MulticastAddress}' → IPv4 address"
)]
    private partial void LogMulticastAddressResolutionFailed(string multicastAddress);

    [LoggerMessage(EventId = 115070, Level = LogLevel.Error, Message = "Error resolving multicast address '{MulticastAddress}': {ErrorMessage}"
)]
    private partial void LogMulticastAddressError(string multicastAddress, string errorMessage);

    [LoggerMessage(EventId = 115071, Level = LogLevel.Debug, Message = "Using specific KNX USB device '{ConfiguredDevice}': {ActualDevice}"
)]
    private partial void LogUsingSpecificUsbDevice(string configuredDevice, string actualDevice);

    [LoggerMessage(EventId = 115072, Level = LogLevel.Warning, Message = "Configured USB device '{ConfiguredDevice}' not found, using first available device"
)]
    private partial void LogSpecificUsbDeviceNotFound(string configuredDevice);

    [LoggerMessage(EventId = 115073, Level = LogLevel.Debug, Message = "KNX group value received: {GroupAddress} = {Value}"
)]
    private partial void LogGroupValueReceived(string groupAddress, object value);

    [LoggerMessage(EventId = 115074, Level = LogLevel.Debug, Message = "KNX command mapped: {GroupAddress} → {CommandType}"
)]
    private partial void LogCommandMapped(string groupAddress, string commandType);

    [LoggerMessage(EventId = 115075, Level = LogLevel.Error, Message = "Error processing KNX group value from {GroupAddress}"
)]
    private partial void LogGroupValueProcessingError(string groupAddress, Exception exception);

    [LoggerMessage(EventId = 115076, Level = LogLevel.Warning, Message = "KNX service not connected for operation: {Operation}"
)]
    private partial void LogNotConnected(string operation);

    [LoggerMessage(EventId = 115077, Level = LogLevel.Warning, Message = "No KNX group address found for status {StatusId} on target {TargetId}"
)]
    private partial void LogGroupAddressNotFound(string statusId, int targetId);

    [LoggerMessage(EventId = 115078, Level = LogLevel.Error, Message = "Error sending KNX status {StatusId} → target {TargetId}"
)]
    private partial void LogSendStatusError(string statusId, int targetId, Exception exception);

    [LoggerMessage(EventId = 115079, Level = LogLevel.Debug, Message = "KNX group value written: {GroupAddress} = {Value}"
)]
    private partial void LogGroupValueWritten(string groupAddress, object value);

    [LoggerMessage(EventId = 115080, Level = LogLevel.Error, Message = "Error writing KNX group value {GroupAddress} = {Value}"
)]
    private partial void LogWriteGroupValueError(string groupAddress, object value, Exception exception);

    [LoggerMessage(EventId = 115081, Level = LogLevel.Debug, Message = "KNX group value read: {GroupAddress} = {Value}"
)]
    private partial void LogGroupValueRead(string groupAddress, object value);

    [LoggerMessage(EventId = 115082, Level = LogLevel.Error, Message = "Error reading KNX group value from {GroupAddress}"
)]
    private partial void LogReadGroupValueError(string groupAddress, Exception exception);

    [LoggerMessage(EventId = 115083, Level = LogLevel.Error, Message = "Error handling KNX status notification {StatusId} for target {TargetId}"
)]
    private partial void LogStatusNotificationError(string statusId, int targetId, Exception exception);

    [LoggerMessage(EventId = 115084, Level = LogLevel.Debug, Message = "KNX reconnect timer started"
)]
    private partial void LogReconnectTimerStarted();

    [LoggerMessage(EventId = 115085, Level = LogLevel.Debug, Message = "Attempting KNX reconnection"
)]
    private partial void LogAttemptingReconnection();

    [LoggerMessage(EventId = 115086, Level = LogLevel.Warning, Message = "Invalid target ID '{TargetId}' for status '{StatusId}' - expected integer"
)]
    private partial void LogInvalidTargetId(string statusId, string targetId);

    [LoggerMessage(EventId = 115087, Level = LogLevel.Error, Message = "Error executing KNX command {CommandType}"
)]
    private partial void LogCommandExecutionError(string commandType, Exception exception);

    [LoggerMessage(EventId = 115088, Level = LogLevel.Warning, Message = "Error during KNX service disposal: {ErrorMessage}"
)]
    private partial void LogKnxDisposalError(string errorMessage);

    [LoggerMessage(EventId = 115089, Level = LogLevel.Debug, Message = "KNX read request received for status address {GroupAddress}"
)]
    private partial void LogReadRequestReceived(string groupAddress);

    [LoggerMessage(EventId = 115090, Level = LogLevel.Debug, Message = "KNX read response sent: {GroupAddress} = {Value} (StatusId: {StatusId}, Target: {TargetId})"
)]
    private partial void LogReadResponseSent(string groupAddress, object value, string statusId, string targetId);

    [LoggerMessage(EventId = 115091, Level = LogLevel.Warning, Message = "KNX read request for unmapped status address {GroupAddress} - no response sent"
)]
    private partial void LogUnmappedReadRequest(string groupAddress);

    [LoggerMessage(EventId = 115092, Level = LogLevel.Error, Message = "Error handling KNX read request for {GroupAddress}"
)]
    private partial void LogReadRequestError(string groupAddress, Exception exception);

    [LoggerMessage(EventId = 115093, Level = LogLevel.Warning, Message = "KNX read request for {GroupAddress} - current state not available for {EntityType} {EntityId}"
)]
    private partial void LogReadRequestStateNotAvailable(string groupAddress, string entityType, string entityId);

    [LoggerMessage(EventId = 115094, Level = LogLevel.Error, Message = "Error retrieving current state for {EntityType} {EntityId}, StatusId: {StatusId}"
)]
    private partial void LogCurrentStateRetrievalError(string entityType, string entityId, string statusId, Exception exception);

    #endregion
}
