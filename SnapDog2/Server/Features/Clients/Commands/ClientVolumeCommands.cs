namespace SnapDog2.Server.Features.Clients.Commands;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the volume for a specific client.
/// </summary>
public record SetClientVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the desired volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set client mute state.
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
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle client mute state.
/// </summary>
public record ToggleClientMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
