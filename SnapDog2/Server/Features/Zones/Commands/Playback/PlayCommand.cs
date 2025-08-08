namespace SnapDog2.Server.Features.Zones.Commands.Playback;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to start or resume playback in a zone. Supports both track index and media URL playback modes.
/// </summary>
public record PlayCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the optional track index to play (1-based). When specified, plays the track at this position in the current playlist.
    /// </summary>
    public int? TrackIndex { get; init; }

    /// <summary>
    /// Gets the optional media URL to play. When specified, plays the media from this URL directly. Takes precedence over TrackIndex if both are provided.
    /// </summary>
    public string? MediaUrl { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
