using Cortex.Mediator.Notifications;
using SnapDog2.Core.Abstractions;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Zones.Notifications;

namespace SnapDog2.Infrastructure.Notifications;

public class ZoneStateSignalRHandler(
    IZoneUpdateService zoneUpdateService,
    IZoneManager zoneManager) :
    INotificationHandler<ZonePlaybackStateChangedNotification>,
    INotificationHandler<ZoneVolumeChangedNotification>,
    INotificationHandler<ZoneMuteChangedNotification>,
    INotificationHandler<ZoneTrackChangedNotification>
{
    private readonly IZoneUpdateService _zoneUpdateService = zoneUpdateService;
    private readonly IZoneManager _zoneManager = zoneManager;

    public async Task Handle(ZonePlaybackStateChangedNotification notification, CancellationToken cancellationToken)
    {
        var zoneStateResult = await _zoneManager.GetZoneStateAsync(notification.ZoneIndex);
        if (zoneStateResult.IsSuccess && zoneStateResult.Value != null)
        {
            await _zoneUpdateService.BroadcastZoneStateUpdate(zoneStateResult.Value);
        }
    }

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        var zoneStateResult = await _zoneManager.GetZoneStateAsync(notification.ZoneIndex);
        if (zoneStateResult.IsSuccess && zoneStateResult.Value != null)
        {
            await _zoneUpdateService.BroadcastZoneStateUpdate(zoneStateResult.Value);
        }
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        var zoneStateResult = await _zoneManager.GetZoneStateAsync(notification.ZoneIndex);
        if (zoneStateResult.IsSuccess && zoneStateResult.Value != null)
        {
            await _zoneUpdateService.BroadcastZoneStateUpdate(zoneStateResult.Value);
        }
    }

    public async Task Handle(ZoneTrackChangedNotification notification, CancellationToken cancellationToken)
    {
        await _zoneUpdateService.BroadcastTrackChange(notification.ZoneIndex, notification.TrackInfo);

        var zoneStateResult = await _zoneManager.GetZoneStateAsync(notification.ZoneIndex);
        if (zoneStateResult.IsSuccess && zoneStateResult.Value != null)
        {
            await _zoneUpdateService.BroadcastZoneStateUpdate(zoneStateResult.Value);
        }
    }
}
