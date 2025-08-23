namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to increase a client's volume by a specified step.
/// </summary>
[CommandId("CLIENT_VOLUME_UP")]
[MqttTopic("snapdog/client/{clientIndex}/volume/up")]
public record ClientVolumeUpCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the client index (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the volume step to increase by (default: 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the command source.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
