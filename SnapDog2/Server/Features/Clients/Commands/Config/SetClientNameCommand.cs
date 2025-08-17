namespace SnapDog2.Server.Features.Clients.Commands.Config;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the display name of a Snapcast client.
/// </summary>
[CommandId("CLIENT_NAME")]
public record SetClientNameCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target client (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new name for the client.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
