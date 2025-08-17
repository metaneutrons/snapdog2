namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve recent command errors.
/// </summary>
public record CommandErrorsQuery : IQuery<Result<string[]>>;
