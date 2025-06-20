using MediatR;

namespace SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Command to set the volume level for a specific Snapcast client.
/// </summary>
/// <param name="ClientId">Unique identifier of the client</param>
/// <param name="Volume">Volume level (0-100)</param>
public record SetClientVolumeCommand(string ClientId, int Volume) : IRequest<bool>;
