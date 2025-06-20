using MediatR;
using SnapDog2.Infrastructure.Services.Models;

namespace SnapDog2.Server.Features.Knx.Queries;

/// <summary>
/// Query to get information about KNX devices and group addresses.
/// </summary>
public record GetKnxDevicesQuery : IRequest<IEnumerable<KnxDeviceInfo>>
{
    /// <summary>
    /// Gets whether to include only subscribed addresses.
    /// </summary>
    public bool OnlySubscribed { get; init; } = false;

    /// <summary>
    /// Gets whether to include device details.
    /// </summary>
    public bool IncludeDetails { get; init; } = true;
}
