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
namespace SnapDog2.Server.Shared.Handlers;

using System.Collections.Concurrent;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Server.Shared.Notifications;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Constants;
using SnapDog2.Shared.Models;

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
    private readonly ConcurrentDictionary<int, ZoneState> _previousZoneStates = new();

    // Cache for previous client states to enable change detection
    private readonly ConcurrentDictionary<int, ClientState> _previousClientStates = new();

    // Debouncing: Track last publish time for each zone to prevent rapid-fire publishing
    private readonly ConcurrentDictionary<int, DateTime> _lastZonePublishTime = new();
    private readonly TimeSpan _zonePublishDebounceTime = TimeSpan.FromMilliseconds(500); // 500ms debounce

    #region Client Notification Handlers

    public async Task Handle(ClientVolumeStatusNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientVolumeChange(notification.ClientIndex, notification.Volume);
        await this.PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientVolumeStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.Volume,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.ClientVolumeStatus,
            notification.ClientIndex,
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ClientMuteStatusNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientMuteChange(notification.ClientIndex, notification.Muted);
        await this.PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientMuteStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.Muted,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.ClientMuteStatus,
            notification.ClientIndex,
            notification.Muted,
            cancellationToken
        );
    }

    public async Task Handle(ClientLatencyStatusNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientLatencyChange(notification.ClientIndex, notification.LatencyMs);
        await this.PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientLatencyStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.LatencyMs,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.ClientLatencyStatus,
            notification.ClientIndex,
            notification.LatencyMs,
            cancellationToken
        );
    }

    public async Task Handle(ClientZoneStatusNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientZoneChange(notification.ClientIndex, null, notification.ZoneIndex);
        await this.PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientZoneStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.ZoneIndex,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.ClientZoneStatus,
            notification.ClientIndex,
            notification.ZoneIndex,
            cancellationToken
        );
    }

    public async Task Handle(ClientConnectionStatusNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientConnectionChange(notification.ClientIndex, notification.IsConnected);
        await this.PublishClientStatusAsync(
            StatusIdAttribute.GetStatusId<ClientConnectionStatusNotification>(),
            notification.ClientIndex.ToString(),
            notification.IsConnected,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.ClientConnected,
            notification.ClientIndex,
            notification.IsConnected,
            cancellationToken
        );
    }

    public async Task Handle(ClientStateNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientStateChange(notification.ClientIndex);

        // Check if this represents a meaningful change compared to the previous state
        var previousState = this._previousClientStates.GetValueOrDefault(notification.ClientIndex);

        if (HasMeaningfulClientChange(previousState, notification.State))
        {
            this.LogClientStatePublishing(notification.ClientIndex, previousState == null ? "first-time" : "changed");

            // Update cache with new state
            this._previousClientStates[notification.ClientIndex] = notification.State;

            await this.PublishClientStatusAsync(
                StatusIdAttribute.GetStatusId<ClientStateNotification>(),
                notification.ClientIndex.ToString(),
                notification.State,
                cancellationToken
            );
        }
        else
        {
            this.LogClientStateSkipped(notification.ClientIndex);
        }
    }

    #endregion

    #region Zone Notification Handlers

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneVolumeChange(notification.ZoneIndex, notification.Volume);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneVolumeChangedNotification>(),
            notification.ZoneIndex,
            notification.Volume,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.VolumeStatus,
            notification.ZoneIndex,
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneMuteChange(notification.ZoneIndex, notification.IsMuted);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneMuteChangedNotification>(),
            notification.ZoneIndex,
            notification.IsMuted,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.MuteStatus,
            notification.ZoneIndex,
            notification.IsMuted,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        this.LogZonePlaybackStateChange(notification.ZoneIndex, playbackStateString);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZonePlaybackStateChangedNotification>(),
            notification.ZoneIndex,
            playbackStateString,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.PlaybackState,
            notification.ZoneIndex,
            playbackStateString,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackChange(notification.ZoneIndex, notification.TrackInfo.Title, notification.TrackInfo.Artist);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackChangedNotification>(),
            notification.ZoneIndex,
            notification.TrackInfo,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration (send track index, not full TrackInfo)
        await this.PublishKnxStatusAsync(
            StatusIds.TrackIndex,
            notification.ZoneIndex,
            notification.TrackInfo.Index,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZonePlaylistChange(notification.ZoneIndex, notification.PlaylistInfo.Name, notification.PlaylistIndex);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZonePlaylistChangedNotification>(),
            notification.ZoneIndex,
            new { notification.PlaylistInfo, notification.PlaylistIndex },
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackRepeatChange(notification.ZoneIndex, notification.Enabled);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackRepeatChangedNotification>(),
            notification.ZoneIndex,
            notification.Enabled,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.TrackRepeatStatus,
            notification.ZoneIndex,
            notification.Enabled,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaylistRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZonePlaylistRepeatChange(notification.ZoneIndex, notification.Enabled);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZonePlaylistRepeatChangedNotification>(),
            notification.ZoneIndex,
            notification.Enabled,
            cancellationToken
        );
    }

    public async Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneShuffleModeChange(notification.ZoneIndex, notification.ShuffleEnabled);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneShuffleModeChangedNotification>(),
            notification.ZoneIndex,
            notification.ShuffleEnabled,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackMetadataChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackMetadataChange(notification.ZoneIndex, notification.TrackInfo.Title, notification.TrackInfo.Artist, notification.TrackInfo.Album ?? "Unknown");
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackMetadataChangedNotification>(),
            notification.ZoneIndex,
            notification.TrackInfo,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackTitleChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackTitleChange(notification.ZoneIndex, notification.Title);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackTitleChangedNotification>(),
            notification.ZoneIndex,
            notification.Title,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.TrackMetadataTitle,
            notification.ZoneIndex,
            notification.Title,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackArtistChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackArtistChange(notification.ZoneIndex, notification.Artist);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackArtistChangedNotification>(),
            notification.ZoneIndex,
            notification.Artist,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.TrackMetadataArtist,
            notification.ZoneIndex,
            notification.Artist,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackAlbumChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackAlbumChange(notification.ZoneIndex, notification.Album);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackAlbumChangedNotification>(),
            notification.ZoneIndex,
            notification.Album,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.TrackMetadataAlbum,
            notification.ZoneIndex,
            notification.Album,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackProgressChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackProgressChange(notification.ZoneIndex, notification.Progress);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackProgressChangedNotification>(),
            notification.ZoneIndex,
            notification.Progress,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
            StatusIds.TrackProgressStatus,
            notification.ZoneIndex,
            notification.Progress,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackPlayingStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneTrackPlayingStatusChange(notification.ZoneIndex, notification.IsPlaying);
        await this.PublishZoneStatusAsync(
            StatusIdAttribute.GetStatusId<ZoneTrackPlayingStatusChangedNotification>(),
            notification.ZoneIndex,
            notification.IsPlaying,
            cancellationToken
        );

        // Also publish StatusChangedNotification for KNX integration
        await this.PublishKnxStatusAsync(
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
        using var scope = this._serviceProvider.CreateScope();
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
            var smartPublisher = this._serviceProvider.GetService<ISmartMqttPublisher>();
            if (smartPublisher != null)
            {
                await smartPublisher.PublishClientStatusAsync(clientIndex, eventType, payload, cancellationToken);
            }
            else
            {
                this.LogSmartPublisherNotAvailable("Client", clientIndex, eventType);
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider disposed during shutdown - ignore
        }
        catch (Exception ex)
        {
            this.LogPublishError("Client", clientIndex, eventType, ex);
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
            LogAttemptingToPublishZoneStatus(this._logger, eventType, zoneIndex);

            var smartPublisher = this._serviceProvider.GetService<ISmartMqttPublisher>();
            if (smartPublisher != null)
            {
                LogSmartPublisherFound(this._logger);
                await smartPublisher.PublishZoneStatusAsync(zoneIndex, eventType, payload, cancellationToken);
            }
            else
            {
                LogSmartPublisherNotAvailableWarning(this._logger);
                this.LogSmartPublisherNotAvailable("Zone", zoneIndex.ToString(), eventType);
            }
        }
        catch (ObjectDisposedException)
        {
            // Service provider disposed during shutdown - ignore
        }
        catch (Exception ex)
        {
            this.LogPublishError("Zone", zoneIndex.ToString(), eventType, ex);
        }
    }

    #endregion

    #region Logging

    // Client logging
    [LoggerMessage(EventId = 113100, Level = LogLevel.Information, Message = "Client {ClientIndex} volume changed → {Volume}"
)]
    private partial void LogClientVolumeChange(int clientIndex, int volume);

    [LoggerMessage(EventId = 113101, Level = LogLevel.Information, Message = "Client {ClientIndex} mute changed → {IsMuted}"
)]
    private partial void LogClientMuteChange(int clientIndex, bool isMuted);

    [LoggerMessage(EventId = 113102, Level = LogLevel.Information, Message = "Client {ClientIndex} latency changed → {LatencyMs}ms"
)]
    private partial void LogClientLatencyChange(int clientIndex, int latencyMs);

    [LoggerMessage(EventId = 113103, Level = LogLevel.Information, Message = "Client {ClientIndex} zone changed from {PreviousZoneIndex} → {ZoneIndex}"
)]
    private partial void LogClientZoneChange(int clientIndex, int? previousZoneIndex, int? zoneIndex);

    [LoggerMessage(EventId = 113104, Level = LogLevel.Information, Message = "Client {ClientIndex} connection changed → {IsConnected}"
)]
    private partial void LogClientConnectionChange(int clientIndex, bool isConnected);

    [LoggerMessage(EventId = 113105, Level = LogLevel.Information, Message = "Client {ClientIndex} complete state changed"
)]
    private partial void LogClientStateChange(int clientIndex);

    [LoggerMessage(EventId = 113106, Level = LogLevel.Debug, Message = "Publishing client {ClientIndex} state → MQTT ({ChangeType})"
)]
    private partial void LogClientStatePublishing(int clientIndex, string changeType);

    [LoggerMessage(EventId = 113107, Level = LogLevel.Debug, Message = "⏭️ Skipping client {ClientIndex} state publish - no meaningful changes"
)]
    private partial void LogClientStateSkipped(int clientIndex);

    /// <summary>
    /// Determines if there's a meaningful change between client states.
    /// </summary>
    private static bool HasMeaningfulClientChange(ClientState? previous, ClientState current)
    {
        if (previous == null)
        {
            return true;
        }

        return previous.Name != current.Name ||
               previous.Volume != current.Volume ||
               previous.Mute != current.Mute ||
               previous.Connected != current.Connected ||
               previous.ZoneIndex != current.ZoneIndex;
    }

    // Zone logging
    [LoggerMessage(EventId = 113108, Level = LogLevel.Information, Message = "Zone {ZoneIndex} volume changed → {Volume}"
)]
    private partial void LogZoneVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(EventId = 113109, Level = LogLevel.Information, Message = "Zone {ZoneIndex} mute changed → {IsMuted}"
)]
    private partial void LogZoneMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(EventId = 113110, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playback state changed → {PlaybackState}"
)]
    private partial void LogZonePlaybackStateChange(int zoneIndex, string playbackState);

    [LoggerMessage(EventId = 113111, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track changed → {TrackTitle} by {Artist}"
)]
    private partial void LogZoneTrackChange(int zoneIndex, string trackTitle, string artist);

    [LoggerMessage(EventId = 113116, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playlist changed → {PlaylistName} (Index: {PlaylistIndex})"
)]
    private partial void LogZonePlaylistChange(int zoneIndex, string playlistName, int playlistIndex);

    [LoggerMessage(EventId = 113117, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track repeat changed → {TrackRepeatEnabled}"
)]
    private partial void LogZoneTrackRepeatChange(int zoneIndex, bool trackRepeatEnabled);

    [LoggerMessage(EventId = 113118, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playlist repeat changed → {PlaylistRepeatEnabled}"
)]
    private partial void LogZonePlaylistRepeatChange(int zoneIndex, bool playlistRepeatEnabled);

    [LoggerMessage(EventId = 113115, Level = LogLevel.Information, Message = "Zone {ZoneIndex} shuffle mode changed → {ShuffleEnabled}"
)]
    private partial void LogZoneShuffleModeChange(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(EventId = 113116, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track metadata changed: {Title} by {Artist} from {Album}"
)]
    private partial void LogZoneTrackMetadataChange(int zoneIndex, string title, string artist, string album);

    [LoggerMessage(EventId = 113117, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track title changed → {Title}"
)]
    private partial void LogZoneTrackTitleChange(int zoneIndex, string title);

    [LoggerMessage(EventId = 113118, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track artist changed → {Artist}"
)]
    private partial void LogZoneTrackArtistChange(int zoneIndex, string artist);

    [LoggerMessage(EventId = 113119, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track album changed → {Album}"
)]
    private partial void LogZoneTrackAlbumChange(int zoneIndex, string album);

    [LoggerMessage(EventId = 113120, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track progress changed → {Progress}%"
)]
    private partial void LogZoneTrackProgressChange(int zoneIndex, double progress);

    [LoggerMessage(EventId = 113121, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track playing status changed → {IsPlaying}"
)]
    private partial void LogZoneTrackPlayingStatusChange(int zoneIndex, bool isPlaying);

    [LoggerMessage(EventId = 113122, Level = LogLevel.Debug, Message = "Zone {ZoneIndex} complete state changed"
)]
    private partial void LogZoneStateChange(int zoneIndex);

    // Error logging
    [LoggerMessage(EventId = 113123, Level = LogLevel.Warning, Message = "Smart MQTT publisher not available for {EntityType} {EntityId} {EventType}"
)]
    private partial void LogSmartPublisherNotAvailable(string entityType, string entityId, string eventType);

    [LoggerMessage(EventId = 113124, Level = LogLevel.Error, Message = "Failed → publish {EntityType} {EntityId} {EventType} → MQTT"
)]
    private partial void LogPublishError(string entityType, string entityId, string eventType, Exception ex);

    [LoggerMessage(EventId = 113125, Level = LogLevel.Debug, Message = "Attempting → publish zone status: {EventType} for Zone {ZoneIndex}"
)]
    private static partial void LogAttemptingToPublishZoneStatus(ILogger logger, string eventType, int zoneIndex);

    [LoggerMessage(EventId = 113126, Level = LogLevel.Debug, Message = "Smart publisher found, publishing zone status"
)]
    private static partial void LogSmartPublisherFound(ILogger logger);

    [LoggerMessage(EventId = 113127, Level = LogLevel.Warning, Message = "❌ Smart publisher not available"
)]
    private static partial void LogSmartPublisherNotAvailableWarning(ILogger logger);

    [LoggerMessage(EventId = 113128, Level = LogLevel.Information, Message = "🚀 Publishing StatusChangedNotification: {StatusType} for target {TargetIndex} with value {Value}"
)]
    private static partial void LogPublishingStatusChangedNotification(ILogger logger, string StatusType, int TargetIndex, string Value);

    [LoggerMessage(EventId = 113129, Level = LogLevel.Information, Message = "✅ StatusChangedNotification published successfully"
)]
    private static partial void LogStatusChangedNotificationPublished(ILogger logger);

    #endregion
}

/// <summary>
/// KNX notification handler that forwards StatusChangedNotification to KNX service.
/// Located in same file as other handlers to ensure MediatR auto-discovery works.
/// </summary>
public partial class KnxIntegrationHandler(IKnxService knxService, ILogger<KnxIntegrationHandler> logger)
    : INotificationHandler<StatusChangedNotification>
{
    public async Task Handle(StatusChangedNotification notification, CancellationToken cancellationToken)
    {
        // Debug log to verify we're receiving notifications
        this.LogKnxIntegrationReceived(notification.StatusType, notification.TargetIndex, notification.Value?.ToString() ?? "null");

        try
        {
            this.LogCallingKnxService();
            // Ensure we have a non-null value for KNX service
            var knxValue = notification.Value ?? "null";
            await knxService.SendStatusAsync(notification.StatusType, notification.TargetIndex, knxValue, cancellationToken);
            this.LogKnxServiceCompleted();
        }
        catch (Exception ex)
        {
            this.LogKnxServiceError(ex, ex.Message);
        }
    }

    #region LoggerMessage Methods

    [LoggerMessage(EventId = 113130, Level = LogLevel.Information, Message = "KNX integration received: {StatusType} for target {TargetIndex} with value {Value}"
)]
    private partial void LogKnxIntegrationReceived(string StatusType, int TargetIndex, string Value);

    [LoggerMessage(EventId = 113131, Level = LogLevel.Information, Message = "✅ Calling KNX service SendStatusAsync method"
)]
    private partial void LogCallingKnxService();

    [LoggerMessage(EventId = 113132, Level = LogLevel.Information, Message = "✅ KNX service SendStatusAsync method completed"
)]
    private partial void LogKnxServiceCompleted();

    [LoggerMessage(EventId = 113133, Level = LogLevel.Error, Message = "❌ Error calling KNX service SendStatusAsync: {Error}"
)]
    private partial void LogKnxServiceError(Exception ex, string Error);

    #endregion
}
