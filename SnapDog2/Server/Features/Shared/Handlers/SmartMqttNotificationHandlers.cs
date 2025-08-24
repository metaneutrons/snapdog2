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
namespace SnapDog2.Server.Features.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Features.Clients.Notifications;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Smart MQTT notification handlers that use the hybrid direct/queue publishing approach.
/// Replaces the inconsistent pattern with a unified, reliable MQTT publishing strategy.
/// </summary>
public partial class SmartMqttNotificationHandlers(
    IServiceProvider serviceProvider,
    ILogger<SmartMqttNotificationHandlers> logger
)
    :
    // Client notification handlers
    INotificationHandler<ClientVolumeStatusNotification>,
        INotificationHandler<ClientMuteStatusNotification>,
        INotificationHandler<ClientLatencyStatusNotification>,
        INotificationHandler<ClientZoneStatusNotification>,
        INotificationHandler<ClientConnectionStatusNotification>,
        INotificationHandler<ClientStateNotification>,
        // Zone notification handlers
        INotificationHandler<ZoneVolumeChangedNotification>,
        INotificationHandler<ZoneMuteChangedNotification>,
        INotificationHandler<ZonePlaybackStateChangedNotification>,
        INotificationHandler<ZoneTrackChangedNotification>,
        INotificationHandler<ZonePlaylistChangedNotification>,
        INotificationHandler<ZoneTrackRepeatChangedNotification>,
        INotificationHandler<ZonePlaylistRepeatChangedNotification>,
        INotificationHandler<ZoneShuffleModeChangedNotification>,
        INotificationHandler<ZoneStateChangedNotification>,
        // Track metadata notification handlers
        INotificationHandler<ZoneTrackAlbumChangedNotification>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<SmartMqttNotificationHandlers> _logger = logger;

    #region Client Notification Handlers

    public async Task Handle(ClientVolumeStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientVolumeChange(notification.ClientIndex, notification.Volume);
        await PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientVolumeStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ClientMuteStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientMuteChange(notification.ClientIndex, notification.Muted);
        await PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientMuteStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.Muted,
            cancellationToken
        );
    }

    public async Task Handle(ClientLatencyStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientLatencyChange(notification.ClientIndex, notification.LatencyMs);
        await PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientLatencyStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.LatencyMs,
            cancellationToken
        );
    }

    public async Task Handle(ClientZoneStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientZoneChange(notification.ClientIndex, null, notification.ZoneIndex);
        await PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientZoneStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.ZoneIndex,
            cancellationToken
        );
    }

    public async Task Handle(ClientConnectionStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientConnectionChange(notification.ClientIndex, notification.IsConnected);
        await PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientConnectionStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.IsConnected,
            cancellationToken
        );
    }

    public async Task Handle(ClientStateNotification notification, CancellationToken cancellationToken)
    {
        LogClientStateChange(notification.ClientIndex);
        await PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientStateNotification>(),
            notification.ClientIndex.ToString(),
            notification,
            cancellationToken
        );
    }

    #endregion

    #region Zone Notification Handlers

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneVolumeChange(notification.ZoneIndex, notification.Volume);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneVolumeChangedNotification>(),
            notification.ZoneIndex,
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneMuteChange(notification.ZoneIndex, notification.IsMuted);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneMuteChangedNotification>(),
            notification.ZoneIndex,
            notification.IsMuted,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        LogZonePlaybackStateChange(notification.ZoneIndex, playbackStateString);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZonePlaybackStateChangedNotification>(),
            notification.ZoneIndex,
            playbackStateString,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackChange(notification.ZoneIndex, notification.TrackInfo.Title, notification.TrackInfo.Artist);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackChangedNotification>(),
            notification.ZoneIndex,
            notification.TrackInfo,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZonePlaylistChange(notification.ZoneIndex, notification.PlaylistInfo.Name, notification.PlaylistIndex);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZonePlaylistChangedNotification>(),
            notification.ZoneIndex,
            new { PlaylistInfo = notification.PlaylistInfo, PlaylistIndex = notification.PlaylistIndex },
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackRepeatChange(notification.ZoneIndex, notification.Enabled);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackRepeatChangedNotification>(),
            notification.ZoneIndex,
            notification.Enabled,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaylistRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZonePlaylistRepeatChange(notification.ZoneIndex, notification.Enabled);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZonePlaylistRepeatChangedNotification>(),
            notification.ZoneIndex,
            notification.Enabled,
            cancellationToken
        );
    }

    public async Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneShuffleModeChange(notification.ZoneIndex, notification.ShuffleEnabled);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneShuffleModeChangedNotification>(),
            notification.ZoneIndex,
            notification.ShuffleEnabled,
            cancellationToken
        );
    }

    public async Task Handle(ZoneStateChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneStateChange(notification.ZoneIndex);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneStateChangedNotification>(),
            notification.ZoneIndex,
            notification,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackAlbumChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackAlbumChange(notification.ZoneIndex, notification.Album);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackAlbumChangedNotification>(),
            notification.ZoneIndex,
            notification.Album,
            cancellationToken
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Publishes client status using the smart MQTT publisher.
    /// </summary>
    private async Task PublishClientStatusAsync<T>(
        string eventType,
        string clientIndex,
        T payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var smartPublisher = _serviceProvider.GetService<ISmartMqttPublisher>();
            if (smartPublisher != null)
            {
                await smartPublisher.PublishClientStatusAsync(clientIndex, eventType, payload, cancellationToken);
            }
            else
            {
                LogSmartPublisherNotAvailable("Client", clientIndex, eventType);
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider disposed during shutdown - ignore
        }
        catch (Exception ex)
        {
            LogPublishError("Client", clientIndex, eventType, ex);
        }
    }

    /// <summary>
    /// Publishes zone status using the smart MQTT publisher.
    /// </summary>
    private async Task PublishZoneStatusAsync<T>(
        string eventType,
        int zoneIndex,
        T payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            LogAttemptingToPublishZoneStatus(_logger, eventType, zoneIndex);

            var smartPublisher = _serviceProvider.GetService<ISmartMqttPublisher>();
            if (smartPublisher != null)
            {
                LogSmartPublisherFound(_logger);
                await smartPublisher.PublishZoneStatusAsync(zoneIndex, eventType, payload, cancellationToken);
            }
            else
            {
                LogSmartPublisherNotAvailableWarning(_logger);
                LogSmartPublisherNotAvailable("Zone", zoneIndex.ToString(), eventType);
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider disposed during shutdown - ignore
        }
        catch (Exception ex)
        {
            LogPublishError("Zone", zoneIndex.ToString(), eventType, ex);
        }
    }

    #endregion

    #region Logging

    // Client logging
    [LoggerMessage(9001, LogLevel.Information, "Client {ClientIndex} volume changed to {Volume}")]
    private partial void LogClientVolumeChange(int clientIndex, int volume);

    [LoggerMessage(9002, LogLevel.Information, "Client {ClientIndex} mute changed to {IsMuted}")]
    private partial void LogClientMuteChange(int clientIndex, bool isMuted);

    [LoggerMessage(9003, LogLevel.Information, "Client {ClientIndex} latency changed to {LatencyMs}ms")]
    private partial void LogClientLatencyChange(int clientIndex, int latencyMs);

    [LoggerMessage(
        9004,
        LogLevel.Information,
        "Client {ClientIndex} zone changed from {PreviousZoneIndex} to {ZoneIndex}"
    )]
    private partial void LogClientZoneChange(int clientIndex, int? previousZoneIndex, int? zoneIndex);

    [LoggerMessage(9005, LogLevel.Information, "Client {ClientIndex} connection changed to {IsConnected}")]
    private partial void LogClientConnectionChange(int clientIndex, bool isConnected);

    [LoggerMessage(9006, LogLevel.Information, "Client {ClientIndex} complete state changed")]
    private partial void LogClientStateChange(int clientIndex);

    // Zone logging
    [LoggerMessage(9011, LogLevel.Information, "Zone {ZoneIndex} volume changed to {Volume}")]
    private partial void LogZoneVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(9012, LogLevel.Information, "Zone {ZoneIndex} mute changed to {IsMuted}")]
    private partial void LogZoneMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(9013, LogLevel.Information, "Zone {ZoneIndex} playback state changed to {PlaybackState}")]
    private partial void LogZonePlaybackStateChange(int zoneIndex, string playbackState);

    [LoggerMessage(9014, LogLevel.Information, "Zone {ZoneIndex} track changed to {TrackTitle} by {Artist}")]
    private partial void LogZoneTrackChange(int zoneIndex, string trackTitle, string artist);

    [LoggerMessage(
        9015,
        LogLevel.Information,
        "Zone {ZoneIndex} playlist changed to {PlaylistName} (Index: {PlaylistIndex})"
    )]
    private partial void LogZonePlaylistChange(int zoneIndex, string playlistName, int playlistIndex);

    [LoggerMessage(9016, LogLevel.Information, "Zone {ZoneIndex} track repeat changed to {TrackRepeatEnabled}")]
    private partial void LogZoneTrackRepeatChange(int zoneIndex, bool trackRepeatEnabled);

    [LoggerMessage(9017, LogLevel.Information, "Zone {ZoneIndex} playlist repeat changed to {PlaylistRepeatEnabled}")]
    private partial void LogZonePlaylistRepeatChange(int zoneIndex, bool playlistRepeatEnabled);

    [LoggerMessage(9018, LogLevel.Information, "Zone {ZoneIndex} shuffle mode changed to {ShuffleEnabled}")]
    private partial void LogZoneShuffleModeChange(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(9019, LogLevel.Information, "Zone {ZoneIndex} track album changed to {Album}")]
    private partial void LogZoneTrackAlbumChange(int zoneIndex, string album);

    [LoggerMessage(9019, LogLevel.Debug, "Zone {ZoneIndex} complete state changed")]
    private partial void LogZoneStateChange(int zoneIndex);

    // Error logging
    [LoggerMessage(
        9020,
        LogLevel.Warning,
        "Smart MQTT publisher not available for {EntityType} {EntityId} {EventType}"
    )]
    private partial void LogSmartPublisherNotAvailable(string entityType, string entityId, string eventType);

    [LoggerMessage(9021, LogLevel.Error, "Failed to publish {EntityType} {EntityId} {EventType} to MQTT")]
    private partial void LogPublishError(string entityType, string entityId, string eventType, Exception ex);

    [LoggerMessage(
        EventId = 9022,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Attempting to publish zone status: {EventType} for Zone {ZoneIndex}"
    )]
    private static partial void LogAttemptingToPublishZoneStatus(ILogger logger, string eventType, int zoneIndex);

    [LoggerMessage(
        EventId = 9023,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Smart publisher found, publishing zone status"
    )]
    private static partial void LogSmartPublisherFound(ILogger logger);

    [LoggerMessage(
        EventId = 9024,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "❌ Smart publisher not available"
    )]
    private static partial void LogSmartPublisherNotAvailableWarning(ILogger logger);

    #endregion
}
