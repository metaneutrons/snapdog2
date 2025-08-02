namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a zone's playback state changes.
/// </summary>
public record ZonePlaybackStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the new playback state.
    /// </summary>
    public required PlaybackStatus PlaybackState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's volume changes.
/// </summary>
public record ZoneVolumeChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

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
/// Notification published when a zone's mute state changes.
/// </summary>
public record ZoneMuteChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether the zone is muted.
    /// </summary>
    public required bool IsMuted { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the mute state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's current track changes.
/// </summary>
public record ZoneTrackChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the new track information.
    /// </summary>
    public required TrackInfo TrackInfo { get; init; }

    /// <summary>
    /// Gets the new track index (1-based).
    /// </summary>
    public required int TrackIndex { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the track changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's current playlist changes.
/// </summary>
public record ZonePlaylistChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the new playlist information.
    /// </summary>
    public required PlaylistInfo PlaylistInfo { get; init; }

    /// <summary>
    /// Gets the new playlist index (1-based).
    /// </summary>
    public required int PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the playlist changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's repeat mode changes.
/// </summary>
public record ZoneRepeatModeChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether track repeat is enabled.
    /// </summary>
    public required bool TrackRepeatEnabled { get; init; }

    /// <summary>
    /// Gets whether playlist repeat is enabled.
    /// </summary>
    public required bool PlaylistRepeatEnabled { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the repeat mode changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's shuffle mode changes.
/// </summary>
public record ZoneShuffleModeChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether shuffle is enabled.
    /// </summary>
    public required bool ShuffleEnabled { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the shuffle mode changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's complete state changes.
/// </summary>
public record ZoneStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the complete zone state.
    /// </summary>
    public required ZoneState ZoneState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
