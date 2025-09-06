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
/// Handles the CommandErrorsQuery.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandErrorsQueryHandler"/> class.
/// </remarks>
/// <param name="commandStatusService">The command status service.</param>
/// <param name="logger">The logger instance.</param>
public partial class CommandErrorsQueryHandler(
    ICommandStatusService commandStatusService,
    ILogger<CommandErrorsQueryHandler> logger
) : IQueryHandler<CommandErrorsQuery, Result<string[]>>
{
    private readonly ICommandStatusService _commandStatusService = commandStatusService;
    private readonly ILogger<CommandErrorsQueryHandler> _logger = logger;

    [LoggerMessage(EventId = 112550, Level = LogLevel.Debug, Message = "Retrieving recent command errors"
)]
    private partial void LogRetrievingCommandErrors();

    [LoggerMessage(EventId = 112551, Level = LogLevel.Warning, Message = "Failed to retrieve command errors: {ErrorMessage}"
)]
    private partial void LogFailedToRetrieveCommandErrors(string errorMessage);

    /// <summary>
    /// Handles the CommandErrorsQuery.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Array of recent command error messages.</returns>
    public async Task<Result<string[]>> Handle(CommandErrorsQuery request, CancellationToken cancellationToken)
    {
        this.LogRetrievingCommandErrors();

        try
        {
            var errors = await this._commandStatusService.GetRecentErrorsAsync().ConfigureAwait(false);
            return Result<string[]>.Success(errors);
        }
        catch (Exception ex)
        {
            this.LogFailedToRetrieveCommandErrors(ex.Message);
            return Result<string[]>.Failure($"Failed to retrieve command errors: {ex.Message}");
        }
    }
}
