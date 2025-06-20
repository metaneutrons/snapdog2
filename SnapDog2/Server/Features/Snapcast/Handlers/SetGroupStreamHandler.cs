using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Snapcast.Commands;

namespace SnapDog2.Server.Features.Snapcast.Handlers;

/// <summary>
/// Handler for assigning a stream to a Snapcast group.
/// </summary>
public class SetGroupStreamHandler : IRequestHandler<SetGroupStreamCommand, bool>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<SetGroupStreamHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetGroupStreamHandler"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="logger">The logger.</param>
    public SetGroupStreamHandler(ISnapcastService snapcastService, ILogger<SetGroupStreamHandler> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the set group stream command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    public async Task<bool> Handle(SetGroupStreamCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing SetGroupStreamCommand for group {GroupId} with stream {StreamId}",
            request.GroupId,
            request.StreamId
        );

        try
        {
            var result = await _snapcastService.SetGroupStreamAsync(
                request.GroupId,
                request.StreamId,
                cancellationToken
            );

            _logger.LogInformation(
                "SetGroupStreamCommand completed for group {GroupId}: {Success}",
                request.GroupId,
                result ? "Success" : "Failed"
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing SetGroupStreamCommand for group {GroupId} with stream {StreamId}",
                request.GroupId,
                request.StreamId
            );
            throw;
        }
    }
}
