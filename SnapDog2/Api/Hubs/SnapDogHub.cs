namespace SnapDog2.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

public class SnapDogHub : Hub
{
    public async Task JoinZoneGroup(int zoneIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZoneGroup(int zoneIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClientGroup(int clientIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClientGroup(int clientIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystemGroup()
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "system");
    }

    public async Task LeaveSystemGroup()
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "system");
    }
}
