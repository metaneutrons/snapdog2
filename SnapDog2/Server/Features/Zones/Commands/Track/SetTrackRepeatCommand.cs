namespace SnapDog2.Server.Features.Zones.Commands.Track;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set track repeat mode in a zone.
/// </summary>
[CommandId("TRACK_REPEAT")]
[MqttTopic("snapdog/zone/{zoneIndex}/repeat/track/set")]
public record SetTrackRepeatCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets whether track repeat should be enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
