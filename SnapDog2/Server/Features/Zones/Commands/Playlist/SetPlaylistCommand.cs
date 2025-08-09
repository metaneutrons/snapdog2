namespace SnapDog2.Server.Features.Zones.Commands.Playlist;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the current playlist in a zone. Changes to a specific playlist by index.
/// </summary>
[CommandId("SET_PLAYLIST", "ZPL-002")]
public record SetPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the playlist index to set (1-based). Either this or PlaylistId must be specified.
    /// </summary>
    public int? PlaylistIndex { get; init; }

    /// <summary>
    /// Gets the playlist ID to set. Either this or PlaylistIndex must be specified.
    /// </summary>
    public string? PlaylistId { get; init; }

    /// <summary>
    /// Gets the optional track index to start from (1-based). If null, starts from beginning.
    /// </summary>
    public int? StartTrackIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
