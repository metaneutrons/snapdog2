namespace SnapDog2.Api.Controllers.V1;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Api.Models;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Commands;
using SnapDog2.Server.Features.Snapcast.Handlers;
using SnapDog2.Server.Features.Snapcast.Queries;

/// <summary>
/// API controller for Snapcast operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SnapcastController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SnapcastController> _logger;

    public SnapcastController(IServiceProvider serviceProvider, ILogger<SnapcastController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current Snapcast server status.
    /// </summary>
    /// <returns>The server status including all clients, groups, and streams.</returns>
    [HttpGet("status")]
    [ProducesResponseType<ApiResponse<SnapcastServerStatus>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SnapcastServerStatus>>> GetServerStatus()
    {
        try
        {
            this._logger.LogDebug("Getting Snapcast server status");

            var handler = this._serviceProvider.GetService<GetSnapcastServerStatusQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetSnapcastServerStatusQueryHandler not found in DI container");
                return this.StatusCode(
                    500,
                    ApiResponse<SnapcastServerStatus>.CreateError("HANDLER_ERROR", "Handler not available")
                );
            }

            var query = new GetSnapcastServerStatusQuery();
            var result = await handler.Handle(query, CancellationToken.None);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<SnapcastServerStatus>.CreateSuccess(result.Value!));
            }

            return this.StatusCode(
                500,
                ApiResponse<SnapcastServerStatus>.CreateError("SNAPCAST_ERROR", result.ErrorMessage ?? "Unknown error")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting Snapcast server status");
            return this.StatusCode(
                500,
                ApiResponse<SnapcastServerStatus>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Sets the volume for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">The Snapcast client ID.</param>
    /// <param name="request">The volume request.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("clients/{clientId}/volume")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SetClientVolume(
        string clientId,
        [FromBody] SetVolumeRequest request
    )
    {
        try
        {
            this._logger.LogDebug(
                "Setting volume for Snapcast client {ClientId} to {Volume}%",
                clientId,
                request.Volume
            );

            var handler = this._serviceProvider.GetService<SetSnapcastClientVolumeCommandHandler>();
            if (handler == null)
            {
                this._logger.LogError("SetSnapcastClientVolumeCommandHandler not found in DI container");
                return this.StatusCode(500, ApiResponse<object>.CreateError("HANDLER_ERROR", "Handler not available"));
            }

            var command = new SetSnapcastClientVolumeCommand
            {
                ClientId = clientId,
                Volume = request.Volume,
                Source = CommandSource.Api,
            };

            var result = await handler.Handle(command, CancellationToken.None);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            return this.BadRequest(
                ApiResponse<object>.CreateError("VOLUME_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting client volume for {ClientId}", clientId);
            return this.StatusCode(500, ApiResponse<object>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Sets the mute state for a specific Snapcast client.
    /// </summary>
    /// <param name="clientId">The Snapcast client ID.</param>
    /// <param name="request">The mute request.</param>
    /// <returns>Success or error response.</returns>
    [HttpPost("clients/{clientId}/mute")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SetClientMute(
        string clientId,
        [FromBody] SetMuteRequest request
    )
    {
        try
        {
            this._logger.LogDebug(
                "Setting mute state for Snapcast client {ClientId} to {Muted}",
                clientId,
                request.Muted
            );

            var handler = this._serviceProvider.GetService<SetSnapcastClientVolumeCommandHandler>();
            if (handler == null)
            {
                this._logger.LogError("SetSnapcastClientVolumeCommandHandler not found in DI container");
                return this.StatusCode(500, ApiResponse<object>.CreateError("HANDLER_ERROR", "Handler not available"));
            }

            // For now, we'll implement mute by setting volume to 0
            // In a full implementation, you'd create a separate SetSnapcastClientMuteCommand
            var volume = request.Muted ? 0 : 50; // Default to 50% when unmuting

            var command = new SetSnapcastClientVolumeCommand
            {
                ClientId = clientId,
                Volume = volume,
                Source = CommandSource.Api,
            };

            var result = await handler.Handle(command, CancellationToken.None);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            return this.BadRequest(
                ApiResponse<object>.CreateError("MUTE_ERROR", result.ErrorMessage ?? "Failed to set mute state")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting client mute for {ClientId}", clientId);
            return this.StatusCode(500, ApiResponse<object>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }
}

/// <summary>
/// Request model for setting volume.
/// </summary>
public record SetVolumeRequest
{
    /// <summary>
    /// Volume percentage (0-100).
    /// </summary>
    public required int Volume { get; init; }
}

/// <summary>
/// Request model for setting mute state.
/// </summary>
public record SetMuteRequest
{
    /// <summary>
    /// Whether the client should be muted.
    /// </summary>
    public required bool Muted { get; init; }
}
