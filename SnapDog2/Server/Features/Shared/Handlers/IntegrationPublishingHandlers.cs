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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Constants;
using SnapDog2.Core.Mappers;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Features.Clients.Notifications;
using SnapDog2.Server.Features.Shared.Notifications;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Integration publishing handlers that publish status changes to all external integrations (MQTT, KNX, etc.).
/// Implements the Command-Status Flow pattern by consolidating all integration publishing logic.
/// </summary>
public partial class IntegrationPublishingHandlers(
    IServiceProvider serviceProvider,
    ILogger<IntegrationPublishingHandlers> logger
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
        INotificationHandler<ZoneTrackMetadataChangedNotification>,
        INotificationHandler<ZoneTrackTitleChangedNotification>,
        INotificationHandler<ZoneTrackArtistChangedNotification>,
        INotificationHandler<ZoneTrackAlbumChangedNotification>,
        // Track playback status notification handlers
        INotificationHandler<ZoneTrackProgressChangedNotification>,
        INotificationHandler<ZoneTrackPlayingStatusChangedNotification>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<IntegrationPublishingHandlers> _logger = logger;

    // Cache for previous zone states to enable change detection
    private readonly ConcurrentDictionary<int, PublishableZoneState> _previousZoneStates = new();

    // Cache for previous client states to enable change detection
    private readonly ConcurrentDictionary<int, PublishableClientState> _previousClientStates = new();

    // Debouncing: Track last publish time for each zone to prevent rapid-fire publishing
    private readonly ConcurrentDictionary<int, DateTime> _lastZonePublishTime = new();
    private readonly TimeSpan _zonePublishDebounceTime = TimeSpan.FromMilliseconds(500); // 500ms debounce

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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.ClientVolumeStatus,
            notification.ClientIndex,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.ClientMuteStatus,
            notification.ClientIndex,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.ClientLatencyStatus,
            notification.ClientIndex,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.ClientZoneStatus,
            notification.ClientIndex,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.ClientConnected,
            notification.ClientIndex,
            notification.IsConnected,
            cancellationToken
        );
    }

    public async Task Handle(ClientStateNotification notification, CancellationToken cancellationToken)
    {
        LogClientStateChange(notification.ClientIndex);

        // Convert to simplified publishable format for MQTT
        var publishableClientState = PublishableClientStateMapper.ToPublishableClientState(notification.State);

        // Check if this represents a meaningful change compared to the previous state
        var previousState = _previousClientStates.GetValueOrDefault(notification.ClientIndex);

        if (PublishableClientStateMapper.HasMeaningfulChange(previousState, publishableClientState))
        {
            LogClientStatePublishing(notification.ClientIndex, previousState == null ? "first-time" : "changed");

            // Update cache with new state
            _previousClientStates[notification.ClientIndex] = publishableClientState;

            await PublishClientStatusAsync(
                StatusIdAttribute.GetStatusId<ClientStateNotification>(),
                notification.ClientIndex.ToString(),
                publishableClientState,
                cancellationToken
            );
        }
        else
        {
            LogClientStateSkipped(notification.ClientIndex);
        }
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.VolumeStatus,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.MuteStatus,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.PlaybackState,
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

        // Also publish StatusChangedNotification for KNX integration (send track index, not full TrackInfo)
        await PublishKnxStatusAsync(
            StatusIds.TrackIndex,
            notification.ZoneIndex,
            notification.TrackInfo.Index,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.TrackRepeatStatus,
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
        // Convert to simplified publishable format for MQTT
        var publishableZoneState = PublishableZoneStateMapper.ToMqttZoneState(notification.ZoneState);

        // Check if this represents a meaningful change compared to the previous state
        var previousState = _previousZoneStates.GetValueOrDefault(notification.ZoneIndex);

        if (!PublishableZoneStateMapper.HasMeaningfulChange(previousState, publishableZoneState))
        {
            return; // No meaningful changes - don't publish at all
        }

        // We have meaningful changes - now check debouncing to prevent rapid-fire publishing
        var now = DateTime.UtcNow;
        var lastPublishTime = _lastZonePublishTime.GetValueOrDefault(notification.ZoneIndex, DateTime.MinValue);

        if (now - lastPublishTime < _zonePublishDebounceTime)
        {
            return; // Meaningful changes exist, but publishing too rapidly - debounce
        }

        // Update cache with new state and publish time
        _previousZoneStates[notification.ZoneIndex] = publishableZoneState;
        _lastZonePublishTime[notification.ZoneIndex] = now;

        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneStateChangedNotification>(),
            notification.ZoneIndex,
            publishableZoneState,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackMetadataChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackMetadataChange(notification.ZoneIndex, notification.TrackInfo.Title, notification.TrackInfo.Artist, notification.TrackInfo.Album ?? "Unknown");
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackMetadataChangedNotification>(),
            notification.ZoneIndex,
            notification.TrackInfo,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackTitleChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackTitleChange(notification.ZoneIndex, notification.Title);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackTitleChangedNotification>(),
            notification.ZoneIndex,
            notification.Title,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.TrackMetadataTitle,
            notification.ZoneIndex,
            notification.Title,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackArtistChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackArtistChange(notification.ZoneIndex, notification.Artist);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackArtistChangedNotification>(),
            notification.ZoneIndex,
            notification.Artist,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.TrackMetadataArtist,
            notification.ZoneIndex,
            notification.Artist,
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

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.TrackMetadataAlbum,
            notification.ZoneIndex,
            notification.Album,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackProgressChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackProgressChange(notification.ZoneIndex, notification.Progress);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackProgressChangedNotification>(),
            notification.ZoneIndex,
            notification.Progress,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.TrackProgressStatus,
            notification.ZoneIndex,
            notification.Progress,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackPlayingStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackPlayingStatusChange(notification.ZoneIndex, notification.IsPlaying);
        await PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackPlayingStatusChangedNotification>(),
            notification.ZoneIndex,
            notification.IsPlaying,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await PublishKnxStatusAsync(
            StatusIds.TrackPlayingStatus,
            notification.ZoneIndex,
            notification.IsPlaying,
            cancellationToken
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Publishes status change notification for KNX integration.
    /// </summary>
    private async Task PublishKnxStatusAsync<T>(string statusType, int targetIndex, T value, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Debug log to verify we're publishing the notification
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationPublishingHandlers>>();
        LogPublishingStatusChangedNotification(logger, statusType, targetIndex, value?.ToString() ?? "null");

        await mediator.PublishAsync(
            new StatusChangedNotification
            {
                StatusType = statusType,
                TargetIndex = targetIndex,
                Value = value ?? (object)"null"
            },
            cancellationToken
        );

        LogStatusChangedNotificationPublished(logger);
    }

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
    [LoggerMessage(
        EventId = 5003,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Client {ClientIndex} volume changed to {Volume}"
    )]
    private partial void LogClientVolumeChange(int clientIndex, int volume);

    [LoggerMessage(
        EventId = 5004,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Client {ClientIndex} mute changed to {IsMuted}"
    )]
    private partial void LogClientMuteChange(int clientIndex, bool isMuted);

    [LoggerMessage(
        EventId = 5005,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Client {ClientIndex} latency changed to {LatencyMs}ms"
    )]
    private partial void LogClientLatencyChange(int clientIndex, int latencyMs);

    [LoggerMessage(
        EventId = 5006,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Client {ClientIndex} zone changed from {PreviousZoneIndex} to {ZoneIndex}"
    )]
    private partial void LogClientZoneChange(int clientIndex, int? previousZoneIndex, int? zoneIndex);

    [LoggerMessage(
        EventId = 5007,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Client {ClientIndex} connection changed to {IsConnected}"
    )]
    private partial void LogClientConnectionChange(int clientIndex, bool isConnected);

    [LoggerMessage(
        EventId = 5008,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Client {ClientIndex} complete state changed"
    )]
    private partial void LogClientStateChange(int clientIndex);

    [LoggerMessage(
        EventId = 5031,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "📤 Publishing client {ClientIndex} state to MQTT ({ChangeType})"
    )]
    private partial void LogClientStatePublishing(int clientIndex, string changeType);

    [LoggerMessage(
        EventId = 5032,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "⏭️ Skipping client {ClientIndex} state publish - no meaningful changes"
    )]
    private partial void LogClientStateSkipped(int clientIndex);

    // Zone logging
    [LoggerMessage(
        EventId = 5009,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} volume changed to {Volume}"
    )]
    private partial void LogZoneVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(
        EventId = 5010,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} mute changed to {IsMuted}"
    )]
    private partial void LogZoneMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(
        EventId = 5011,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} playback state changed to {PlaybackState}"
    )]
    private partial void LogZonePlaybackStateChange(int zoneIndex, string playbackState);

    [LoggerMessage(
        EventId = 5012,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track changed to {TrackTitle} by {Artist}"
    )]
    private partial void LogZoneTrackChange(int zoneIndex, string trackTitle, string artist);

    [LoggerMessage(
        EventId = 5013,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} playlist changed to {PlaylistName} (Index: {PlaylistIndex})"
    )]
    private partial void LogZonePlaylistChange(int zoneIndex, string playlistName, int playlistIndex);

    [LoggerMessage(
        EventId = 5014,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track repeat changed to {TrackRepeatEnabled}"
    )]
    private partial void LogZoneTrackRepeatChange(int zoneIndex, bool trackRepeatEnabled);

    [LoggerMessage(
        EventId = 5015,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} playlist repeat changed to {PlaylistRepeatEnabled}"
    )]
    private partial void LogZonePlaylistRepeatChange(int zoneIndex, bool playlistRepeatEnabled);

    [LoggerMessage(
        EventId = 5016,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} shuffle mode changed to {ShuffleEnabled}"
    )]
    private partial void LogZoneShuffleModeChange(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(
        EventId = 5013,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track metadata changed: {Title} by {Artist} from {Album}"
    )]
    private partial void LogZoneTrackMetadataChange(int zoneIndex, string title, string artist, string album);

    [LoggerMessage(
        EventId = 5014,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track title changed to {Title}"
    )]
    private partial void LogZoneTrackTitleChange(int zoneIndex, string title);

    [LoggerMessage(
        EventId = 5015,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track artist changed to {Artist}"
    )]
    private partial void LogZoneTrackArtistChange(int zoneIndex, string artist);

    [LoggerMessage(
        EventId = 5017,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track album changed to {Album}"
    )]
    private partial void LogZoneTrackAlbumChange(int zoneIndex, string album);

    [LoggerMessage(
        EventId = 5018,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track progress changed to {Progress}%"
    )]
    private partial void LogZoneTrackProgressChange(int zoneIndex, double progress);

    [LoggerMessage(
        EventId = 5019,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} track playing status changed to {IsPlaying}"
    )]
    private partial void LogZoneTrackPlayingStatusChange(int zoneIndex, bool isPlaying);

    [LoggerMessage(
        EventId = 5020,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Zone {ZoneIndex} complete state changed"
    )]
    private partial void LogZoneStateChange(int zoneIndex);

    // Error logging
    [LoggerMessage(
        EventId = 5021,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Smart MQTT publisher not available for {EntityType} {EntityId} {EventType}"
    )]
    private partial void LogSmartPublisherNotAvailable(string entityType, string entityId, string eventType);

    [LoggerMessage(
        EventId = 5022,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to publish {EntityType} {EntityId} {EventType} to MQTT"
    )]
    private partial void LogPublishError(string entityType, string entityId, string eventType, Exception ex);

    [LoggerMessage(
        EventId = 5000,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Attempting to publish zone status: {EventType} for Zone {ZoneIndex}"
    )]
    private static partial void LogAttemptingToPublishZoneStatus(ILogger logger, string eventType, int zoneIndex);

    [LoggerMessage(
        EventId = 5001,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Smart publisher found, publishing zone status"
    )]
    private static partial void LogSmartPublisherFound(ILogger logger);

    [LoggerMessage(
        EventId = 5002,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "❌ Smart publisher not available"
    )]
    private static partial void LogSmartPublisherNotAvailableWarning(ILogger logger);

    [LoggerMessage(
        EventId = 5023,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🚀 Publishing StatusChangedNotification: {StatusType} for target {TargetIndex} with value {Value}"
    )]
    private static partial void LogPublishingStatusChangedNotification(ILogger logger, string StatusType, int TargetIndex, string Value);

    [LoggerMessage(
        EventId = 5024,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ StatusChangedNotification published successfully"
    )]
    private static partial void LogStatusChangedNotificationPublished(ILogger logger);

    #endregion
}

/// <summary>
/// KNX notification handler that forwards StatusChangedNotification to KNX service.
/// Located in same file as other handlers to ensure MediatR auto-discovery works.
/// </summary>
public partial class KnxIntegrationHandler : INotificationHandler<StatusChangedNotification>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<KnxIntegrationHandler> _logger;

    public KnxIntegrationHandler(IKnxService knxService, ILogger<KnxIntegrationHandler> logger)
    {
        _knxService = knxService;
        _logger = logger;
    }

    public async Task Handle(StatusChangedNotification notification, CancellationToken cancellationToken)
    {
        // Debug log to verify we're receiving notifications
        LogKnxIntegrationReceived(notification.StatusType, notification.TargetIndex, notification.Value?.ToString() ?? "null");

        try
        {
            LogCallingKnxService();
            // Ensure we have a non-null value for KNX service
            var knxValue = notification.Value ?? "null";
            await _knxService.SendStatusAsync(notification.StatusType, notification.TargetIndex, knxValue, cancellationToken);
            LogKnxServiceCompleted();
        }
        catch (Exception ex)
        {
            LogKnxServiceError(ex, ex.Message);
        }
    }

    #region LoggerMessage Methods

    [LoggerMessage(
        EventId = 5025,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🔔 KNX integration received: {StatusType} for target {TargetIndex} with value {Value}"
    )]
    private partial void LogKnxIntegrationReceived(string StatusType, int TargetIndex, string Value);

    [LoggerMessage(
        EventId = 5026,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Calling KNX service SendStatusAsync method"
    )]
    private partial void LogCallingKnxService();

    [LoggerMessage(
        EventId = 5027,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ KNX service SendStatusAsync method completed"
    )]
    private partial void LogKnxServiceCompleted();

    [LoggerMessage(
        EventId = 5028,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Error calling KNX service SendStatusAsync: {Error}"
    )]
    private partial void LogKnxServiceError(Exception ex, string Error);

    #endregion
}
