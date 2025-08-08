namespace SnapDog2.Server.Features.Zones.Commands.Playlist;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

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
