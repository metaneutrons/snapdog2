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

using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Snapcast.Notifications;

/// <summary>
/// Handles Snapcast event notifications and processes them for the application.
/// This demonstrates how Snapcast events flow through the mediator pattern.
/// </summary>
public partial class SnapcastEventNotificationHandler(ILogger<SnapcastEventNotificationHandler> logger)
    : INotificationHandler<SnapcastClientConnectedNotification>,
        INotificationHandler<SnapcastClientDisconnectedNotification>,
        INotificationHandler<SnapcastClientVolumeChangedNotification>,
        INotificationHandler<SnapcastGroupMuteChangedNotification>,
        INotificationHandler<SnapcastConnectionEstablishedNotification>,
        INotificationHandler<SnapcastConnectionLostNotification>
{
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

    public Task Handle(SnapcastClientConnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientConnected(notification.Client.Id, notification.Client.Config.Name);

        // Here you could:
        // - Update internal client state
        // - Publish MQTT status updates
        // - Send KNX notifications
        // - Update metrics
        // - Trigger other business logic

        return Task.CompletedTask;
    }

    public Task Handle(SnapcastClientDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientDisconnected(notification.Client.Id, notification.Client.Config.Name);

        // Here you could:
        // - Update internal client state
        // - Publish MQTT status updates
        // - Send KNX notifications
        // - Update metrics
        // - Trigger reconnection logic

        return Task.CompletedTask;
    }

    public Task Handle(SnapcastClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientVolumeChanged(notification.ClientIndex, notification.Volume.Percent, notification.Volume.Muted);

        // Here you could:
        // - Update internal client state
        // - Publish MQTT volume updates
        // - Send KNX volume status
        // - Update metrics
        // - Trigger volume-based automation

        return Task.CompletedTask;
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
