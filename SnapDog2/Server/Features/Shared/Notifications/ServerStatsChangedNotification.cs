namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when server statistics are updated.
/// </summary>
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
