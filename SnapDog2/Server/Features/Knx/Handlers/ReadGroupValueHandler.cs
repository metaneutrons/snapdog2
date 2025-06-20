using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Handlers;

/// <summary>
/// Handler for reading values from KNX group addresses.
/// </summary>
public class ReadGroupValueHandler : IRequestHandler<ReadGroupValueCommand, byte[]?>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<ReadGroupValueHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadGroupValueHandler"/> class.
    /// </summary>
    /// <param name="knxService">The KNX service.</param>
    /// <param name="logger">The logger.</param>
    public ReadGroupValueHandler(IKnxService knxService, ILogger<ReadGroupValueHandler> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the read group value command.
    /// </summary>
    /// <param name="request">The read group value command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value read from the group address, or null if the read failed.</returns>
    public async Task<byte[]?> Handle(ReadGroupValueCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reading value from KNX group address {Address}{Description}",
            request.Address,
            !string.IsNullOrEmpty(request.Description) ? $" ({request.Description})" : ""
        );

        try
        {
            var result = await _knxService.ReadGroupValueAsync(request.Address, cancellationToken);

            if (result != null)
            {
                _logger.LogDebug(
                    "Successfully read {Length} bytes from KNX group address {Address}",
                    result.Length,
                    request.Address
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to read value from KNX group address {Address} - no response received",
                    request.Address
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading value from KNX group address {Address}", request.Address);
            return null;
        }
    }
}
