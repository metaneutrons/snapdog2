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
namespace SnapDog2.Server.Features.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the GetSystemStatusQuery.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GetSystemStatusQueryHandler"/> class.
/// </remarks>
/// <param name="systemStatusService">The system status service.</param>
/// <param name="logger">The logger instance.</param>
public partial class GetSystemStatusQueryHandler(
    IAppStatusService systemStatusService,
    ILogger<GetSystemStatusQueryHandler> logger
) : IQueryHandler<GetSystemStatusQuery, Result<SystemStatus>>
{
    private readonly IAppStatusService _systemStatusService = systemStatusService;
    private readonly ILogger<GetSystemStatusQueryHandler> _logger = logger;

    /// <inheritdoc/>
    public async Task<Result<SystemStatus>> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var status = await this._systemStatusService.GetCurrentStatusAsync().ConfigureAwait(false);
            return Result<SystemStatus>.Success(status);
        }
        catch (Exception ex)
        {
            this.LogError(ex);
            return Result<SystemStatus>.Failure(ex);
        }
    }

    [LoggerMessage(1001, LogLevel.Information, "Handling GetSystemStatusQuery")]
    private partial void LogHandling();

    [LoggerMessage(1002, LogLevel.Error, "Error retrieving system status")]
    private partial void LogError(Exception ex);
}
