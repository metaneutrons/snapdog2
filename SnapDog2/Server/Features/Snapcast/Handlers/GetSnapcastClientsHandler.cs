using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Snapcast.Queries;

namespace SnapDog2.Server.Features.Snapcast.Handlers;

/// <summary>
/// Handler for retrieving Snapcast clients.
/// </summary>
public class GetSnapcastClientsHandler : IRequestHandler<GetSnapcastClientsQuery, IEnumerable<string>>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<GetSnapcastClientsHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSnapcastClientsHandler"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="logger">The logger.</param>
    public GetSnapcastClientsHandler(ISnapcastService snapcastService, ILogger<GetSnapcastClientsHandler> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the get Snapcast clients query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of client identifiers.</returns>
    public async Task<IEnumerable<string>> Handle(GetSnapcastClientsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing GetSnapcastClientsQuery");

        try
        {
            var result = await _snapcastService.GetClientsAsync(cancellationToken);

            _logger.LogInformation(
                "GetSnapcastClientsQuery completed successfully with {ClientCount} clients",
                result.Count()
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetSnapcastClientsQuery");
            throw;
        }
    }
}
