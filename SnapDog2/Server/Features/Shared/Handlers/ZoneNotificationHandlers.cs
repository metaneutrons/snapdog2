namespace SnapDog2.Server.Features.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
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
        INotificationHandler<ZoneTrackRepeatChangedNotification>,
        INotificationHandler<ZonePlaylistRepeatChangedNotification>,
        INotificationHandler<ZoneShuffleModeChangedNotification>,
        INotificationHandler<ZoneStateChangedNotification>
{
    private readonly INotificationQueue? _queue;
    private readonly ILogger<ZoneStateNotificationHandler> _logger;

    public ZoneStateNotificationHandler(INotificationQueue? queue, ILogger<ZoneStateNotificationHandler> logger)
    {
        this._queue = queue;
        this._logger = logger;
    }

    [LoggerMessage(6001, LogLevel.Information, "Zone {ZoneIndex} volume changed to {Volume}")]
    private partial void LogVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(6002, LogLevel.Information, "Zone {ZoneIndex} mute changed to {IsMuted}")]
    private partial void LogMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(6003, LogLevel.Information, "Zone {ZoneIndex} playback state changed to {PlaybackState}")]
    private partial void LogPlaybackStateChange(int zoneIndex, string playbackState);

    [LoggerMessage(6004, LogLevel.Information, "Zone {ZoneIndex} track changed to {TrackTitle} by {Artist}")]
    private partial void LogTrackChange(int zoneIndex, string trackTitle, string artist);

    [LoggerMessage(
        6005,
        LogLevel.Information,
        "Zone {ZoneIndex} playlist changed to {PlaylistName} (Index: {PlaylistIndex})"
    )]
    private partial void LogPlaylistChange(int zoneIndex, string playlistName, int playlistIndex);

    [LoggerMessage(6006, LogLevel.Information, "Zone {ZoneIndex} track repeat changed to {TrackRepeatEnabled}")]
    private partial void LogTrackRepeatChange(int zoneIndex, bool trackRepeatEnabled);

    [LoggerMessage(6007, LogLevel.Information, "Zone {ZoneIndex} playlist repeat changed to {PlaylistRepeatEnabled}")]
    private partial void LogPlaylistRepeatChange(int zoneIndex, bool playlistRepeatEnabled);

    [LoggerMessage(6008, LogLevel.Information, "Zone {ZoneIndex} shuffle mode changed to {ShuffleEnabled}")]
    private partial void LogShuffleModeChange(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(6008, LogLevel.Information, "Zone {ZoneIndex} complete state changed")]
    private partial void LogStateChange(int zoneIndex);

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVolumeChange(notification.ZoneIndex, notification.Volume);

        // Enqueue for background processing
        var eventType = StatusIdAttribute.GetStatusId<ZoneVolumeChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.Volume,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogMuteChange(notification.ZoneIndex, notification.IsMuted);

        var eventType = StatusIdAttribute.GetStatusId<ZoneMuteChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.IsMuted,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        this.LogPlaybackStateChange(notification.ZoneIndex, playbackStateString);

        var eventType = StatusIdAttribute.GetStatusId<ZonePlaybackStateChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                playbackStateString,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogTrackChange(notification.ZoneIndex, notification.TrackInfo.Title, notification.TrackInfo.Artist);

        var eventType = StatusIdAttribute.GetStatusId<ZoneTrackChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.TrackInfo,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogPlaylistChange(notification.ZoneIndex, notification.PlaylistInfo.Name, notification.PlaylistIndex);

        var eventType = StatusIdAttribute.GetStatusId<ZonePlaylistChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                new { notification.PlaylistInfo, notification.PlaylistIndex },
                cancellationToken
            );
        }
    }

    public async Task Handle(ZoneTrackRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogTrackRepeatChange(notification.ZoneIndex, notification.Enabled);

        var eventType = StatusIdAttribute.GetStatusId<ZoneTrackRepeatChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.Enabled,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZonePlaylistRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogPlaylistRepeatChange(notification.ZoneIndex, notification.Enabled);

        var eventType = StatusIdAttribute.GetStatusId<ZonePlaylistRepeatChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.Enabled,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogShuffleModeChange(notification.ZoneIndex, notification.ShuffleEnabled);

        var eventType = StatusIdAttribute.GetStatusId<ZoneShuffleModeChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.ShuffleEnabled,
                cancellationToken
            );
        }
    }

    public async Task Handle(ZoneStateChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogStateChange(notification.ZoneIndex);

        var eventType = StatusIdAttribute.GetStatusId<ZoneStateChangedNotification>();
        if (this._queue != null)
        {
            await this._queue.EnqueueZoneAsync(
                eventType,
                notification.ZoneIndex,
                notification.ZoneState,
                cancellationToken
            );
        }
    }

    /// <summary>
    /// Publishes zone events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    // No longer needed: publishing is handled by background service via queue
}
