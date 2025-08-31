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

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles the CommandStatusQuery.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandStatusQueryHandler"/> class.
/// </remarks>
/// <param name="commandStatusService">The command status service.</param>
/// <param name="logger">The logger instance.</param>
public partial class CommandStatusQueryHandler(
    ICommandStatusService commandStatusService,
    ILogger<CommandStatusQueryHandler> logger
) : IQueryHandler<CommandStatusQuery, Result<string>>
{
    private readonly ICommandStatusService _commandStatusService = commandStatusService;
    private readonly ILogger<CommandStatusQueryHandler> _logger = logger;

    [LoggerMessage(
        EventId = 9300,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Retrieving command processing status"
    )]
    private partial void LogRetrievingCommandStatus();

    [LoggerMessage(
        EventId = 9301,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to retrieve command status: {ErrorMessage}"
    )]
    private partial void LogFailedToRetrieveCommandStatus(string errorMessage);

    /// <summary>
    /// Handles the CommandStatusQuery.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command processing status.</returns>
    public async Task<Result<string>> Handle(CommandStatusQuery request, CancellationToken cancellationToken)
    {
        this.LogRetrievingCommandStatus();

        try
        {
            var status = await this._commandStatusService.GetStatusAsync().ConfigureAwait(false);
            return Result<string>.Success(status);
        }
        catch (System.Exception ex)
        {
            this.LogFailedToRetrieveCommandStatus(ex.Message);
            return Result<string>.Failure($"Failed to retrieve command status: {ex.Message}");
        }
    }
}
