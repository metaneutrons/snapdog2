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
namespace SnapDog2.Api.Controllers.V1;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Server.Global.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Controller for system-wide information endpoints.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SystemController"/> class.
/// </remarks>
[ApiController]
[Route("api/v1/system")]
[Authorize]
[Produces("application/json")]
public partial class SystemController(
    IServiceProvider serviceProvider,
    IMediator mediator,
    ILogger<SystemController> logger
) : ControllerBase
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<SystemController> _logger = logger;

    /// <summary>
    /// Gets system online status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>System status information.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SystemStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<SystemStatus>>> GetSystemStatus(CancellationToken cancellationToken)
    {
        try
        {
            this.LogGettingSystemStatus();

            var handler =
                this._serviceProvider.GetService<Server.Global.Handlers.GetSystemStatusQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetSystemStatusQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<SystemStatus>.CreateError("HANDLER_NOT_FOUND", "System status handler not available")
                );
            }

            var result = await handler.Handle(new GetSystemStatusQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<SystemStatus>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetSystemStatus(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<SystemStatus>.CreateError(
                    "SYSTEM_STATUS_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve system status"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingSystemStatus(ex);
            return this.StatusCode(
                500,
                ApiResponse<SystemStatus>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Gets recent system errors.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of recent system errors.</returns>
    [HttpGet("errors")]
    [ProducesResponseType(typeof(ApiResponse<List<ErrorDetails>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<ErrorDetails>>>> GetSystemErrors(
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingSystemErrors();

            var handler =
                this._serviceProvider.GetService<Server.Global.Handlers.GetErrorStatusQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetErrorStatusQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<List<ErrorDetails>>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Error status handler not available"
                    )
                );
            }

            var result = await handler.Handle(new GetErrorStatusQuery(), cancellationToken);

            if (result.IsSuccess)
            {
                // Convert single ErrorDetails to List<ErrorDetails>
                var errorList =
                    result.Value != null ? new List<ErrorDetails> { result.Value } : new List<ErrorDetails>();
                return this.Ok(ApiResponse<List<ErrorDetails>>.CreateSuccess(errorList));
            }

            this.LogFailedToGetSystemErrors(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<List<ErrorDetails>>.CreateError(
                    "ERROR_STATUS_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve system errors"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingSystemErrors(ex);
            return this.StatusCode(
                500,
                ApiResponse<List<ErrorDetails>>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Gets software version information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Version information.</returns>
    [HttpGet("version")]
    [ProducesResponseType(typeof(ApiResponse<VersionDetails>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VersionDetails>>> GetVersion(CancellationToken cancellationToken)
    {
        try
        {
            this.LogGettingSystemVersion();

            var handler =
                this._serviceProvider.GetService<Server.Global.Handlers.GetVersionInfoQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetVersionInfoQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<VersionDetails>.CreateError("HANDLER_NOT_FOUND", "Version info handler not available")
                );
            }

            var result = await handler.Handle(new GetVersionInfoQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<VersionDetails>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetSystemVersion(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<VersionDetails>.CreateError(
                    "VERSION_INFO_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve version information"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingSystemVersion(ex);
            return this.StatusCode(
                500,
                ApiResponse<VersionDetails>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Gets server performance statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Server statistics.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ServerStats>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ServerStats>>> GetServerStats(CancellationToken cancellationToken)
    {
        try
        {
            this.LogGettingSystemStatistics();

            var handler =
                this._serviceProvider.GetService<Server.Global.Handlers.GetServerStatsQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetServerStatsQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<ServerStats>.CreateError("HANDLER_NOT_FOUND", "Server stats handler not available")
                );
            }

            var result = await handler.Handle(new GetServerStatsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ServerStats>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetSystemStatistics(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<ServerStats>.CreateError(
                    "SERVER_STATS_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve server statistics"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingSystemStatistics(ex);
            return this.StatusCode(
                500,
                ApiResponse<ServerStats>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    #region Logging

    // ARCHITECTURAL PROBLEM - This should never happen in production
    [LoggerMessage(
        EventId = 5200,
        Level = Microsoft.Extensions.Logging.LogLevel.Critical,
        Message = "🚨 CRITICAL: Handler {HandlerType} not found in DI container - This is a configuration BUG!"
    )]
    private partial void LogCriticalHandlerNotFound(string handlerType);

    // System Status (2310-2319)
    [LoggerMessage(
        EventId = 5201,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting system status"
    )]
    private partial void LogGettingSystemStatus();

    [LoggerMessage(
        EventId = 5202,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get system status: {Error}"
    )]
    private partial void LogFailedToGetSystemStatus(string? error);

    [LoggerMessage(
        EventId = 5203,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting system status"
    )]
    private partial void LogErrorGettingSystemStatus(Exception exception);

    // System Errors (2320-2329)
    [LoggerMessage(
        EventId = 5204,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting system errors"
    )]
    private partial void LogGettingSystemErrors();

    [LoggerMessage(
        EventId = 5205,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get system errors: {Error}"
    )]
    private partial void LogFailedToGetSystemErrors(string? error);

    [LoggerMessage(
        EventId = 5206,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting system errors"
    )]
    private partial void LogErrorGettingSystemErrors(Exception exception);

    // System Version (2330-2339)
    [LoggerMessage(
        EventId = 5207,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting system version"
    )]
    private partial void LogGettingSystemVersion();

    [LoggerMessage(
        EventId = 5208,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get system version: {Error}"
    )]
    private partial void LogFailedToGetSystemVersion(string? error);

    [LoggerMessage(
        EventId = 5209,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting system version"
    )]
    private partial void LogErrorGettingSystemVersion(Exception exception);

    // System Statistics (2340-2349)
    [LoggerMessage(
        EventId = 5210,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting system statistics"
    )]
    private partial void LogGettingSystemStatistics();

    [LoggerMessage(
        EventId = 5211,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get system statistics: {Error}"
    )]
    private partial void LogFailedToGetSystemStatistics(string? error);

    [LoggerMessage(
        EventId = 5212,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting system statistics"
    )]
    private partial void LogErrorGettingSystemStatistics(Exception exception);

    // Command Status (2350-2359)
    [LoggerMessage(
        EventId = 5213,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting command processing status"
    )]
    private partial void LogGettingCommandStatus();

    [LoggerMessage(
        EventId = 5214,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get command status: {Error}"
    )]
    private partial void LogFailedToGetCommandStatus(string? error);

    [LoggerMessage(
        EventId = 5215,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting command status"
    )]
    private partial void LogErrorGettingCommandStatus(Exception exception);

    [LoggerMessage(
        EventId = 5216,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting command errors"
    )]
    private partial void LogGettingCommandErrors();

    [LoggerMessage(
        EventId = 5217,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get command errors: {Error}"
    )]
    private partial void LogFailedToGetCommandErrors(string? error);

    [LoggerMessage(
        EventId = 5218,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error getting command errors"
    )]
    private partial void LogErrorGettingCommandErrors(Exception exception);

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════════
    // COMMAND STATUS ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get current command processing status.
    /// </summary>
    /// <returns>Command processing status</returns>
    [HttpGet("commands/status")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> GetCommandStatus()
    {
        this.LogGettingCommandStatus();

        try
        {
            var query = new CommandStatusQuery();
            var result = await this._mediator.SendQueryAsync<CommandStatusQuery, Result<string>>(query);

            if (result.IsFailure)
            {
                this.LogFailedToGetCommandStatus(result.ErrorMessage);
                return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
            }

            return this.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            this.LogErrorGettingCommandStatus(ex);
            return this.Problem(
                "An error occurred while getting command status",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get recent command errors.
    /// </summary>
    /// <returns>Array of recent command error messages</returns>
    [HttpGet("commands/errors")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string[]>> GetCommandErrors()
    {
        this.LogGettingCommandErrors();

        try
        {
            var query = new CommandErrorsQuery();
            var result = await this._mediator.SendQueryAsync<CommandErrorsQuery, Result<string[]>>(query);

            if (result.IsFailure)
            {
                this.LogFailedToGetCommandErrors(result.ErrorMessage);
                return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
            }

            return this.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            this.LogErrorGettingCommandErrors(ex);
            return this.Problem(
                "An error occurred while getting command errors",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
