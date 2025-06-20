using MediatR;

namespace SnapDog2.Server.Features.Snapcast.Queries;

/// <summary>
/// Query to retrieve the current status of the Snapcast server.
/// </summary>
public record GetSnapcastServerStatusQuery : IRequest<string>;
