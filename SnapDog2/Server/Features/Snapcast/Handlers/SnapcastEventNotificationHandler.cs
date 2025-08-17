namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Features.Snapcast.Notifications;

/// <summary>
/// Handles Snapcast event notifications and processes them for the application.
/// This demonstrates how Snapcast events flow through the mediator pattern.
/// </summary>
public partial class SnapcastEventNotificationHandler(ILogger<SnapcastEventNotificationHandler> logger)
    : INotificationHandler<SnapcastClientConnectedNotification>,
        INotificationHandler<SnapcastClientDisconnectedNotification>,
        INotificationHandler<SnapcastClientVolumeChangedNotification>,
        INotificationHandler<SnapcastGroupMuteChangedNotification>,
        INotificationHandler<SnapcastConnectionEstablishedNotification>,
        INotificationHandler<SnapcastConnectionLostNotification>
{
    private readonly ILogger<SnapcastEventNotificationHandler> _logger = logger;

    #region Logging

    [LoggerMessage(4001, LogLevel.Information, "Snapcast client connected: {ClientIndex} ({ClientName})")]
    private partial void LogClientConnected(string clientIndex, string clientName);

    [LoggerMessage(4002, LogLevel.Information, "Snapcast client disconnected: {ClientIndex} ({ClientName})")]
    private partial void LogClientDisconnected(string clientIndex, string clientName);

    [LoggerMessage(
        4003,
        LogLevel.Information,
        "Snapcast client volume changed: {ClientIndex} -> {Volume}% (Muted: {Muted})"
    )]
    private partial void LogClientVolumeChanged(string clientIndex, int volume, bool muted);

    [LoggerMessage(4004, LogLevel.Information, "Snapcast group mute changed: {GroupId} -> Muted: {Muted}")]
    private partial void LogGroupMuteChanged(string groupId, bool muted);

    [LoggerMessage(4005, LogLevel.Information, "Snapcast connection established")]
    private partial void LogConnectionEstablished();

    [LoggerMessage(4006, LogLevel.Warning, "Snapcast connection lost: {Reason}")]
    private partial void LogConnectionLost(string reason);

    #endregion

    #region Client Events

    public Task Handle(SnapcastClientConnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientConnected(notification.Client.Id, notification.Client.Config.Name);

        // Here you could:
        // - Update internal client state
        // - Publish MQTT status updates
        // - Send KNX notifications
        // - Update metrics
        // - Trigger other business logic

        return Task.CompletedTask;
    }

    public Task Handle(SnapcastClientDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientDisconnected(notification.Client.Id, notification.Client.Config.Name);

        // Here you could:
        // - Update internal client state
        // - Publish MQTT status updates
        // - Send KNX notifications
        // - Update metrics
        // - Trigger reconnection logic

        return Task.CompletedTask;
    }

    public Task Handle(SnapcastClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogClientVolumeChanged(notification.ClientIndex, notification.Volume.Percent, notification.Volume.Muted);

        // Here you could:
        // - Update internal client state
        // - Publish MQTT volume updates
        // - Send KNX volume status
        // - Update metrics
        // - Trigger volume-based automation

        return Task.CompletedTask;
    }

    #endregion

    #region Group Events

    public Task Handle(SnapcastGroupMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogGroupMuteChanged(notification.GroupId, notification.Muted);

        // Here you could:
        // - Update internal group state
        // - Publish MQTT group status updates
        // - Send KNX group notifications
        // - Update metrics
        // - Trigger group-based automation

        return Task.CompletedTask;
    }

    #endregion

    #region Connection Events

    public Task Handle(SnapcastConnectionEstablishedNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionEstablished();

        // Here you could:
        // - Initialize system state
        // - Publish connection status to MQTT
        // - Update health check status
        // - Trigger initialization routines

        return Task.CompletedTask;
    }

    public Task Handle(SnapcastConnectionLostNotification notification, CancellationToken cancellationToken)
    {
        this.LogConnectionLost(notification.Reason);

        // Here you could:
        // - Update connection status
        // - Publish disconnection status to MQTT
        // - Update health check status
        // - Trigger reconnection logic
        // - Alert administrators

        return Task.CompletedTask;
    }

    #endregion
}
