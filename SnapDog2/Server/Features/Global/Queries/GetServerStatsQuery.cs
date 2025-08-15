namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to get server performance statistics.
/// </summary>
public record GetServerStatsQuery : IQuery<Result<ServerStats>>;
