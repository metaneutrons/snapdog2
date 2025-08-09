namespace SnapDog2.Server.Features.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when server statistics are updated.
/// </summary>
[StatusId("SERVER_STATS", "STS-001")]
public record ServerStatsChangedNotification : INotification
{
    /// <summary>
    /// Gets the server statistics.
    /// </summary>
    public required ServerStats Stats { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
