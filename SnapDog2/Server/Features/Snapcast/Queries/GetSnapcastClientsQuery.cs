using MediatR;

namespace SnapDog2.Server.Features.Snapcast.Queries;

/// <summary>
/// Query to retrieve all clients connected to the Snapcast server.
/// </summary>
public record GetSnapcastClientsQuery : IRequest<IEnumerable<string>>;
