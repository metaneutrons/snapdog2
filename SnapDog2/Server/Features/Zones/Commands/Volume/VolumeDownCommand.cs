namespace SnapDog2.Server.Features.Zones.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to decrease zone volume by a specified step. Decreases the volume for all clients in the zone relative to current level.
/// </summary>
public record VolumeDownCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the volume step to decrease (default 5). The volume will be decreased by this amount. Final volume will be clamped to minimum of 0.
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
