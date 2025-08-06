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
public partial class SnapcastController : ControllerBase
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
            this.LogGettingSnapcastServerStatus();

            var handler = this._serviceProvider.GetService<GetSnapcastServerStatusQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound(nameof(GetSnapcastServerStatusQueryHandler));
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
            this.LogErrorGettingSnapcastServerStatus(ex);
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
            this.LogSettingClientVolume(clientId, request.Volume);

            var handler = this._serviceProvider.GetService<SetSnapcastClientVolumeCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound(nameof(SetSnapcastClientVolumeCommandHandler));
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
            this.LogErrorSettingClientVolume(clientId, ex);
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
            this.LogSettingClientMute(clientId, request.Muted);

            var handler = this._serviceProvider.GetService<SetSnapcastClientVolumeCommandHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound(
                    "SetSnapcastClientVolumeCommandHandler (used for mute - architectural bug!)"
                );
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
            this.LogErrorSettingClientMute(clientId, ex);
            return this.StatusCode(500, ApiResponse<object>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    #region Logging

    // ARCHITECTURAL PROBLEM - This should never happen in production
    [LoggerMessage(
        2201,
        LogLevel.Critical,
        "🚨 CRITICAL: Handler {HandlerType} not found in DI container - This is a configuration BUG!"
    )]
    private partial void LogCriticalHandlerNotFound(string handlerType);

    // Snapcast Server Status (2210-2219)
    [LoggerMessage(2210, LogLevel.Debug, "Getting Snapcast server status")]
    private partial void LogGettingSnapcastServerStatus();

    [LoggerMessage(2211, LogLevel.Error, "Error getting Snapcast server status")]
    private partial void LogErrorGettingSnapcastServerStatus(Exception exception);

    // Client Volume Control (2220-2229)
    [LoggerMessage(2220, LogLevel.Debug, "Setting volume for Snapcast client {ClientId} to {Volume}%")]
    private partial void LogSettingClientVolume(string clientId, int volume);

    [LoggerMessage(2221, LogLevel.Error, "Error setting client volume for {ClientId}")]
    private partial void LogErrorSettingClientVolume(string clientId, Exception exception);

    // Client Mute Control (2230-2239)
    [LoggerMessage(2230, LogLevel.Debug, "Setting mute state for Snapcast client {ClientId} to {Muted}")]
    private partial void LogSettingClientMute(string clientId, bool muted);

    [LoggerMessage(2231, LogLevel.Error, "Error setting client mute for {ClientId}")]
    private partial void LogErrorSettingClientMute(string clientId, Exception exception);

    #endregion
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
