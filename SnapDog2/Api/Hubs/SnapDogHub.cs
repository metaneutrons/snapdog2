namespace SnapDog2.Api.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public partial class SnapDogHub : Hub
{
    private readonly ILogger<SnapDogHub> _logger;

    public SnapDogHub(ILogger<SnapDogHub> logger)
    {
        _logger = logger;
    }

    #region Logging

    [LoggerMessage(EventId = 113350, Level = LogLevel.Information, Message = "SignalR client connected: {ConnectionId}"
)]
    private partial void LogClientConnected(string connectionId);

    [LoggerMessage(EventId = 113351, Level = LogLevel.Information, Message = "SignalR client disconnected: {ConnectionId}, Exception: {Exception}"
)]
    private partial void LogClientDisconnected(string connectionId, string? exception);

    [LoggerMessage(EventId = 113352, Level = LogLevel.Debug, Message = "Client {ConnectionId} joined zone {ZoneIndex}"
)]
    private partial void LogClientJoinedZone(string connectionId, int zoneIndex);

    [LoggerMessage(EventId = 113353, Level = LogLevel.Debug, Message = "Client {ConnectionId} left zone {ZoneIndex}"
)]
    private partial void LogClientLeftZone(string connectionId, int zoneIndex);

    [LoggerMessage(EventId = 113354, Level = LogLevel.Debug, Message = "Client {ConnectionId} joined client {ClientIndex}"
)]
    private partial void LogClientJoinedClient(string connectionId, int clientIndex);

    [LoggerMessage(EventId = 113355, Level = LogLevel.Debug, Message = "Client {ConnectionId} left client {ClientIndex}"
)]
    private partial void LogClientLeftClient(string connectionId, int clientIndex);

    [LoggerMessage(EventId = 113356, Level = LogLevel.Information, Message = "Client {ConnectionId} joined system group"
)]
    private partial void LogClientJoinedSystem(string connectionId);

    [LoggerMessage(EventId = 113357, Level = LogLevel.Information, Message = "Client {ConnectionId} left system group"
)]
    private partial void LogClientLeftSystem(string connectionId);

    #endregion

    public override async Task OnConnectedAsync()
    {
        this.LogClientConnected(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        this.LogClientDisconnected(Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinZone(int zoneIndex)
    {
        this.LogClientJoinedZone(Context.ConnectionId, zoneIndex);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZone(int zoneIndex)
    {
        this.LogClientLeftZone(Context.ConnectionId, zoneIndex);
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClient(int clientIndex)
    {
        this.LogClientJoinedClient(Context.ConnectionId, clientIndex);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClient(int clientIndex)
    {
        this.LogClientLeftClient(Context.ConnectionId, clientIndex);
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystem()
    {
        this.LogClientJoinedSystem(Context.ConnectionId);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "system");
    }

    public async Task LeaveSystem()
    {
        this.LogClientLeftSystem(Context.ConnectionId);
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "system");
    }
}
