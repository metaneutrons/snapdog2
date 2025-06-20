using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Handlers;

/// <summary>
/// Handler for unsubscribing from KNX group address changes.
/// </summary>
public class UnsubscribeFromGroupHandler : IRequestHandler<UnsubscribeFromGroupCommand, bool>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<UnsubscribeFromGroupHandler> _logger;

    public UnsubscribeFromGroupHandler(IKnxService knxService, ILogger<UnsubscribeFromGroupHandler> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(UnsubscribeFromGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Unsubscribing from KNX group address {Address}{Description}",
            request.Address,
            !string.IsNullOrEmpty(request.Description) ? $" ({request.Description})" : ""
        );

        try
        {
            var result = await _knxService.UnsubscribeFromGroupAsync(request.Address, cancellationToken);

            if (result)
            {
                _logger.LogDebug("Successfully unsubscribed from KNX group address {Address}", request.Address);
            }
            else
            {
                _logger.LogWarning("Failed to unsubscribe from KNX group address {Address}", request.Address);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from KNX group address {Address}", request.Address);
            return false;
        }
    }
}
