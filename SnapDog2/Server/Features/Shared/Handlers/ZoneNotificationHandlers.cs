namespace SnapDog2.Server.Features.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Handles zone state change notifications to log and process status updates.
/// </summary>
public partial class ZoneStateNotificationHandler
    : INotificationHandler<ZoneVolumeChangedNotification>,
        INotificationHandler<ZoneMuteChangedNotification>,
        INotificationHandler<ZonePlaybackStateChangedNotification>,
        INotificationHandler<ZoneTrackChangedNotification>,
        INotificationHandler<ZonePlaylistChangedNotification>,
        INotificationHandler<ZoneRepeatModeChangedNotification>,
        INotificationHandler<ZoneShuffleModeChangedNotification>,
        INotificationHandler<ZoneStateChangedNotification>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZoneStateNotificationHandler> _logger;

    public ZoneStateNotificationHandler(IServiceProvider serviceProvider, ILogger<ZoneStateNotificationHandler> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    [LoggerMessage(6001, LogLevel.Information, "Zone {ZoneId} volume changed to {Volume}")]
    private partial void LogVolumeChange(int zoneId, int volume);

    [LoggerMessage(6002, LogLevel.Information, "Zone {ZoneId} mute changed to {IsMuted}")]
    private partial void LogMuteChange(int zoneId, bool isMuted);

    [LoggerMessage(6003, LogLevel.Information, "Zone {ZoneId} playback state changed to {PlaybackState}")]
    private partial void LogPlaybackStateChange(int zoneId, string playbackState);

    [LoggerMessage(6004, LogLevel.Information, "Zone {ZoneId} track changed to {TrackTitle} by {Artist}")]
    private partial void LogTrackChange(int zoneId, string trackTitle, string artist);

    [LoggerMessage(
        6005,
        LogLevel.Information,
        "Zone {ZoneId} playlist changed to {PlaylistName} (Index: {PlaylistIndex})"
    )]
    private partial void LogPlaylistChange(int zoneId, string playlistName, int playlistIndex);

    [LoggerMessage(
        6006,
        LogLevel.Information,
        "Zone {ZoneId} repeat mode changed - Track: {TrackRepeat}, Playlist: {PlaylistRepeat}"
    )]
    private partial void LogRepeatModeChange(int zoneId, bool trackRepeat, bool playlistRepeat);

    [LoggerMessage(6007, LogLevel.Information, "Zone {ZoneId} shuffle mode changed to {ShuffleEnabled}")]
    private partial void LogShuffleModeChange(int zoneId, bool shuffleEnabled);

    [LoggerMessage(6008, LogLevel.Information, "Zone {ZoneId} complete state changed")]
    private partial void LogStateChange(int zoneId);

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVolumeChange(notification.ZoneId, notification.Volume);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_VOLUME",
            notification.ZoneId,
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogMuteChange(notification.ZoneId, notification.IsMuted);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_MUTE",
            notification.ZoneId,
            notification.IsMuted,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        this.LogPlaybackStateChange(notification.ZoneId, playbackStateString);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_PLAYBACK_STATE",
            notification.ZoneId,
            playbackStateString,
            cancellationToken
        );
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogTrackChange(notification.ZoneId, notification.TrackInfo.Title, notification.TrackInfo.Artist);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_TRACK",
            notification.ZoneId,
            notification.TrackInfo,
            cancellationToken
        );
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogPlaylistChange(notification.ZoneId, notification.PlaylistInfo.Name, notification.PlaylistIndex);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_PLAYLIST",
            notification.ZoneId,
            new { PlaylistInfo = notification.PlaylistInfo, PlaylistIndex = notification.PlaylistIndex },
            cancellationToken
        );
    }

    public async Task Handle(ZoneRepeatModeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogRepeatModeChange(
            notification.ZoneId,
            notification.TrackRepeatEnabled,
            notification.PlaylistRepeatEnabled
        );

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_REPEAT_MODE",
            notification.ZoneId,
            new
            {
                TrackRepeatEnabled = notification.TrackRepeatEnabled,
                PlaylistRepeatEnabled = notification.PlaylistRepeatEnabled,
            },
            cancellationToken
        );
    }

    public async Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogShuffleModeChange(notification.ZoneId, notification.ShuffleEnabled);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            "ZONE_SHUFFLE_MODE",
            notification.ZoneId,
            notification.ShuffleEnabled,
            cancellationToken
        );
    }

    public async Task Handle(ZoneStateChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogStateChange(notification.ZoneId);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync("ZONE_STATE", notification.ZoneId, notification, cancellationToken);
    }

    /// <summary>
    /// Publishes zone events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    private async Task PublishToExternalSystemsAsync<T>(
        string eventType,
        int zoneId,
        T payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Publish to MQTT if enabled
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                await mqttService.PublishZoneStatusAsync(zoneId, eventType, payload, cancellationToken);
            }

            // Publish to KNX if enabled
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                await knxService.PublishZoneStatusAsync(zoneId, eventType, payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(
                ex,
                "Failed to publish {EventType} for zone {ZoneId} to external systems",
                eventType,
                zoneId
            );
        }
    }
}
