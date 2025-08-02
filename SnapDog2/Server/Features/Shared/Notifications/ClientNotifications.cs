namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a client's volume changes.
/// </summary>
public record ClientVolumeChangedNotification : INotification
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the new volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the volume changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's mute state changes.
/// </summary>
public record ClientMuteChangedNotification : INotification
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets whether the client is muted.
    /// </summary>
    public required bool IsMuted { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the mute state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's latency changes.
/// </summary>
public record ClientLatencyChangedNotification : INotification
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the new latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the latency changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client is assigned to a different zone.
/// </summary>
public record ClientZoneAssignmentChangedNotification : INotification
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the new zone ID (null if unassigned).
    /// </summary>
    public int? ZoneId { get; init; }

    /// <summary>
    /// Gets the previous zone ID (null if was unassigned).
    /// </summary>
    public int? PreviousZoneId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the assignment changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's connection status changes.
/// </summary>
public record ClientConnectionChangedNotification : INotification
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the connection status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's complete state changes.
/// </summary>
public record ClientStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the complete client state.
    /// </summary>
    public required ClientState ClientState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
