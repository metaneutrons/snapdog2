namespace SnapDog2.Server.Features.Zones.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to increase zone volume by a specified step. Increases the volume for all clients in the zone relative to current level.
/// </summary>
public record VolumeUpCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the volume step to increase (default 5). The volume will be increased by this amount. Final volume will be clamped to maximum of 100.
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
