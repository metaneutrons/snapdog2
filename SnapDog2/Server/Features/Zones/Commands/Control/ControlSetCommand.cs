namespace SnapDog2.Server.Features.Zones.Commands.Control;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Unified control command that accepts vocabulary from the blueprint specification.
/// This command provides a single endpoint for various control operations using string commands.
/// </summary>
[CommandId("CONTROL")]
[MqttTopic("snapdog/zone/{zoneIndex}/control/set")]
public record ControlSetCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the control command string (e.g., "play", "pause", "next", "shuffle_on").
    /// Accepts vocabulary from blueprint section 14.3.2.3.
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
