using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Knx.Commands;

namespace SnapDog2.Server.Features.Knx.Handlers;

/// <summary>
/// Handler for writing values to KNX group addresses.
/// </summary>
public class WriteGroupValueHandler : IRequestHandler<WriteGroupValueCommand, bool>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<WriteGroupValueHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteGroupValueHandler"/> class.
    /// </summary>
    /// <param name="knxService">The KNX service.</param>
    /// <param name="logger">The logger.</param>
    public WriteGroupValueHandler(IKnxService knxService, ILogger<WriteGroupValueHandler> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the write group value command.
    /// </summary>
    /// <param name="request">The write group value command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the write operation was successful, false otherwise.</returns>
    public async Task<bool> Handle(WriteGroupValueCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Writing value to KNX group address {Address}: {Value} bytes{Description}",
            request.Address,
            request.Value.Length,
            !string.IsNullOrEmpty(request.Description) ? $" ({request.Description})" : ""
        );

        try
        {
            var result = await _knxService.WriteGroupValueAsync(request.Address, request.Value, cancellationToken);

            if (result)
            {
                _logger.LogDebug("Successfully wrote value to KNX group address {Address}", request.Address);
            }
            else
            {
                _logger.LogWarning("Failed to write value to KNX group address {Address}", request.Address);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing value to KNX group address {Address}", request.Address);
            return false;
        }
    }
}
