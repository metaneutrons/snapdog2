namespace SnapDog2.Api.Hubs.Handlers;

using Cortex.Mediator.Notifications;
using Microsoft.AspNetCore.SignalR;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Server.Zones.Notifications;

/// <summary>
/// Bridges mediator notifications to SignalR hub notifications
/// </summary>
public class SignalRNotificationHandler :
    INotificationHandler<ZoneVolumeChangedNotification>,
    INotificationHandler<ZoneMuteChangedNotification>,
    INotificationHandler<ZonePlaybackStateChangedNotification>,
    INotificationHandler<ZoneTrackChangedNotification>,
    INotificationHandler<ZonePlaylistChangedNotification>,
    INotificationHandler<ZoneTrackRepeatChangedNotification>,
    INotificationHandler<ZonePlaylistRepeatChangedNotification>,
    INotificationHandler<ZoneShuffleModeChangedNotification>,
    INotificationHandler<ClientVolumeChangedNotification>,
    INotificationHandler<ClientMuteChangedNotification>
{
    private readonly IHubContext<SnapDogHub> _hubContext;
    private readonly ILogger<SignalRNotificationHandler> _logger;

    public SignalRNotificationHandler(IHubContext<SnapDogHub> hubContext, ILogger<SignalRNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} volume changed to {Volume}", notification.ZoneIndex, notification.Volume);
        await _hubContext.Clients.All.SendAsync("ZoneVolumeChanged", notification.ZoneIndex, notification.Volume, cancellationToken);
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} mute changed to {IsMuted}", notification.ZoneIndex, notification.IsMuted);
        await _hubContext.Clients.All.SendAsync("ZoneMuteChanged", notification.ZoneIndex, notification.IsMuted, cancellationToken);
    }

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} playback changed to {PlaybackState}", notification.ZoneIndex, notification.PlaybackState);
        await _hubContext.Clients.All.SendAsync("ZonePlaybackChanged", notification.ZoneIndex, notification.PlaybackState.ToString().ToLowerInvariant(), cancellationToken);
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} track changed to {TrackIndex}", notification.ZoneIndex, notification.TrackIndex);
        await _hubContext.Clients.All.SendAsync("ZoneTrackMetadataChanged", notification.ZoneIndex, notification.TrackInfo, cancellationToken);
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} playlist changed to {PlaylistIndex}", notification.ZoneIndex, notification.PlaylistIndex);
        await _hubContext.Clients.All.SendAsync("ZonePlaylistChanged", notification.ZoneIndex, notification.PlaylistInfo, cancellationToken);
    }

    public async Task Handle(ZoneTrackRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} track repeat changed to {Enabled}", notification.ZoneIndex, notification.Enabled);
        await _hubContext.Clients.All.SendAsync("ZoneRepeatModeChanged", notification.ZoneIndex, notification.Enabled, false, cancellationToken);
    }

    public async Task Handle(ZonePlaylistRepeatChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} playlist repeat changed to {Enabled}", notification.ZoneIndex, notification.Enabled);
        await _hubContext.Clients.All.SendAsync("ZoneRepeatModeChanged", notification.ZoneIndex, false, notification.Enabled, cancellationToken);
    }

    public async Task Handle(ZoneShuffleModeChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Zone {ZoneIndex} shuffle changed to {ShuffleEnabled}", notification.ZoneIndex, notification.ShuffleEnabled);
        await _hubContext.Clients.All.SendAsync("ZoneShuffleChanged", notification.ZoneIndex, notification.ShuffleEnabled, cancellationToken);
    }

    public async Task Handle(ClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Client {ClientIndex} volume changed to {Volume}", notification.ClientIndex, notification.Volume);
        await _hubContext.Clients.All.SendAsync("ClientVolumeChanged", notification.ClientIndex, notification.Volume, cancellationToken);
    }

    public async Task Handle(ClientMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔔 SignalR: Client {ClientIndex} mute changed to {IsMuted}", notification.ClientIndex, notification.IsMuted);
        await _hubContext.Clients.All.SendAsync("ClientMuteChanged", notification.ClientIndex, notification.IsMuted, cancellationToken);
    }
}
