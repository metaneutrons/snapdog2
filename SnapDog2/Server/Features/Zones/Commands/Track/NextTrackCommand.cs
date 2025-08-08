namespace SnapDog2.Server.Features.Zones.Commands.Track;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

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
