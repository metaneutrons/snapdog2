namespace SnapDog2.Server.Features.Shared.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Shared.Notifications;

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
    private readonly ILogger<ZoneStateNotificationHandler> _logger;

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

    public ZoneStateNotificationHandler(ILogger<ZoneStateNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogVolumeChange(notification.ZoneId, notification.Volume);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        LogMuteChange(notification.ZoneId, notification.IsMuted);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var playbackStateString = notification.PlaybackState.ToString().ToLowerInvariant();
        LogPlaybackStateChange(notification.ZoneId, playbackStateString);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        LogTrackChange(notification.ZoneId, notification.TrackInfo.Title, notification.TrackInfo.Artist);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        LogPlaylistChange(notification.ZoneId, notification.PlaylistInfo.Name, notification.PlaylistIndex);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZoneRepeatModeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogRepeatModeChange(notification.ZoneId, notification.TrackRepeatEnabled, notification.PlaylistRepeatEnabled);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogShuffleModeChange(notification.ZoneId, notification.ShuffleEnabled);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ZoneStateChangedNotification notification, CancellationToken cancellationToken)
    {
        LogStateChange(notification.ZoneId);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }
}
