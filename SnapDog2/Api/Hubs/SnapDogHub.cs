namespace SnapDog2.Api.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class SnapDogHub : Hub
{
    private readonly ILogger<SnapDogHub> _logger;

    public SnapDogHub(ILogger<SnapDogHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🔗 SignalR client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("❌ SignalR client disconnected: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinZone(int zoneIndex)
    {
        _logger.LogDebug("📡 Client {ConnectionId} joined zone {ZoneIndex}", Context.ConnectionId, zoneIndex);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZone(int zoneIndex)
    {
        _logger.LogDebug("📤 Client {ConnectionId} left zone {ZoneIndex}", Context.ConnectionId, zoneIndex);
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClient(int clientIndex)
    {
        _logger.LogDebug("📡 Client {ConnectionId} joined client {ClientIndex}", Context.ConnectionId, clientIndex);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClient(int clientIndex)
    {
        _logger.LogDebug("📤 Client {ConnectionId} left client {ClientIndex}", Context.ConnectionId, clientIndex);
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystem()
    {
        _logger.LogInformation("📡 Client {ConnectionId} joined system group", Context.ConnectionId);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "system");
    }

    public async Task LeaveSystem()
    {
        _logger.LogInformation("📤 Client {ConnectionId} left system group", Context.ConnectionId);
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "system");
    }
}
