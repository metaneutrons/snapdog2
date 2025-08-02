namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to get the current system status.
/// </summary>
public record GetSystemStatusQuery : IQuery<Result<SystemStatus>>;
