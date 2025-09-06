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
namespace SnapDog2.Server.Snapcast.Handlers;

using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Server.Snapcast.Notifications;

/// <summary>
/// Handles Snapcast event notifications and processes them for the application.
/// This demonstrates how Snapcast events flow through the mediator pattern.
/// </summary>
public partial class SnapcastEventNotificationHandler(
    IMediator mediator,
    IClientStateStore clientStateStore,
    ISnapcastStateRepository snapcastStateRepository,
    ILogger<SnapcastEventNotificationHandler> logger)
    : INotificationHandler<SnapcastClientConnectedNotification>,
        INotificationHandler<SnapcastClientDisconnectedNotification>,
        INotificationHandler<SnapcastClientVolumeChangedNotification>,
        INotificationHandler<SnapcastClientLatencyChangedNotification>,
        INotificationHandler<SnapcastClientNameChangedNotification>,
        INotificationHandler<SnapcastGroupMuteChangedNotification>,
        INotificationHandler<SnapcastConnectionEstablishedNotification>,
        INotificationHandler<SnapcastConnectionLostNotification>
{
    private readonly IMediator _mediator = mediator;
    private readonly IClientStateStore _clientStateStore = clientStateStore;
    private readonly ISnapcastStateRepository _snapcastStateRepository = snapcastStateRepository;
    private readonly ILogger<SnapcastEventNotificationHandler> _logger = logger;

    #region Logging

    [LoggerMessage(EventId = 113250, Level = LogLevel.Information, Message = "Snapcast client connected: {ClientIndex} ({ClientName})"
)]
    private partial void LogClientConnected(string clientIndex, string clientName);

    [LoggerMessage(EventId = 113251, Level = LogLevel.Information, Message = "Snapcast client disconnected: {ClientIndex} ({ClientName})"
)]
    private partial void LogClientDisconnected(string clientIndex, string clientName);

    [LoggerMessage(EventId = 113252, Level = LogLevel.Information, Message = "Snapcast client volume changed: {ClientIndex} -> {Volume}% (Muted: {Muted})"
)]
    private partial void LogClientVolumeChanged(string clientIndex, int volume, bool muted);

    [LoggerMessage(EventId = 113253, Level = LogLevel.Information, Message = "Snapcast group mute changed: {GroupId} -> Muted: {Muted}"
)]
    private partial void LogGroupMuteChanged(string groupId, bool muted);

    [LoggerMessage(EventId = 113254, Level = LogLevel.Information, Message = "Snapcast connection established"
)]
    private partial void LogConnectionEstablished();

    [LoggerMessage(EventId = 113255, Level = LogLevel.Warning, Message = "Snapcast connection lost: {Reason}"
)]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(EventId = 113256, Level = LogLevel.Information, Message = "Snapcast client latency changed: {ClientIndex} -> {LatencyMs}ms"
)]
    private partial void LogClientLatencyChanged(int clientIndex, int latencyMs);

    [LoggerMessage(EventId = 113257, Level = LogLevel.Warning, Message = "Invalid client index format for connection: {ClientIndex}"
)]
    private partial void LogInvalidClientIndexForConnection(string clientIndex);

    [LoggerMessage(EventId = 113258, Level = LogLevel.Debug, Message = "Updated client {ClientIndex} storage: Connected=true"
)]
    private partial void LogClientStorageUpdatedConnected(int clientIndex);

    [LoggerMessage(EventId = 113259, Level = LogLevel.Warning, Message = "Client {ClientIndex} state not found in storage for connection update"
)]
    private partial void LogClientStateNotFoundForConnection(int clientIndex);

    [LoggerMessage(EventId = 113260, Level = LogLevel.Warning, Message = "Invalid client index format for disconnection: {ClientIndex}"
)]
    private partial void LogInvalidClientIndexForDisconnection(string clientIndex);

    [LoggerMessage(EventId = 113261, Level = LogLevel.Debug, Message = "Updated client {ClientIndex} storage: Connected=false"
)]
    private partial void LogClientStorageUpdatedDisconnected(int clientIndex);

    [LoggerMessage(EventId = 113262, Level = LogLevel.Warning, Message = "Client {ClientIndex} state not found in storage for disconnection update"
)]
    private partial void LogClientStateNotFoundForDisconnection(int clientIndex);

    [LoggerMessage(EventId = 113263, Level = LogLevel.Warning, Message = "Invalid client index format: {ClientIndex}"
)]
    private partial void LogInvalidClientIndexFormat(string clientIndex);

    [LoggerMessage(EventId = 113264, Level = LogLevel.Debug, Message = "Updated client {ClientIndex} storage: Volume={Volume}, Mute={Mute}"
)]
    private partial void LogClientStorageUpdatedVolume(int clientIndex, int volume, bool mute);

    [LoggerMessage(EventId = 113265, Level = LogLevel.Warning, Message = "Client {ClientIndex} state not found in storage for volume update"
)]
    private partial void LogClientStateNotFoundForVolume(int clientIndex);

    [LoggerMessage(EventId = 113266, Level = LogLevel.Warning, Message = "Invalid client index format for latency change: {ClientIndex}"
)]
    private partial void LogInvalidClientIndexForLatency(string clientIndex);

    [LoggerMessage(EventId = 113267, Level = LogLevel.Debug, Message = "Updated client {ClientIndex} storage: LatencyMs={LatencyMs}"
)]
    private partial void LogClientStorageUpdatedLatency(int clientIndex, int latencyMs);

    [LoggerMessage(EventId = 113268, Level = LogLevel.Warning, Message = "Client {ClientIndex} state not found in storage for latency update"
)]
    private partial void LogClientStateNotFoundForLatency(int clientIndex);

    [LoggerMessage(EventId = 113269, Level = LogLevel.Warning, Message = "Invalid client index format for name change: {ClientIndex}"
)]
    private partial void LogInvalidClientIndexForName(string clientIndex);

    [LoggerMessage(EventId = 113270, Level = LogLevel.Debug, Message = "SnapcastClientVolumeChangedNotification received for client {ClientIndex}"
)]
    private partial void LogVolumeNotificationReceived(string clientIndex);

    [LoggerMessage(EventId = 113271, Level = LogLevel.Debug, Message = "Publishing ClientVolumeChangedNotification for SignalR - Client {ClientIndex}, Volume {Volume}"
)]
    private partial void LogPublishingVolumeNotification(int clientIndex, int volume);

    [LoggerMessage(EventId = 113272, Level = LogLevel.Debug, Message = "Publishing ClientMuteChangedNotification for SignalR - Client {ClientIndex}, Muted {IsMuted}"
)]
    private partial void LogPublishingMuteNotification(int clientIndex, bool isMuted);

    #endregion

    #region Client Events

    public async Task Handle(SnapcastClientConnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientConnected(notification.Client.Id, notification.Client.Config.Name);

        // Map Snapcast client ID to SnapDog client index using MAC address
        var clientIndex = this._snapcastStateRepository.GetClientIndexBySnapcastId(notification.Client.Id);
        if (clientIndex == null)
        {
            this.LogInvalidClientIndexForConnection(notification.Client.Id);
            return;
        }

        // 1. Update storage (single source of truth) - Command-Status Flow Pattern
        var currentState = this._clientStateStore.GetClientState(clientIndex.Value);
        if (currentState != null)
        {
            var updatedState = currentState with { Connected = true };
            this._clientStateStore.SetClientState(clientIndex.Value, updatedState);

            this.LogClientStorageUpdatedConnected(clientIndex.Value);
        }
        else
        {
            this.LogClientStateNotFoundForConnection(clientIndex.Value);
        }

        // 2. Publish status notification to trigger integration publishing
        await this._mediator.PublishAsync(new ClientConnectionChangedNotification
        {
            ClientIndex = clientIndex.Value,
            IsConnected = true
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientDisconnected(notification.Client.Id, notification.Client.Config.Name);

        // Map Snapcast client ID to SnapDog client index using MAC address
        var clientIndex = this._snapcastStateRepository.GetClientIndexBySnapcastId(notification.Client.Id);
        if (clientIndex == null)
        {
            this.LogInvalidClientIndexForDisconnection(notification.Client.Id);
            return;
        }

        // 1. Update storage (single source of truth) - Command-Status Flow Pattern
        var currentState = this._clientStateStore.GetClientState(clientIndex.Value);
        if (currentState != null)
        {
            var updatedState = currentState with { Connected = false };
            this._clientStateStore.SetClientState(clientIndex.Value, updatedState);

            this.LogClientStorageUpdatedDisconnected(clientIndex.Value);
        }
        else
        {
            this.LogClientStateNotFoundForDisconnection(clientIndex.Value);
        }

        // 2. Publish status notification to trigger integration publishing
        await this._mediator.PublishAsync(new ClientConnectionChangedNotification
        {
            ClientIndex = clientIndex.Value,
            IsConnected = false
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientVolumeChanged(notification.ClientIndex, notification.Volume.Percent, notification.Volume.Muted);
        this.LogVolumeNotificationReceived(notification.ClientIndex);

        // Parse client index from string to int (now it should be a proper client Index)
        if (!int.TryParse(notification.ClientIndex, out var clientIndex))
        {
            this.LogInvalidClientIndexFormat(notification.ClientIndex);
            return;
        }

        // 1. Update storage (single source of truth) - Command-Status Flow Pattern
        var currentState = this._clientStateStore.GetClientState(clientIndex);
        if (currentState != null)
        {
            var updatedState = currentState with
            {
                Volume = notification.Volume.Percent,
                Mute = notification.Volume.Muted
            };
            this._clientStateStore.SetClientState(clientIndex, updatedState);

            this.LogClientStorageUpdatedVolume(clientIndex, notification.Volume.Percent, notification.Volume.Muted);
        }
        else
        {
            this.LogClientStateNotFoundForVolume(clientIndex);
        }

        // 2. Publish status notifications to trigger integration publishing
        await this._mediator.PublishAsync(new ClientVolumeStatusNotification(clientIndex, notification.Volume.Percent), cancellationToken);

        // Also publish mute status notification for KNX integration
        await this._mediator.PublishAsync(new ClientMuteStatusNotification(clientIndex, notification.Volume.Muted), cancellationToken);

        // 3. Publish SignalR notifications for real-time UI updates
        this.LogPublishingVolumeNotification(clientIndex, notification.Volume.Percent);
        await this._mediator.PublishAsync(new ClientVolumeChangedNotification
        {
            ClientIndex = clientIndex,
            Volume = notification.Volume.Percent
        }, cancellationToken);

        this.LogPublishingMuteNotification(clientIndex, notification.Volume.Muted);
        await this._mediator.PublishAsync(new ClientMuteChangedNotification
        {
            ClientIndex = clientIndex,
            IsMuted = notification.Volume.Muted
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientLatencyChangedNotification notification, CancellationToken cancellationToken)
    {
        // Parse client index from string to int
        if (!int.TryParse(notification.ClientIndex, out var clientIndex))
        {
            this.LogInvalidClientIndexForLatency(notification.ClientIndex);
            return;
        }

        this.LogClientLatencyChanged(clientIndex, notification.LatencyMs);

        // 1. Update storage (single source of truth) - Command-Status Flow Pattern
        var currentState = this._clientStateStore.GetClientState(clientIndex);
        if (currentState != null)
        {
            var updatedState = currentState with { LatencyMs = notification.LatencyMs };
            this._clientStateStore.SetClientState(clientIndex, updatedState);

            this.LogClientStorageUpdatedLatency(clientIndex, notification.LatencyMs);
        }
        else
        {
            this.LogClientStateNotFoundForLatency(clientIndex);
        }

        // 2. Publish status notification to trigger integration publishing
        await this._mediator.PublishAsync(new ClientLatencyStatusNotification(clientIndex, notification.LatencyMs), cancellationToken);
    }

    public async Task Handle(SnapcastClientNameChangedNotification notification, CancellationToken cancellationToken)
    {
        // Parse client index from string to int
        if (!int.TryParse(notification.ClientIndex, out var clientIndex))
        {
            this.LogInvalidClientIndexForName(notification.ClientIndex);
            return;
        }

        // Publish ClientNameChangedNotification to trigger MQTT/KNX updates
        await this._mediator.PublishAsync(new ClientNameChangedNotification
        {
            ClientIndex = clientIndex,
            Name = notification.Name
        }, cancellationToken);
    }

    #endregion

    #region Group Events

    public Task Handle(SnapcastGroupMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogGroupMuteChanged(notification.GroupId, notification.Muted);

        // Here you could:
        // - Update internal group state
        // - Publish MQTT group status updates
        // - Send KNX group notifications
        // - Update metrics
        // - Trigger group-based automation

        return Task.CompletedTask;
    }

    #endregion

    #region Connection Events

    public Task Handle(SnapcastConnectionEstablishedNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionEstablished();

        // Here you could:
        // - Initialize system state
        // - Publish connection status to MQTT
        // - Update health check status
        // - Trigger initialization routines

        return Task.CompletedTask;
    }

    public Task Handle(SnapcastConnectionLostNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionLost(notification.Reason);

        // Here you could:
        // - Update connection status
        // - Publish disconnection status to MQTT
        // - Update health check status
        // - Trigger reconnection logic
        // - Alert administrators

        return Task.CompletedTask;
    }

    #endregion
}
