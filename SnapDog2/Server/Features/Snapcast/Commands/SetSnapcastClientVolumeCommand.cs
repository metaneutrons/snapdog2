namespace SnapDog2.Server.Features.Snapcast.Commands;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the volume of a Snapcast client.
/// </summary>
public record SetSnapcastClientVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// The Snapcast client ID.
    /// </summary>
    public required string ClientIndex { get; init; }

    /// <summary>
    /// The volume percentage (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// The source of the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Api;
}
