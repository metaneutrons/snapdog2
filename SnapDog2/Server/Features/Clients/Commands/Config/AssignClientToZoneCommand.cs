namespace SnapDog2.Server.Features.Clients.Commands.Config;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to assign a client to a zone. Moves a Snapcast client from its current zone to a different zone. The client will start playing the audio stream of the new zone.
/// </summary>
public record AssignClientToZoneCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the ID of the zone to assign the client to (1-based). The client will be moved to this zone and start playing its audio stream.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
