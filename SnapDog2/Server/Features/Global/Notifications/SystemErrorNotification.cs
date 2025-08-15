namespace SnapDog2.Server.Features.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a system error occurs.
/// </summary>
[StatusId("SYSTEM_ERROR")]
public record SystemErrorNotification : INotification
{
    /// <summary>
    /// Gets the error details.
    /// </summary>
    public required ErrorDetails Error { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the error occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
