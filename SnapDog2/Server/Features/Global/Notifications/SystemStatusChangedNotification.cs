namespace SnapDog2.Server.Features.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when the system status changes.
/// </summary>
[StatusId("SYSTEM_STATUS", "SS-001")]
public record SystemStatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the new system status.
    /// </summary>
    public required SystemStatus Status { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
