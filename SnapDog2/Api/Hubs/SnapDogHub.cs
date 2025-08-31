using Microsoft.AspNetCore.SignalR;

namespace SnapDog2.Api.Hubs;

public class SnapDogHub : Hub
{
    public async Task JoinZoneGroup(int zoneIndex)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZoneGroup(int zoneIndex)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClientGroup(int clientIndex)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClientGroup(int clientIndex)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystemGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "system");
    }

    public async Task LeaveSystemGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "system");
    }
}
