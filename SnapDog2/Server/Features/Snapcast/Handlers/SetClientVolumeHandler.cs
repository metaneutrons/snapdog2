using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Snapcast.Commands;

namespace SnapDog2.Server.Features.Snapcast.Handlers;

/// <summary>
/// Handler for setting the volume level of a Snapcast client.
/// </summary>
public class SetClientVolumeHandler : IRequestHandler<SetClientVolumeCommand, bool>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<SetClientVolumeHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetClientVolumeHandler"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="logger">The logger.</param>
    public SetClientVolumeHandler(ISnapcastService snapcastService, ILogger<SetClientVolumeHandler> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the set client volume command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    public async Task<bool> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing SetClientVolumeCommand for client {ClientId} with volume {Volume}",
            request.ClientId,
            request.Volume
        );

        try
        {
            var result = await _snapcastService.SetClientVolumeAsync(
                request.ClientId,
                request.Volume,
                cancellationToken
            );

            _logger.LogInformation(
                "SetClientVolumeCommand completed for client {ClientId}: {Success}",
                request.ClientId,
                result ? "Success" : "Failed"
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing SetClientVolumeCommand for client {ClientId} with volume {Volume}",
                request.ClientId,
                request.Volume
            );
            throw;
        }
    }
}
