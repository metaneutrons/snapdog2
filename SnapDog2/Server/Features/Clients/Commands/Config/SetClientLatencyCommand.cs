namespace SnapDog2.Server.Features.Clients.Commands.Config;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set client latency. Adjusts the audio latency compensation for an individual Snapcast client. Used to synchronize audio playback across multiple clients.
/// </summary>
public record SetClientLatencyCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the latency in milliseconds. Positive values add delay, negative values reduce delay. Typical range is -500ms to +500ms.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
