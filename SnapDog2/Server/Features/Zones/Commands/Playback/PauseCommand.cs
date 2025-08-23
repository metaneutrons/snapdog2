namespace SnapDog2.Server.Features.Zones.Commands.Playback;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to pause playback in a zone. Pauses the currently playing media.
/// </summary>
[CommandId("PAUSE")]
[MqttTopic("snapdog/zone/{zoneIndex}/pause")]
public record PauseCommand : ICommand<Result>
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
