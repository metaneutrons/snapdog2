namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to decrease a client's volume by a specified step.
/// </summary>
[CommandId("CLIENT_VOLUME_DOWN")]
public record ClientVolumeDownCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the client index (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the volume step to decrease by (default: 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the command source.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
