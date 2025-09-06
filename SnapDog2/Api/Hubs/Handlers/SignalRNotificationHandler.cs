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

    [LoggerMessage(EventId = 20001, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} volume changed to {Volume}")]
    private partial void LogZoneVolumeChanged(int zoneIndex, int volume);

    [LoggerMessage(EventId = 20002, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} mute changed to {IsMuted}")]
    private partial void LogZoneMuteChanged(int zoneIndex, bool isMuted);

    [LoggerMessage(EventId = 20003, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} playback changed to {PlaybackState}")]
    private partial void LogZonePlaybackChanged(int zoneIndex, string playbackState);

    [LoggerMessage(EventId = 20004, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} track changed to {TrackIndex}")]
    private partial void LogZoneTrackChanged(int zoneIndex, int? trackIndex);

    [LoggerMessage(EventId = 20005, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} playlist changed to {PlaylistIndex}")]
    private partial void LogZonePlaylistChanged(int zoneIndex, int? playlistIndex);

    [LoggerMessage(EventId = 20006, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} track repeat changed to {Enabled}")]
    private partial void LogZoneTrackRepeatChanged(int zoneIndex, bool enabled);

    [LoggerMessage(EventId = 20007, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} playlist repeat changed to {Enabled}")]
    private partial void LogZonePlaylistRepeatChanged(int zoneIndex, bool enabled);

    [LoggerMessage(EventId = 20008, Level = LogLevel.Information, Message = "SignalR: Zone {ZoneIndex} shuffle changed to {ShuffleEnabled}")]
    private partial void LogZoneShuffleChanged(int zoneIndex, bool shuffleEnabled);

    [LoggerMessage(EventId = 20009, Level = LogLevel.Debug, Message = "SignalR: Zone {ZoneIndex} progress: {Position}ms ({Progress}%)")]
    private partial void LogZoneProgress(int zoneIndex, long position, double progress);

    [LoggerMessage(EventId = 20010, Level = LogLevel.Information, Message = "SignalR: Client {ClientIndex} volume changed to {Volume}")]
    private partial void LogClientVolumeChanged(int clientIndex, int volume);

    [LoggerMessage(EventId = 20011, Level = LogLevel.Information, Message = "SignalR: Client {ClientIndex} mute changed to {IsMuted}")]
    private partial void LogClientMuteChanged(int clientIndex, bool isMuted);

    [LoggerMessage(EventId = 20012, Level = LogLevel.Information, Message = "SignalR: Client {ClientIndex} zone changed to {ZoneIndex}")]
    private partial void LogClientZoneChanged(int clientIndex, string zoneIndex);

    [LoggerMessage(EventId = 20013, Level = LogLevel.Information, Message = "SignalR: Client {ClientIndex} connection changed to {Connected}")]
    private partial void LogClientConnectionChanged(int clientIndex, bool connected);

    [LoggerMessage(EventId = 20014, Level = LogLevel.Information, Message = "SignalR: Client {ClientIndex} latency changed to {Latency}ms")]
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
