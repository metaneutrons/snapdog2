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
public class SystemController : ControllerBase
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
            this._logger.LogDebug("Getting system status");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetSystemStatusQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetSystemStatusQueryHandler not found in DI container");
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

            this._logger.LogWarning("Failed to get system status: {Error}", result.ErrorMessage);
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
            this._logger.LogError(ex, "Error getting system status");
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
            this._logger.LogDebug("Getting system errors");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetErrorStatusQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetErrorStatusQueryHandler not found in DI container");
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

            this._logger.LogWarning("Failed to get system errors: {Error}", result.ErrorMessage);
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
            this._logger.LogError(ex, "Error getting system errors");
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
            this._logger.LogDebug("Getting version information");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetVersionInfoQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetVersionInfoQueryHandler not found in DI container");
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

            this._logger.LogWarning("Failed to get version information: {Error}", result.ErrorMessage);
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
            this._logger.LogError(ex, "Error getting version information");
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
            this._logger.LogDebug("Getting server statistics");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetServerStatsQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetServerStatsQueryHandler not found in DI container");
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

            this._logger.LogWarning("Failed to get server statistics: {Error}", result.ErrorMessage);
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
            this._logger.LogError(ex, "Error getting server statistics");
            return this.StatusCode(
                500,
                ApiResponse<ServerStats>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }
}
