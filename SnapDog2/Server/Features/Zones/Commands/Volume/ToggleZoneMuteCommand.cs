namespace SnapDog2.Server.Features.Zones.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to toggle zone mute state. Toggles the mute state of all clients in the zone.
/// </summary>
[CommandId("MUTE_TOGGLE")]
[MqttTopic("snapdog/zone/{zoneIndex}/mute/toggle")]
public record ToggleZoneMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
