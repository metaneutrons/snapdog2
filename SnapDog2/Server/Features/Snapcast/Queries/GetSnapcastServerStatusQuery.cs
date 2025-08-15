namespace SnapDog2.Server.Features.Snapcast.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to get the current Snapcast server status.
/// </summary>
public record GetSnapcastServerStatusQuery : IQuery<Result<SnapcastServerStatus>>;
