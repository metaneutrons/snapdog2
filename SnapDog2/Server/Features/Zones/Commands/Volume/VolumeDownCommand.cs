namespace SnapDog2.Server.Features.Zones.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to decrease zone volume by a specified step. Decreases the volume for all clients in the zone.
/// </summary>
[CommandId("VOLUME_DOWN")]
[MqttTopic("snapdog/zone/{zoneIndex}/volume/down")]
public record VolumeDownCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the volume decrease step (1-50). Default is typically 5.
    /// </summary>
    public required int Step { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
