namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set client mute state. Mutes or unmutes an individual Snapcast client.
/// </summary>
public record SetClientMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets whether to mute (true) or unmute (false) the client.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
