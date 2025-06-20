using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Snapcast.Queries;

namespace SnapDog2.Server.Features.Snapcast.Handlers;

/// <summary>
/// Handler for retrieving the Snapcast server status.
/// </summary>
public class GetSnapcastServerStatusHandler : IRequestHandler<GetSnapcastServerStatusQuery, string>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<GetSnapcastServerStatusHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSnapcastServerStatusHandler"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="logger">The logger.</param>
    public GetSnapcastServerStatusHandler(
        ISnapcastService snapcastService,
        ILogger<GetSnapcastServerStatusHandler> logger
    )
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the get Snapcast server status query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server status as JSON string.</returns>
    public async Task<string> Handle(GetSnapcastServerStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing GetSnapcastServerStatusQuery");

        try
        {
            var result = await _snapcastService.GetServerStatusAsync(cancellationToken);

            _logger.LogInformation("GetSnapcastServerStatusQuery completed successfully");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetSnapcastServerStatusQuery");
            throw;
        }
    }
}
