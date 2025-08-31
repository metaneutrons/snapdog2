//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Server.Global.Handlers;

using Cortex.Mediator.Queries;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Shared.Models;

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

    [LoggerMessage(
        EventId = 7200,
        Level = LogLevel.Information,
        Message = "Handling GetServerStatsQuery"
    )]
    private partial void LogHandling();

    [LoggerMessage(
        EventId = 7201,
        Level = LogLevel.Error,
        Message = "Error retrieving server statistics"
    )]
    private partial void LogError(Exception ex);
}
