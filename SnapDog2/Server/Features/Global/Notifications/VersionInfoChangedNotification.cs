namespace SnapDog2.Server.Features.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when version information is updated or requested.
/// </summary>
public record VersionInfoChangedNotification : INotification
{
    /// <summary>
    /// Gets the version details.
    /// </summary>
    public required VersionDetails VersionInfo { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
