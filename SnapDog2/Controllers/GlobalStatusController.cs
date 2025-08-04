namespace SnapDog2.Controllers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Controller for global system status endpoints.
/// Follows CQRS pattern using Cortex.Mediator for enterprise-grade architecture compliance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GlobalStatusController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GlobalStatusController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStatusController"/> class.
    /// </summary>
    /// <param name="mediator">The Cortex.Mediator instance for CQRS command/query dispatch.</param>
    /// <param name="logger">The logger instance.</param>
    public GlobalStatusController(IMediator mediator, ILogger<GlobalStatusController> logger)
    {
        this._mediator = mediator;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current system status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current system status wrapped in ApiResponse.</returns>
    [HttpGet("system")]
    [ProducesResponseType(typeof(ApiResponse<SystemStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SystemStatus>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SystemStatus>), 500)]
    public async Task<ActionResult<ApiResponse<SystemStatus>>> GetSystemStatus(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting system status via CQRS mediator");

            var query = new GetSystemStatusQuery();
            var result = await this._mediator.SendQueryAsync<GetSystemStatusQuery, Result<SystemStatus>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<SystemStatus>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get system status: {Error}", result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<SystemStatus>.CreateError(
                    "SYSTEM_STATUS_ERROR",
                    result.ErrorMessage ?? "Failed to get system status"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting system status");
            return this.StatusCode(
                500,
                ApiResponse<SystemStatus>.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Gets the latest system error information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest error details wrapped in ApiResponse, or null if no recent errors.</returns>
    [HttpGet("error")]
    [ProducesResponseType(typeof(ApiResponse<ErrorDetails>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ErrorDetails>), 204)]
    [ProducesResponseType(typeof(ApiResponse<ErrorDetails>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ErrorDetails>), 500)]
    public async Task<ActionResult<ApiResponse<ErrorDetails?>>> GetErrorStatus(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting error status via CQRS mediator");

            var query = new GetErrorStatusQuery();
            var result = await this._mediator.SendQueryAsync<GetErrorStatusQuery, Result<ErrorDetails?>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                if (result.Value != null)
                {
                    return this.Ok(ApiResponse<ErrorDetails>.CreateSuccess(result.Value));
                }
                else
                {
                    return this.NoContent(); // No recent errors - return 204 without ApiResponse wrapper
                }
            }

            this._logger.LogWarning("Failed to get error status: {Error}", result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<ErrorDetails?>.CreateError(
                    "ERROR_STATUS_ERROR",
                    result.ErrorMessage ?? "Failed to get error status"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting error status");
            return this.StatusCode(
                500,
                ApiResponse<ErrorDetails?>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Gets the current version information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current version information wrapped in ApiResponse.</returns>
    [HttpGet("version")]
    [ProducesResponseType(typeof(ApiResponse<VersionDetails>), 200)]
    [ProducesResponseType(typeof(ApiResponse<VersionDetails>), 400)]
    [ProducesResponseType(typeof(ApiResponse<VersionDetails>), 500)]
    public async Task<ActionResult<ApiResponse<VersionDetails>>> GetVersionInfo(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting version info via CQRS mediator");

            var query = new GetVersionInfoQuery();
            var result = await this._mediator.SendQueryAsync<GetVersionInfoQuery, Result<VersionDetails>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<VersionDetails>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get version info: {Error}", result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<VersionDetails>.CreateError(
                    "VERSION_INFO_ERROR",
                    result.ErrorMessage ?? "Failed to get version info"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting version info");
            return this.StatusCode(
                500,
                ApiResponse<VersionDetails>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Gets the current server statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current server statistics wrapped in ApiResponse.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ServerStats>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ServerStats>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ServerStats>), 500)]
    public async Task<ActionResult<ApiResponse<ServerStats>>> GetServerStats(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting server stats via CQRS mediator");

            var query = new GetServerStatsQuery();
            var result = await this._mediator.SendQueryAsync<GetServerStatsQuery, Result<ServerStats>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ServerStats>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get server stats: {Error}", result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<ServerStats>.CreateError(
                    "SERVER_STATS_ERROR",
                    result.ErrorMessage ?? "Failed to get server stats"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting server stats");
            return this.StatusCode(
                500,
                ApiResponse<ServerStats>.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }
}
