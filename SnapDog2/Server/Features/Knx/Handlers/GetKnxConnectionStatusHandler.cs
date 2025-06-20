using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Infrastructure.Services.Models;
using SnapDog2.Server.Features.Knx.Queries;

namespace SnapDog2.Server.Features.Knx.Handlers;

/// <summary>
/// Handler for getting KNX connection status information.
/// </summary>
public class GetKnxConnectionStatusHandler : IRequestHandler<GetKnxConnectionStatusQuery, KnxConnectionStatus>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<GetKnxConnectionStatusHandler> _logger;

    public GetKnxConnectionStatusHandler(IKnxService knxService, ILogger<GetKnxConnectionStatusHandler> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<KnxConnectionStatus> Handle(GetKnxConnectionStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting KNX connection status{Details}", request.IncludeDetails ? " with details" : "");

        try
        {
            // For now, return a basic status - this could be enhanced to get actual status from the service
            var status = new KnxConnectionStatus
            {
                IsConnected = true, // This would come from actual service status
                Gateway = "192.168.1.1", // This would come from configuration
                Port = 3671,
                ConnectionState = "Connected",
                LastConnectionAttempt = DateTime.UtcNow,
                LastSuccessfulConnection = DateTime.UtcNow,
                SubscribedAddressCount = 0,
                ErrorMessage = null,
            };

            if (request.IncludeDetails)
            {
                // Add more detailed information when requested
                _logger.LogDebug("Including detailed KNX connection information");
            }

            return Task.FromResult(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KNX connection status");

            var errorStatus = new KnxConnectionStatus
            {
                IsConnected = false,
                Gateway = "Unknown",
                Port = 0,
                ConnectionState = "Disconnected",
                LastConnectionAttempt = DateTime.UtcNow,
                LastSuccessfulConnection = null,
                SubscribedAddressCount = 0,
                ErrorMessage = ex.Message,
            };

            return Task.FromResult(errorStatus);
        }
    }
}
