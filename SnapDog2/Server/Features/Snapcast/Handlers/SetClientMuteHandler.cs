using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Snapcast.Commands;

namespace SnapDog2.Server.Features.Snapcast.Handlers;

/// <summary>
/// Handler for setting the mute state of a Snapcast client.
/// </summary>
public class SetClientMuteHandler : IRequestHandler<SetClientMuteCommand, bool>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<SetClientMuteHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetClientMuteHandler"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="logger">The logger.</param>
    public SetClientMuteHandler(ISnapcastService snapcastService, ILogger<SetClientMuteHandler> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the set client mute command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    public async Task<bool> Handle(SetClientMuteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing SetClientMuteCommand for client {ClientId} with muted state {Muted}",
            request.ClientId,
            request.Muted
        );

        try
        {
            var result = await _snapcastService.SetClientMuteAsync(request.ClientId, request.Muted, cancellationToken);

            _logger.LogInformation(
                "SetClientMuteCommand completed for client {ClientId}: {Success}",
                request.ClientId,
                result ? "Success" : "Failed"
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing SetClientMuteCommand for client {ClientId} with muted state {Muted}",
                request.ClientId,
                request.Muted
            );
            throw;
        }
    }
}
