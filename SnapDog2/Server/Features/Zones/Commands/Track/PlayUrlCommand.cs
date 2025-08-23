namespace SnapDog2.Server.Features.Zones.Commands.Track;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to play a direct URL stream and start playback immediately.
/// </summary>
[CommandId("TRACK_PLAY_URL")]
[MqttTopic("snapdog/zone/{zoneIndex}/play/url")]
public record PlayUrlCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the URL to play.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
