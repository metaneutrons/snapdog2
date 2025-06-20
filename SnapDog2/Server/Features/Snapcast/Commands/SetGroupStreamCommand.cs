using MediatR;

namespace SnapDog2.Server.Features.Snapcast.Commands;

/// <summary>
/// Command to assign a specific stream to a group.
/// </summary>
/// <param name="GroupId">Unique identifier of the group</param>
/// <param name="StreamId">Unique identifier of the stream</param>
public record SetGroupStreamCommand(string GroupId, string StreamId) : IRequest<bool>;
