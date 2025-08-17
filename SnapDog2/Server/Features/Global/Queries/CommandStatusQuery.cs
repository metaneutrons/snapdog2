namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve the current command processing status.
/// </summary>
public record CommandStatusQuery : IQuery<Result<string>>;
