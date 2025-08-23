namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to toggle client mute state. Toggles the mute state of an individual Snapcast client.
/// </summary>
[CommandId("CLIENT_MUTE_TOGGLE")]
[MqttTopic("snapdog/client/{clientIndex}/mute/toggle")]
public record ToggleClientMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target client (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
