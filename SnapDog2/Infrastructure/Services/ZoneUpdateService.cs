using Microsoft.AspNetCore.SignalR;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Hubs;

namespace SnapDog2.Infrastructure.Services;

public interface IZoneUpdateService
{
    Task BroadcastZoneStateUpdate(ZoneState zoneState);
    Task BroadcastTrackProgress(int zoneIndex, long position, float progress);
    Task BroadcastTrackChange(int zoneIndex, TrackInfo trackInfo);
}

public class ZoneUpdateService(IHubContext<ZoneHub> hubContext) : IZoneUpdateService
{
    private readonly IHubContext<ZoneHub> _hubContext = hubContext;

    public async Task BroadcastZoneStateUpdate(ZoneState zoneState)
    {
        await _hubContext.Clients.Groups($"Zone_{zoneState.Name}", "AllZones")
            .SendAsync("ZoneStateUpdated", zoneState);
    }

    public async Task BroadcastTrackProgress(int zoneIndex, long position, float progress)
    {
        await _hubContext.Clients.Groups($"Zone_{zoneIndex}", "AllZones")
            .SendAsync("TrackProgressUpdated", zoneIndex, position, progress);
    }

    public async Task BroadcastTrackChange(int zoneIndex, TrackInfo trackInfo)
    {
        await _hubContext.Clients.Groups($"Zone_{zoneIndex}", "AllZones")
            .SendAsync("TrackChanged", zoneIndex, trackInfo);
    }
}
