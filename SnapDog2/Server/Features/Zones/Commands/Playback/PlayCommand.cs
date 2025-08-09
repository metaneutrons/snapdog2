namespace SnapDog2.Server.Features.Zones.Commands.Playback;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to start or resume playback in a zone. Supports both track index and media URL playback modes.
/// </summary>
[CommandId("ZONE_PLAY", "ZP-002")]
public record PlayCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the optional track index to play (1-based). If null, resumes current track.
    /// </summary>
    public int? TrackIndex { get; init; }

    /// <summary>
    /// Gets the optional media URL to play. If provided, takes precedence over TrackIndex.
    /// </summary>
    public string? MediaUrl { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
