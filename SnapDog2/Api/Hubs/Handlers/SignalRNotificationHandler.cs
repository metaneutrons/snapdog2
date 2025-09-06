namespace SnapDog2.Api.Hubs.Handlers;

using Cortex.Mediator.Notifications;
using Microsoft.AspNetCore.SignalR;
using SnapDog2.Api.Hubs.Notifications;
using SnapDog2.Server.Clients.Notifications;
using ServerClientNotifications = SnapDog2.Server.Clients.Notifications;
using ServerZoneNotifications = SnapDog2.Server.Zones.Notifications;

/// <summary>
/// Bridges mediator notifications to SignalR hub notifications
/// </summary>
public partial class SignalRNotificationHandler :
    INotificationHandler<ServerZoneNotifications.ZoneVolumeChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZoneMuteChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZonePlaybackStateChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZoneTrackChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZonePlaylistChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZoneTrackRepeatChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZonePlaylistRepeatChangedNotification>,
    INotificationHandler<ServerZoneNotifications.ZoneShuffleModeChangedNotification>,
    INotificationHandler<ZoneProgressChangedNotification>,
    INotificationHandler<ServerClientNotifications.ClientVolumeChangedNotification>,
    INotificationHandler<ServerClientNotifications.ClientMuteChangedNotification>,
    INotificationHandler<ServerClientNotifications.ClientZoneStatusNotification>,
    INotificationHandler<ServerClientNotifications.ClientConnectionChangedNotification>,
    INotificationHandler<ClientLatencyStatusNotification>
{
    private readonly IHubContext<SnapDogHub> _hubContext;
    private readonly ILogger<SignalRNotificationHandler> _logger;

    public SignalRNotificationHandler(IHubContext<SnapDogHub> hubContext, ILogger<SignalRNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [LoggerMessage(EventId = 113300, Level = LogLevel.Information, Message = "Zone {ZoneIndex} volume changed → {Volume}")]
    private partial void LogZoneVolumeChanged(int zoneIndex, int volume);

    [LoggerMessage(EventId = 113301, Level = LogLevel.Information, Message = "Zone {ZoneIndex} mute changed → {IsMuted}")]
    private partial void LogZoneMuteChanged(int zoneIndex, bool isMuted);

    [LoggerMessage(EventId = 113302, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playback changed → {PlaybackState}")]
    private partial void LogZonePlaybackChanged(int zoneIndex, string playbackState);

    [LoggerMessage(EventId = 113303, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track changed → {TrackIndex}")]
    private partial void LogZoneTrackChanged(int zoneIndex, int? trackIndex);

    [LoggerMessage(EventId = 113304, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playlist changed → {PlaylistIndex}")]
    private partial void LogZonePlaylistChanged(int zoneIndex, int? playlistIndex);

    [LoggerMessage(EventId = 113305, Level = LogLevel.Information, Message = "Zone {ZoneIndex} track repeat changed → {Enabled}")]
    private partial void LogZoneTrackRepeatChanged(int zoneIndex, bool enabled);

    [LoggerMessage(EventId = 113306, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playlist repeat changed → {Enabled}")]
    private partial void LogZonePlaylistRepeatChanged(int zoneIndex, bool enabled);

    [LoggerMessage(EventId = 113307, Level = LogLevel.Information, Message = "Zone {ZoneIndex} shuffle changed → {ShuffleEnabled}")]
    private partial void LogZoneShuffleChanged(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(EventId = 113308, Level = LogLevel.Debug, Message = "Zone {ZoneIndex} progress: {Position}ms ({Progress}%)")]
    private partial void LogZoneProgress(int zoneIndex, long position, double progress);

    [LoggerMessage(EventId = 113309, Level = LogLevel.Information, Message = "Client {ClientIndex} volume changed → {Volume}")]
    private partial void LogClientVolumeChanged(int clientIndex, int volume);

    [LoggerMessage(EventId = 113310, Level = LogLevel.Information, Message = "Client {ClientIndex} mute changed → {IsMuted}")]
    private partial void LogClientMuteChanged(int clientIndex, bool isMuted);

    [LoggerMessage(EventId = 113311, Level = LogLevel.Information, Message = "Client {ClientIndex} zone changed → {ZoneIndex}")]
    private partial void LogClientZoneChanged(int clientIndex, string zoneIndex);

    [LoggerMessage(EventId = 113312, Level = LogLevel.Information, Message = "Client {ClientIndex} connection changed → {Connected}")]
    private partial void LogClientConnectionChanged(int clientIndex, bool connected);

    [LoggerMessage(EventId = 113313, Level = LogLevel.Information, Message = "Client {ClientIndex} latency changed → {Latency}ms")]
    private partial void LogClientLatencyChanged(int clientIndex, int latency);

    public async Task Handle(ServerZoneNotifications.ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneVolumeChanged(notification.ZoneIndex, notification.Volume);
        await _hubContext.Clients.All.SendAsync("ZoneVolumeChanged", notification.ZoneIndex, notification.Volume, cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneMuteChanged(notification.ZoneIndex, notification.IsMuted);
        await _hubContext.Clients.All.SendAsync("ZoneMuteChanged", notification.ZoneIndex, notification.IsMuted, cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZonePlaybackChanged(notification.ZoneIndex, notification.PlaybackState.ToString());
        await _hubContext.Clients.All.SendAsync("ZonePlaybackChanged", notification.ZoneIndex, notification.PlaybackState.ToString().ToLowerInvariant(), cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackChanged(notification.ZoneIndex, notification.TrackIndex);
        await _hubContext.Clients.All.SendAsync("ZoneTrackMetadataChanged", notification.ZoneIndex, notification.TrackInfo, cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZonePlaylistChanged(notification.ZoneIndex, notification.PlaylistIndex);
        await _hubContext.Clients.All.SendAsync("ZonePlaylistChanged", notification.ZoneIndex, notification.PlaylistInfo, cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZoneTrackRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneTrackRepeatChanged(notification.ZoneIndex, notification.Enabled);
        await _hubContext.Clients.All.SendAsync("ZoneRepeatModeChanged", notification.ZoneIndex, notification.Enabled, false, cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZonePlaylistRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZonePlaylistRepeatChanged(notification.ZoneIndex, notification.Enabled);
        await _hubContext.Clients.All.SendAsync("ZoneRepeatModeChanged", notification.ZoneIndex, false, notification.Enabled, cancellationToken);
    }

    public async Task Handle(ServerZoneNotifications.ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneShuffleChanged(notification.ZoneIndex, notification.ShuffleEnabled);
        await _hubContext.Clients.All.SendAsync("ZoneShuffleChanged", notification.ZoneIndex, notification.ShuffleEnabled, cancellationToken);
    }

    public async Task Handle(ZoneProgressChangedNotification notification, CancellationToken cancellationToken)
    {
        LogZoneProgress(notification.ZoneIndex, notification.Position, notification.Progress);
        await _hubContext.Clients.All.SendAsync("TrackProgress", notification.ZoneIndex, notification.Position, notification.Progress, cancellationToken);
    }

    public async Task Handle(ServerClientNotifications.ClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        LogClientVolumeChanged(notification.ClientIndex, notification.Volume);
        await _hubContext.Clients.All.SendAsync("ClientVolumeChanged", notification.ClientIndex, notification.Volume, cancellationToken);
    }

    public async Task Handle(ServerClientNotifications.ClientMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        LogClientMuteChanged(notification.ClientIndex, notification.IsMuted);
        await _hubContext.Clients.All.SendAsync("ClientMuteChanged", notification.ClientIndex, notification.IsMuted, cancellationToken);
    }

    public async Task Handle(ServerClientNotifications.ClientZoneStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientZoneChanged(notification.ClientIndex, notification.ZoneIndex?.ToString() ?? "unassigned");
        await _hubContext.Clients.All.SendAsync("ClientZoneChanged", notification.ClientIndex, notification.ZoneIndex, cancellationToken);
    }

    public async Task Handle(ServerClientNotifications.ClientConnectionChangedNotification notification, CancellationToken cancellationToken)
    {
        LogClientConnectionChanged(notification.ClientIndex, notification.IsConnected);
        await _hubContext.Clients.All.SendAsync("ClientConnected", notification.ClientIndex, notification.IsConnected, cancellationToken);
    }

    public async Task Handle(ClientLatencyStatusNotification notification, CancellationToken cancellationToken)
    {
        LogClientLatencyChanged(notification.ClientIndex, notification.LatencyMs);
        await _hubContext.Clients.All.SendAsync("ClientLatencyChanged", notification.ClientIndex, notification.LatencyMs, cancellationToken);
    }
}
