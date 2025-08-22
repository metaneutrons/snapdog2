namespace SnapDog2.Server.Features.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;

/// <summary>
/// Notification published when the list of available clients changes.
/// Contains the current list of configured client indices.
/// </summary>
[StatusId("CLIENTS_INFO")]
public record ClientsInfoChangedNotification : INotification
{
    /// <summary>
    /// Gets the list of available client indices (1-based).
    /// </summary>
    public required int[] ClientIndices { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the clients info was published.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
