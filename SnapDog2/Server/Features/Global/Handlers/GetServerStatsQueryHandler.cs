namespace SnapDog2.Server.Features.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the GetServerStatsQuery.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GetServerStatsQueryHandler"/> class.
/// </remarks>
/// <param name="metricsService">The metrics service.</param>
/// <param name="logger">The logger instance.</param>
public partial class GetServerStatsQueryHandler(
    IMetricsService metricsService,
    ILogger<GetServerStatsQueryHandler> logger
) : IQueryHandler<GetServerStatsQuery, Result<ServerStats>>
{
    private readonly IMetricsService _metricsService = metricsService;
    private readonly ILogger<GetServerStatsQueryHandler> _logger = logger;

    /// <inheritdoc/>
    public async Task<Result<ServerStats>> Handle(GetServerStatsQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var stats = await this._metricsService.GetServerStatsAsync().ConfigureAwait(false);
            return Result<ServerStats>.Success(stats);
        }
        catch (Exception ex)
        {
            this.LogError(ex);
            return Result<ServerStats>.Failure(ex);
        }
    }

    [LoggerMessage(1005, LogLevel.Information, "Handling GetServerStatsQuery")]
    private partial void LogHandling();

    [LoggerMessage(1006, LogLevel.Error, "Error retrieving server statistics")]
    private partial void LogError(Exception ex);
}
