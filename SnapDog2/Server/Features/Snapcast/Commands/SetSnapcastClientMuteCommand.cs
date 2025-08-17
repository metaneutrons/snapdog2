namespace SnapDog2.Server.Features.Snapcast.Commands;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the mute state of a Snapcast client.
/// </summary>
public record SetSnapcastClientMuteCommand : ICommand<Result>
{
    /// <summary>
    /// The Snapcast client ID.
    /// </summary>
    public required string ClientIndex { get; init; }

    /// <summary>
    /// Whether the client should be muted.
    /// </summary>
    public required bool Muted { get; init; }

    /// <summary>
    /// The source of the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Api;
}
