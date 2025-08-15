namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator.Notifications;

/// <summary>
/// Generic notification published when any tracked status changes within the system.
/// This is used by infrastructure adapters (MQTT, KNX) for protocol-agnostic status updates.
/// </summary>
public record StatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the type of status that changed (matches Command Framework Status IDs).
    /// </summary>
    public required string StatusType { get; init; }

    /// <summary>
    /// Gets the identifier for the entity whose status changed.
    /// </summary>
    public required string TargetId { get; init; }

    /// <summary>
    /// Gets the new value of the status.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
