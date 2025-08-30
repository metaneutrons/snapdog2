using Microsoft.AspNetCore.SignalR;
using SnapDog2.Core.Models;

namespace SnapDog2.Infrastructure.Hubs;

public class ZoneHub : Hub
{
    public async Task JoinZoneGroup(int zoneIndex)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Zone_{zoneIndex}");
    }

    public async Task LeaveZoneGroup(int zoneIndex)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Zone_{zoneIndex}");
    }

    public async Task JoinAllZones()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllZones");
    }
}
