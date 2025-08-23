namespace SnapDog2.Server.Features.Clients.Commands.Config;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to assign a client to a specific zone. Moves a Snapcast client to a different audio zone.
/// </summary>
[CommandId("CLIENT_ZONE")]
[MqttTopic("snapdog/client/{clientIndex}/zone/set")]
public record AssignClientToZoneCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the client to assign (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

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
