namespace SnapDog2.Server.Features.Zones.Commands.Playlist;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

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
