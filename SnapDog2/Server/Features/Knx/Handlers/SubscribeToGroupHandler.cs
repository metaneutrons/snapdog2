using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Handlers;

/// <summary>
/// Handler for subscribing to KNX group address changes.
/// </summary>
public class SubscribeToGroupHandler : IRequestHandler<SubscribeToGroupCommand, bool>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<SubscribeToGroupHandler> _logger;

    public SubscribeToGroupHandler(IKnxService knxService, ILogger<SubscribeToGroupHandler> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(SubscribeToGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Subscribing to KNX group address {Address}{Description}",
            request.Address,
            !string.IsNullOrEmpty(request.Description) ? $" ({request.Description})" : ""
        );

        try
        {
            var result = await _knxService.SubscribeToGroupAsync(request.Address, cancellationToken);

            if (result)
            {
                _logger.LogDebug("Successfully subscribed to KNX group address {Address}", request.Address);
            }
            else
            {
                _logger.LogWarning("Failed to subscribe to KNX group address {Address}", request.Address);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to KNX group address {Address}", request.Address);
            return false;
        }
    }
}
