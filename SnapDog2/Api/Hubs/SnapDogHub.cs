namespace SnapDog2.Api.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class SnapDogHub : Hub
{
    public async Task JoinZone(int zoneIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZone(int zoneIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClient(int clientIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClient(int clientIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystem()
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "system");
    }

    public async Task LeaveSystem()
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "system");
    }
}
