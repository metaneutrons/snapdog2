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
namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Clients.Notifications;
using SnapDog2.Server.Features.Snapcast.Notifications;

/// <summary>
/// Handles Snapcast event notifications and processes them for the application.
/// This demonstrates how Snapcast events flow through the mediator pattern.
/// </summary>
public partial class SnapcastEventNotificationHandler(
    IMediator mediator,
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
    private readonly ILogger<SnapcastEventNotificationHandler> _logger = logger;

    #region Logging

    [LoggerMessage(
        EventId = 3000,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Snapcast client connected: {ClientIndex} ({ClientName})"
    )]
    private partial void LogClientConnected(string clientIndex, string clientName);

    [LoggerMessage(
        EventId = 3001,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Snapcast client disconnected: {ClientIndex} ({ClientName})"
    )]
    private partial void LogClientDisconnected(string clientIndex, string clientName);

    [LoggerMessage(
        EventId = 3002,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Snapcast client volume changed: {ClientIndex} -> {Volume}% (Muted: {Muted})"
    )]
    private partial void LogClientVolumeChanged(string clientIndex, int volume, bool muted);

    [LoggerMessage(
        EventId = 3003,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Snapcast group mute changed: {GroupId} -> Muted: {Muted}"
    )]
    private partial void LogGroupMuteChanged(string groupId, bool muted);

    [LoggerMessage(
        EventId = 3004,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Snapcast connection established"
    )]
    private partial void LogConnectionEstablished();

    [LoggerMessage(
        EventId = 3005,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Snapcast connection lost: {Reason}"
    )]
    private partial void LogConnectionLost(string reason);

    #endregion

    #region Client Events

    public async Task Handle(SnapcastClientConnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientConnected(notification.Client.Id, notification.Client.Config.Name);

        // Publish ClientConnectionChangedNotification to trigger MQTT/KNX updates
        await this._mediator.PublishAsync(new ClientConnectionChangedNotification
        {
            ClientIndex = int.Parse(notification.Client.Id),
            IsConnected = true
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientDisconnected(notification.Client.Id, notification.Client.Config.Name);

        // Publish ClientConnectionChangedNotification to trigger MQTT/KNX updates
        await this._mediator.PublishAsync(new ClientConnectionChangedNotification
        {
            ClientIndex = int.Parse(notification.Client.Id),
            IsConnected = false
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientVolumeChanged(notification.ClientIndex, notification.Volume.Percent, notification.Volume.Muted);

        // Parse client index from string to int (now it should be a proper client ID)
        if (!int.TryParse(notification.ClientIndex, out var clientIndex))
        {
            this._logger.LogWarning("Invalid client index format: {ClientIndex}", notification.ClientIndex);
            return;
        }

        // Publish ClientVolumeChangedNotification to trigger MQTT/KNX updates
        await this._mediator.PublishAsync(new ClientVolumeChangedNotification
        {
            ClientIndex = clientIndex,
            Volume = notification.Volume.Percent
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientLatencyChangedNotification notification, CancellationToken cancellationToken)
    {
        // Parse client index from string to int
        if (!int.TryParse(notification.ClientIndex, out var clientIndex))
        {
            this._logger.LogWarning("Invalid client index format for latency change: {ClientIndex}", notification.ClientIndex);
            return;
        }

        // Publish ClientLatencyChangedNotification to trigger MQTT/KNX updates
        await this._mediator.PublishAsync(new ClientLatencyChangedNotification
        {
            ClientIndex = clientIndex,
            LatencyMs = notification.LatencyMs
        }, cancellationToken);
    }

    public async Task Handle(SnapcastClientNameChangedNotification notification, CancellationToken cancellationToken)
    {
        // Parse client index from string to int
        if (!int.TryParse(notification.ClientIndex, out var clientIndex))
        {
            this._logger.LogWarning("Invalid client index format for name change: {ClientIndex}", notification.ClientIndex);
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
