namespace SnapDog2.Server.Features.Shared.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Shared.Notifications;

/// <summary>
/// Handles client state change notifications to log and process status updates.
/// </summary>
public partial class ClientStateNotificationHandler
    : INotificationHandler<ClientVolumeChangedNotification>,
        INotificationHandler<ClientMuteChangedNotification>,
        INotificationHandler<ClientLatencyChangedNotification>,
        INotificationHandler<ClientZoneAssignmentChangedNotification>,
        INotificationHandler<ClientConnectionChangedNotification>,
        INotificationHandler<ClientStateChangedNotification>
{
    private readonly ILogger<ClientStateNotificationHandler> _logger;

    [LoggerMessage(6101, LogLevel.Information, "Client {ClientId} volume changed to {Volume}")]
    private partial void LogVolumeChange(int clientId, int volume);

    [LoggerMessage(6102, LogLevel.Information, "Client {ClientId} mute changed to {IsMuted}")]
    private partial void LogMuteChange(int clientId, bool isMuted);

    [LoggerMessage(6103, LogLevel.Information, "Client {ClientId} latency changed to {LatencyMs}ms")]
    private partial void LogLatencyChange(int clientId, int latencyMs);

    [LoggerMessage(
        6104,
        LogLevel.Information,
        "Client {ClientId} zone assignment changed from {PreviousZoneId} to {NewZoneId}"
    )]
    private partial void LogZoneAssignmentChange(int clientId, int? previousZoneId, int? newZoneId);

    [LoggerMessage(6105, LogLevel.Information, "Client {ClientId} connection changed to {IsConnected}")]
    private partial void LogConnectionChange(int clientId, bool isConnected);

    [LoggerMessage(6106, LogLevel.Information, "Client {ClientId} complete state changed")]
    private partial void LogStateChange(int clientId);

    public ClientStateNotificationHandler(ILogger<ClientStateNotificationHandler> logger)
    {
        this._logger = logger;
    }

    public async Task Handle(ClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVolumeChange(notification.ClientId, notification.Volume);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ClientMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogMuteChange(notification.ClientId, notification.IsMuted);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ClientLatencyChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogLatencyChange(notification.ClientId, notification.LatencyMs);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ClientZoneAssignmentChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneAssignmentChange(notification.ClientId, notification.PreviousZoneId, notification.ZoneId);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ClientConnectionChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionChange(notification.ClientId, notification.IsConnected);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }

    public async Task Handle(ClientStateChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogStateChange(notification.ClientId);

        // TODO: Publish to external systems (MQTT, KNX) when infrastructure adapters are implemented
        await Task.CompletedTask;
    }
}
