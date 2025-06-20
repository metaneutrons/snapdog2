using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Snapcast.Queries;

namespace SnapDog2.Server.Features.Snapcast.Handlers;

/// <summary>
/// Handler for retrieving Snapcast groups.
/// </summary>
public class GetSnapcastGroupsHandler : IRequestHandler<GetSnapcastGroupsQuery, IEnumerable<string>>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<GetSnapcastGroupsHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSnapcastGroupsHandler"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="logger">The logger.</param>
    public GetSnapcastGroupsHandler(ISnapcastService snapcastService, ILogger<GetSnapcastGroupsHandler> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the get Snapcast groups query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of group identifiers.</returns>
    public async Task<IEnumerable<string>> Handle(GetSnapcastGroupsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing GetSnapcastGroupsQuery");

        try
        {
            var result = await _snapcastService.GetGroupsAsync(cancellationToken);

            _logger.LogInformation(
                "GetSnapcastGroupsQuery completed successfully with {GroupCount} groups",
                result.Count()
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetSnapcastGroupsQuery");
            throw;
        }
    }
}
