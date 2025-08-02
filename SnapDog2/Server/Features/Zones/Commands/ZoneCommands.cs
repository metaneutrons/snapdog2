namespace SnapDog2.Server.Features.Zones.Commands;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

// Playback Control Commands

/// <summary>
/// Command to start or resume playback in a zone.
/// </summary>
public record PlayCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the optional track index to play (1-based).
    /// </summary>
    public int? TrackIndex { get; init; }

    /// <summary>
    /// Gets the optional media URL to play.
    /// </summary>
    public string? MediaUrl { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to pause playback in a zone.
/// </summary>
public record PauseCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to stop playback in a zone.
/// </summary>
public record StopCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

// Volume Control Commands

/// <summary>
/// Command to set the volume for a specific zone.
/// </summary>
public record SetZoneVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the desired volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to increase zone volume.
/// </summary>
public record VolumeUpCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the volume step to increase (default 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to decrease zone volume.
/// </summary>
public record VolumeDownCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the volume step to decrease (default 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set zone mute state.
/// </summary>
public record SetZoneMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to mute (true) or unmute (false) the zone.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle zone mute state.
/// </summary>
public record ToggleZoneMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

// Track Management Commands

/// <summary>
/// Command to set a specific track in a zone.
/// </summary>
public record SetTrackCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the track index to play (1-based).
    /// </summary>
    public required int TrackIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the next track in a zone.
/// </summary>
public record NextTrackCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the previous track in a zone.
/// </summary>
public record PreviousTrackCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

// Playlist Management Commands

/// <summary>
/// Command to set a specific playlist in a zone.
/// </summary>
public record SetPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the playlist index (1-based, where 1 = Radio).
    /// </summary>
    public int? PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the playlist ID (alternative to index).
    /// </summary>
    public string? PlaylistId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the next playlist in a zone.
/// </summary>
public record NextPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to play the previous playlist in a zone.
/// </summary>
public record PreviousPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

// Track Repeat Commands

/// <summary>
/// Command to set track repeat mode.
/// </summary>
public record SetTrackRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to enable track repeat.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle track repeat mode.
/// </summary>
public record ToggleTrackRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

// Playlist Shuffle and Repeat Commands

/// <summary>
/// Command to set playlist shuffle mode.
/// </summary>
public record SetPlaylistShuffleCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to enable playlist shuffle.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle playlist shuffle mode.
/// </summary>
public record TogglePlaylistShuffleCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set playlist repeat mode.
/// </summary>
public record SetPlaylistRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets whether to enable playlist repeat.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle playlist repeat mode.
/// </summary>
public record TogglePlaylistRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
