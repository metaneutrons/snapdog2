using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Infrastructure.Services.Models;
using SnapDog2.Server.Features.Knx.Queries;

namespace SnapDog2.Server.Features.Knx.Handlers;

/// <summary>
/// Handler for getting KNX device information.
/// </summary>
public class GetKnxDevicesHandler : IRequestHandler<GetKnxDevicesQuery, IEnumerable<KnxDeviceInfo>>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<GetKnxDevicesHandler> _logger;

    public GetKnxDevicesHandler(IKnxService knxService, ILogger<GetKnxDevicesHandler> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<KnxDeviceInfo>> Handle(GetKnxDevicesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting KNX devices{OnlySubscribed}{Details}",
            request.OnlySubscribed ? " (only subscribed)" : "",
            request.IncludeDetails ? " with details" : ""
        );

        try
        {
            // For now, return empty list - this could be enhanced to get actual devices from the service
            var devices = new List<KnxDeviceInfo>();

            if (request.IncludeDetails)
            {
                _logger.LogDebug("Including detailed KNX device information");
            }

            if (request.OnlySubscribed)
            {
                _logger.LogDebug("Filtering to only subscribed KNX addresses");
            }

            _logger.LogDebug("Found {Count} KNX devices", devices.Count);
            return Task.FromResult<IEnumerable<KnxDeviceInfo>>(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KNX devices");
            return Task.FromResult<IEnumerable<KnxDeviceInfo>>(new List<KnxDeviceInfo>());
        }
    }
}
