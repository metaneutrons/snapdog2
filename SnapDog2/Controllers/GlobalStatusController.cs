namespace SnapDog2.Controllers;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Controller for global system status endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GlobalStatusController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalStatusController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStatusController"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    public GlobalStatusController(IServiceProvider serviceProvider, ILogger<GlobalStatusController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current system status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current system status.</returns>
    [HttpGet("system")]
    [ProducesResponseType(typeof(SystemStatus), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SystemStatus>> GetSystemStatus(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting system status");

            // Try to get the handler from DI
            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetSystemStatusQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetSystemStatusQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetSystemStatusQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(result.Value);
            }

            this._logger.LogWarning("Failed to get system status: {Error}", result.ErrorMessage);
            return this.BadRequest(new { error = result.ErrorMessage ?? "Failed to get system status" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting system status");
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the latest system error information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest error details or null if no recent errors.</returns>
    [HttpGet("error")]
    [ProducesResponseType(typeof(ErrorDetails), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ErrorDetails?>> GetErrorStatus(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting error status");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetErrorStatusQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetErrorStatusQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetErrorStatusQuery(), cancellationToken);

            if (result.IsSuccess)
            {
                if (result.Value != null)
                {
                    return this.Ok(result.Value);
                }
                else
                {
                    return this.NoContent(); // No recent errors
                }
            }

            this._logger.LogWarning("Failed to get error status: {Error}", result.ErrorMessage);
            return this.BadRequest(new { error = result.ErrorMessage ?? "Failed to get error status" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting error status");
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the current version information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current version information.</returns>
    [HttpGet("version")]
    [ProducesResponseType(typeof(VersionDetails), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<VersionDetails>> GetVersionInfo(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting version info");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetVersionInfoQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetVersionInfoQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetVersionInfoQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(result.Value);
            }

            this._logger.LogWarning("Failed to get version info: {Error}", result.ErrorMessage);
            return this.BadRequest(new { error = result.ErrorMessage ?? "Failed to get version info" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting version info");
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the current server statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current server statistics.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ServerStats), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ServerStats>> GetServerStats(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting server stats");

            var handler =
                this._serviceProvider.GetService<Server.Features.Global.Handlers.GetServerStatsQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetServerStatsQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetServerStatsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(result.Value);
            }

            this._logger.LogWarning("Failed to get server stats: {Error}", result.ErrorMessage);
            return this.BadRequest(new { error = result.ErrorMessage ?? "Failed to get server stats" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception while getting server stats");
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }
}
