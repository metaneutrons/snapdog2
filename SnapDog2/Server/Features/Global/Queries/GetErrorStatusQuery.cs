namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to get the latest system error information.
/// </summary>
public record GetErrorStatusQuery : IQuery<Result<ErrorDetails?>>;
