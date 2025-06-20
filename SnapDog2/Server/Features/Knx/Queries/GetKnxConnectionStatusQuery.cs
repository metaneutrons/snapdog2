using MediatR;
using SnapDog2.Infrastructure.Services.Models;

namespace SnapDog2.Server.Features.Knx.Queries;

/// <summary>
/// Query to get the current KNX connection status and information.
/// </summary>
public record GetKnxConnectionStatusQuery : IRequest<KnxConnectionStatus>
{
    /// <summary>
    /// Gets whether to include detailed connection information.
    /// </summary>
    public bool IncludeDetails { get; init; } = false;
}
