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

    [LoggerMessage(6101, LogLevel.Information, "Client {ClientIndex} volume changed to {Volume}")]
    private partial void LogVolumeChange(int clientIndex, int volume);

    [LoggerMessage(6102, LogLevel.Information, "Client {ClientIndex} mute changed to {IsMuted}")]
    private partial void LogMuteChange(int clientIndex, bool isMuted);

    [LoggerMessage(6103, LogLevel.Information, "Client {ClientIndex} latency changed to {LatencyMs}ms")]
    private partial void LogLatencyChange(int clientIndex, int latencyMs);

    [LoggerMessage(
        6104,
        LogLevel.Information,
        "Client {ClientIndex} zone assignment changed from {PreviousZoneIndex} to {NewZoneIndex}"
    )]
    private partial void LogZoneAssignmentChange(int clientIndex, int? previousZoneIndex, int? newZoneIndex);

    [LoggerMessage(6105, LogLevel.Information, "Client {ClientIndex} connection changed to {IsConnected}")]
    private partial void LogConnectionChange(int clientIndex, bool isConnected);

    [LoggerMessage(6106, LogLevel.Information, "Client {ClientIndex} complete state changed")]
    private partial void LogStateChange(int clientIndex);

    public async Task Handle(ClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVolumeChange(notification.ClientIndex, notification.Volume);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientVolumeChangedNotification>(),
            notification.ClientIndex.ToString(),
            notification.Volume,
            cancellationToken
        );
    }

    public async Task Handle(ClientMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogMuteChange(notification.ClientIndex, notification.IsMuted);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientMuteChangedNotification>(),
            notification.ClientIndex.ToString(),
            notification.IsMuted,
            cancellationToken
        );
    }

    public async Task Handle(ClientLatencyChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogLatencyChange(notification.ClientIndex, notification.LatencyMs);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientLatencyChangedNotification>(),
            notification.ClientIndex.ToString(),
            notification.LatencyMs,
            cancellationToken
        );
    }

    public async Task Handle(ClientZoneAssignmentChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogZoneAssignmentChange(notification.ClientIndex, notification.PreviousZoneIndex, notification.ZoneIndex);

        // Perform actual Snapcast group assignment only if ZoneIndex is not null
        if (notification.ZoneIndex.HasValue)
        {
            try
            {
                var clientManager = this._serviceProvider.GetRequiredService<IClientManager>();
                var result = await clientManager.AssignClientToZoneAsync(
                    notification.ClientIndex,
                    notification.ZoneIndex.Value
                );

                if (result.IsFailure)
                {
                    this._logger.LogWarning(
                        "Failed to assign client {ClientIndex} to zone {ZoneIndex}: {Error}",
                        notification.ClientIndex,
                        notification.ZoneIndex.Value,
                        result.ErrorMessage
                    );
                }
                else
                {
                    this._logger.LogInformation(
                        "Successfully assigned client {ClientIndex} to zone {ZoneIndex}",
                        notification.ClientIndex,
                        notification.ZoneIndex.Value
                    );
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(
                    ex,
                    "Error during client {ClientIndex} zone assignment to zone {ZoneIndex}",
                    notification.ClientIndex,
                    notification.ZoneIndex.Value
                );
            }
        }
        else
        {
            this._logger.LogInformation(
                "Client {ClientIndex} unassigned from zone (ZoneIndex is null)",
                notification.ClientIndex
            );
        }

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientZoneAssignmentChangedNotification>(),
            notification.ClientIndex.ToString(),
            new { PreviousZoneIndex = notification.PreviousZoneIndex, ZoneIndex = notification.ZoneIndex },
            cancellationToken
        );
    }

    public async Task Handle(ClientConnectionChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionChange(notification.ClientIndex, notification.IsConnected);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientConnectionChangedNotification>(),
            notification.ClientIndex.ToString(),
            notification.IsConnected,
            cancellationToken
        );
    }

    public async Task Handle(ClientStateChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogStateChange(notification.ClientIndex);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ClientStateChangedNotification>(),
            notification.ClientIndex.ToString(),
            notification,
            cancellationToken
        );
    }

    /// <summary>
    /// Publishes client events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    private async Task PublishToExternalSystemsAsync<T>(
        string eventType,
        string clientIndex,
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
                await mqttService.PublishClientStatusAsync(clientIndex, eventType, payload, cancellationToken);
            }

            // Publish to KNX if enabled
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                await knxService.PublishClientStatusAsync(clientIndex, eventType, payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishClientEventToExternalSystems(ex, eventType, clientIndex);
        }
    }
}
