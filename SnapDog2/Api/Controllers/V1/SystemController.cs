namespace SnapDog2.Api.Controllers.V1;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Controller for system-wide information endpoints.
/// </summary>
[ApiController]
[Route("api/v1/system")]
[Authorize]
[Produces("application/json")]
public partial class SystemController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SystemController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemController"/> class.
    /// </summary>
    public SystemController(IServiceProvider serviceProvider, ILogger<SystemController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

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
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetSystemStatusQueryHandler>();
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
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetErrorStatusQueryHandler>();
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
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetVersionInfoQueryHandler>();
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
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetServerStatsQueryHandler>();
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
        2301,
        LogLevel.Critical,
        "🚨 CRITICAL: Handler {HandlerType} not found in DI container - This is a configuration BUG!"
    )]
    private partial void LogCriticalHandlerNotFound(string handlerType);

    // System Status (2310-2319)
    [LoggerMessage(2310, LogLevel.Debug, "Getting system status")]
    private partial void LogGettingSystemStatus();

    [LoggerMessage(2311, LogLevel.Warning, "Failed to get system status: {Error}")]
    private partial void LogFailedToGetSystemStatus(string? error);

    [LoggerMessage(2312, LogLevel.Error, "Error getting system status")]
    private partial void LogErrorGettingSystemStatus(Exception exception);

    // System Errors (2320-2329)
    [LoggerMessage(2320, LogLevel.Debug, "Getting system errors")]
    private partial void LogGettingSystemErrors();

    [LoggerMessage(2321, LogLevel.Warning, "Failed to get system errors: {Error}")]
    private partial void LogFailedToGetSystemErrors(string? error);

    [LoggerMessage(2322, LogLevel.Error, "Error getting system errors")]
    private partial void LogErrorGettingSystemErrors(Exception exception);

    // System Version (2330-2339)
    [LoggerMessage(2330, LogLevel.Debug, "Getting system version")]
    private partial void LogGettingSystemVersion();

    [LoggerMessage(2331, LogLevel.Warning, "Failed to get system version: {Error}")]
    private partial void LogFailedToGetSystemVersion(string? error);

    [LoggerMessage(2332, LogLevel.Error, "Error getting system version")]
    private partial void LogErrorGettingSystemVersion(Exception exception);

    // System Statistics (2340-2349)
    [LoggerMessage(2340, LogLevel.Debug, "Getting system statistics")]
    private partial void LogGettingSystemStatistics();

    [LoggerMessage(2341, LogLevel.Warning, "Failed to get system statistics: {Error}")]
    private partial void LogFailedToGetSystemStatistics(string? error);

    [LoggerMessage(2342, LogLevel.Error, "Error getting system statistics")]
    private partial void LogErrorGettingSystemStatistics(Exception exception);

    #endregion
}
