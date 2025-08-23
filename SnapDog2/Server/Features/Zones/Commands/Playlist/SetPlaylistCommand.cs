namespace SnapDog2.Server.Features.Zones.Commands.Playlist;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the current playlist in a zone. Changes to a specific playlist by index.
/// </summary>
[CommandId("PLAYLIST")]
[MqttTopic("snapdog/zone/{zoneIndex}/playlist/set")]
public record SetPlaylistCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the playlist index to set (1-based).
    /// </summary>
    public required int PlaylistIndex { get; init; }

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
