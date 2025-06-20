using MediatR;

namespace SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Command to set the mute state for a specific Snapcast client.
/// </summary>
/// <param name="ClientId">Unique identifier of the client</param>
/// <param name="Muted">True to mute, false to unmute</param>
public record SetClientMuteCommand(string ClientId, bool Muted) : IRequest<bool>;
