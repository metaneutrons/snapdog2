namespace SnapDog2.Server.Features.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
using SnapDog2.Server.Features.Global.Notifications;

/// <summary>
/// Handles global system state change notifications to log and process status updates.
/// </summary>
public partial class GlobalStateNotificationHandler
    : INotificationHandler<SystemStatusChangedNotification>,
        INotificationHandler<VersionInfoChangedNotification>,
        INotificationHandler<ServerStatsChangedNotification>,
        INotificationHandler<SystemErrorNotification>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalStateNotificationHandler> _logger;

    public GlobalStateNotificationHandler(
        IServiceProvider serviceProvider,
        ILogger<GlobalStateNotificationHandler> logger
    )
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    [LoggerMessage(6201, LogLevel.Information, "System status changed to online: {IsOnline}")]
    private partial void LogSystemStatusChange(bool isOnline);

    [LoggerMessage(6202, LogLevel.Information, "Version info updated: {Version} (Build: {BuildDate})")]
    private partial void LogVersionInfoChange(string version, DateTime buildDate);

    [LoggerMessage(6203, LogLevel.Information, "Server stats updated - CPU: {CpuUsage}%, Memory: {MemoryUsage}MB")]
    private partial void LogServerStatsChange(double cpuUsage, double memoryUsage);

    [LoggerMessage(6204, LogLevel.Error, "System error occurred: {ErrorCode} - {Message}")]
    private partial void LogSystemError(string errorCode, string message);

    public async Task Handle(SystemStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogSystemStatusChange(notification.Status.IsOnline);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<SystemStatusChangedNotification>(),
            notification.Status,
            cancellationToken
        );
    }

    public async Task Handle(VersionInfoChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVersionInfoChange(
            notification.VersionInfo.Version,
            notification.VersionInfo.BuildDateUtc ?? DateTime.UtcNow
        );

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<VersionInfoChangedNotification>(),
            notification.VersionInfo,
            cancellationToken
        );
    }

    public async Task Handle(ServerStatsChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogServerStatsChange(notification.Stats.CpuUsagePercent, notification.Stats.MemoryUsageMb);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ServerStatsChangedNotification>(),
            notification.Stats,
            cancellationToken
        );
    }

    public async Task Handle(SystemErrorNotification notification, CancellationToken cancellationToken)
    {
        this.LogSystemError(notification.Error.ErrorCode, notification.Error.Message);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<SystemErrorNotification>(),
            notification.Error,
            cancellationToken
        );
    }

    /// <summary>
    /// Publishes global events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    private async Task PublishToExternalSystemsAsync<T>(
        string eventType,
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
                await mqttService.PublishGlobalStatusAsync(eventType, payload, cancellationToken);
            }

            // Publish to KNX if enabled
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                await knxService.PublishGlobalStatusAsync(eventType, payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishGlobalEventToExternalSystems(ex, eventType);
        }
    }
}
