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
        await _hubContext.Clients.All.SendAsync("ZonePlaybackChanged", notification.ZoneIndex, notification.PlaybackState.ToString(), cancellationToken);
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
