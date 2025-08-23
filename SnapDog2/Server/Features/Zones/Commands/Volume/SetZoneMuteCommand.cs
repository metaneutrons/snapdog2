namespace SnapDog2.Server.Features.Zones.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set zone mute state. Mutes or unmutes all clients in the zone.
/// </summary>
[CommandId("MUTE")]
[MqttTopic("snapdog/zone/{zoneIndex}/mute/set")]
public record SetZoneMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets whether the zone should be muted.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
