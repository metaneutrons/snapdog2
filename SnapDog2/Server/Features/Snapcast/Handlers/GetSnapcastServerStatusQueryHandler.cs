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
namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Queries;

/// <summary>
/// Handler for getting Snapcast server status.
/// </summary>
public partial class GetSnapcastServerStatusQueryHandler(
    ISnapcastService snapcastService,
    ILogger<GetSnapcastServerStatusQueryHandler> logger
) : IQueryHandler<GetSnapcastServerStatusQuery, Result<SnapcastServerStatus>>
{
    private readonly ISnapcastService _snapcastService = snapcastService;
    private readonly ILogger<GetSnapcastServerStatusQueryHandler> _logger = logger;

    [LoggerMessage(
        EventId = 2800,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Getting Snapcast server status"
    )]
    private partial void LogGettingServerStatus();

    [LoggerMessage(
        EventId = 2801,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to get Snapcast server status"
    )]
    private partial void LogGetServerStatusFailed(Exception ex);

    public async Task<Result<SnapcastServerStatus>> Handle(
        GetSnapcastServerStatusQuery query,
        CancellationToken cancellationToken
    )
    {
        this.LogGettingServerStatus();

        try
        {
            var result = await this._snapcastService.GetServerStatusAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogGetServerStatusFailed(new InvalidOperationException(result.ErrorMessage ?? "Unknown error"));
                return Result<SnapcastServerStatus>.Failure(result.ErrorMessage ?? "Unknown error");
            }

            return Result<SnapcastServerStatus>.Success(result.Value!);
        }
        catch (Exception ex)
        {
            this.LogGetServerStatusFailed(ex);
            return Result<SnapcastServerStatus>.Failure(ex);
        }
    }
}
