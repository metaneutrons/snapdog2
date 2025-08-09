namespace SnapDog2.Server.Features.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
using SnapDog2.Server.Features.Clients.Notifications;

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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClientStateNotificationHandler> _logger;

    public ClientStateNotificationHandler(
        IServiceProvider serviceProvider,
        ILogger<ClientStateNotificationHandler> logger
    )
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

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

    public async Task Handle(ClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVolumeChange(notification.ClientId, notification.Volume);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientVolumeChangedNotification>(),
            notification.ClientId.ToString(),
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ClientMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogMuteChange(notification.ClientId, notification.IsMuted);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientMuteChangedNotification>(),
            notification.ClientId.ToString(),
            notification.IsMuted,
            cancellationToken
        );
    }

    public async Task Handle(ClientLatencyChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogLatencyChange(notification.ClientId, notification.LatencyMs);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientLatencyChangedNotification>(),
            notification.ClientId.ToString(),
            notification.LatencyMs,
            cancellationToken
        );
    }

    public async Task Handle(ClientZoneAssignmentChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneAssignmentChange(notification.ClientId, notification.PreviousZoneId, notification.ZoneId);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientZoneAssignmentChangedNotification>(),
            notification.ClientId.ToString(),
            new { PreviousZoneId = notification.PreviousZoneId, ZoneId = notification.ZoneId },
            cancellationToken
        );
    }

    public async Task Handle(ClientConnectionChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionChange(notification.ClientId, notification.IsConnected);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientConnectionChangedNotification>(),
            notification.ClientId.ToString(),
            notification.IsConnected,
            cancellationToken
        );
    }

    public async Task Handle(ClientStateChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogStateChange(notification.ClientId);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientStateChangedNotification>(),
            notification.ClientId.ToString(),
            notification,
            cancellationToken
        );
    }

    /// <summary>
    /// Publishes client events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    private async Task PublishToExternalSystemsAsync<T>(
        string eventType,
        string clientId,
        T payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Publish to MQTT if enabled
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                await mqttService.PublishClientStatusAsync(clientId, eventType, payload, cancellationToken);
            }

            // Publish to KNX if enabled
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                await knxService.PublishClientStatusAsync(clientId, eventType, payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(
                ex,
                "Failed to publish {EventType} for client {ClientId} to external systems",
                eventType,
                clientId
            );
        }
    }
}
