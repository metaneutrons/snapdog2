namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to get system version information.
/// </summary>
public record GetVersionInfoQuery : IQuery<Result<VersionDetails>>;
