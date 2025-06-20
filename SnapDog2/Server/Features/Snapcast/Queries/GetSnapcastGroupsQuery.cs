using MediatR;

namespace SnapDog2.Server.Features.Snapcast.Queries;

/// <summary>
/// Query to retrieve all groups configured on the Snapcast server.
/// </summary>
public record GetSnapcastGroupsQuery : IRequest<IEnumerable<string>>;
