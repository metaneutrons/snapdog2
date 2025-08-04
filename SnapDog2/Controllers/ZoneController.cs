namespace SnapDog2.Controllers;

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Shared.Notifications;
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for zone management operations.
/// Follows CQRS pattern using Cortex.Mediator for enterprise-grade architecture compliance.
/// </summary>
[ApiController]
[Route("api/zones")]
[Produces("application/json")]
public class ZoneController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ZoneController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneController"/> class.
    /// </summary>
    /// <param name="mediator">The Cortex.Mediator instance for CQRS command/query dispatch.</param>
    /// <param name="logger">The logger instance.</param>
    public ZoneController(IMediator mediator, ILogger<ZoneController> logger)
    {
        this._mediator = mediator;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current state of a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The zone state wrapped in ApiResponse.</returns>
    [HttpGet("{zoneId:int}/state")]
    [ProducesResponseType(typeof(ApiResponse<ZoneState>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ZoneState>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ZoneState>), 500)]
    public async Task<ActionResult<ApiResponse<ZoneState>>> GetZoneState(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting zone state for zone {ZoneId} via CQRS mediator via CQRS mediator", zoneId);

            var query = new GetZoneStateQuery { ZoneId = zoneId };
            var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ZoneState>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get zone state for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<ZoneState>.CreateError("ZONE_NOT_FOUND", result.ErrorMessage ?? "Zone not found")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting zone state for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse<ZoneState>.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Gets the states of all zones.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of zone states wrapped in ApiResponse.</returns>
    [HttpGet("states")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ZoneState>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ZoneState>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ZoneState>>>> GetAllZoneStates(
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting all zone states via CQRS mediator");

            var query = new GetAllZoneStatesQuery();
            var result = await this._mediator.SendQueryAsync<GetAllZoneStatesQuery, Result<IEnumerable<ZoneState>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ZoneState>>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get all zone states: {Error}", result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ZoneState>>.CreateError(
                    "ZONE_STATES_ERROR",
                    result.ErrorMessage ?? "Failed to get zone states"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting all zone states");
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ZoneState>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Starts or resumes playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation wrapped in ApiResponse.</returns>
    [HttpPost("{zoneId:int}/play")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> Play(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Starting playback for zone {ZoneId} via CQRS mediator via CQRS mediator", zoneId);

            var command = new PlayCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<PlayCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning("Failed to play zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("PLAY_ERROR", result.ErrorMessage ?? "Failed to start playback")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error starting playback for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Pauses playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation wrapped in ApiResponse.</returns>
    [HttpPost("{zoneId:int}/pause")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> Pause(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Pausing playback for zone {ZoneId} via CQRS mediator via CQRS mediator", zoneId);

            var command = new PauseCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<PauseCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning("Failed to pause zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("PAUSE_ERROR", result.ErrorMessage ?? "Failed to pause playback")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error pausing playback for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Sets the volume for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The volume request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation wrapped in ApiResponse.</returns>
    [HttpPost("{zoneId:int}/volume")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetVolume(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] VolumeRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug(
                "Setting volume for zone {ZoneId} to {Volume} via CQRS mediator",
                zoneId,
                request.Volume
            );

            var command = new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = request.Volume,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning("Failed to set volume for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("VOLUME_SET_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting volume for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Stops playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation wrapped in ApiResponse.</returns>
    [HttpPost("{zoneId:int}/stop")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> Stop(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Stopping playback for zone {ZoneId} via CQRS mediator via CQRS mediator", zoneId);

            var command = new StopCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<StopCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning("Failed to stop zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("STOP_ERROR", result.ErrorMessage ?? "Failed to stop playback")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error stopping playback for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Plays the next track in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation wrapped in ApiResponse.</returns>
    [HttpPost("{zoneId:int}/next-track")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> NextTrack(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Playing next track for zone {ZoneId} via CQRS mediator via CQRS mediator", zoneId);

            var command = new NextTrackCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<NextTrackCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to play next track for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("NEXT_TRACK_ERROR", result.ErrorMessage ?? "Failed to play next track")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error playing next track for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Plays the previous track in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/previous-track")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> PreviousTrack(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Playing previous track for zone {ZoneId} via CQRS mediator", zoneId);
            var command = new PreviousTrackCommand { ZoneId = zoneId, Source = CommandSource.Api };

            var result = await this._mediator.SendCommandAsync<PreviousTrackCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to play previous track for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to play previous track")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error playing previous track for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Sets track repeat mode for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The repeat request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/track-repeat")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetTrackRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] RepeatRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Setting track repeat for zone {ZoneId} to {Enabled}", zoneId, request.Enabled);
            var command = new SetTrackRepeatCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetTrackRepeatCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to set track repeat for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to set track repeat")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting track repeat for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Toggles track repeat mode for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/toggle-track-repeat")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> ToggleTrackRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Toggling track repeat for zone {ZoneId} via CQRS mediator", zoneId);
            var command = new ToggleTrackRepeatCommand { ZoneId = zoneId, Source = CommandSource.Api };

            var result = await this._mediator.SendCommandAsync<ToggleTrackRepeatCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to toggle track repeat for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to toggle track repeat")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error toggling track repeat for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Sets playlist shuffle mode for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The shuffle request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/playlist-shuffle")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetPlaylistShuffle(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] ShuffleRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Setting playlist shuffle for zone {ZoneId} to {Enabled}", zoneId, request.Enabled);
            var command = new SetPlaylistShuffleCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetPlaylistShuffleCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to set playlist shuffle for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to set playlist shuffle")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting playlist shuffle for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Sets playlist repeat mode for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The repeat request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/playlist-repeat")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetPlaylistRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] RepeatRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Setting playlist repeat for zone {ZoneId} to {Enabled}", zoneId, request.Enabled);
            var command = new SetPlaylistRepeatCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = CommandSource.Api,
            };

            var result = await this._mediator.SendCommandAsync<SetPlaylistRepeatCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this._logger.LogWarning(
                "Failed to set playlist repeat for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to set playlist repeat")
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error setting playlist repeat for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Gets all zones with their states.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of all zone states wrapped in ApiResponse.</returns>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ZoneState>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ZoneState>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ZoneState>>>> GetAllZones(
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting all zones via CQRS mediator");
            var result = await this._mediator.SendQueryAsync<GetAllZonesQuery, Result<List<ZoneState>>>(
                new GetAllZonesQuery(),
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ZoneState>>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get all zones: {Error}", result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ZoneState>>.CreateError(
                    "ZONES_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve zones"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting all zones");
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<ZoneState>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Gets the current track information for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current track information wrapped in ApiResponse.</returns>
    [HttpGet("{zoneId:int}/track")]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), 404)]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), 500)]
    public async Task<ActionResult<ApiResponse<TrackInfo>>> GetZoneTrackInfo(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting track info for zone {ZoneId} via CQRS mediator", zoneId);
            var result = await this._mediator.SendQueryAsync<GetZoneTrackInfoQuery, Result<TrackInfo>>(
                new GetZoneTrackInfoQuery { ZoneId = zoneId },
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<TrackInfo>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning("Failed to get track info for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<TrackInfo>.CreateError(
                    "TRACK_INFO_NOT_FOUND",
                    result.ErrorMessage ?? "Track information not found"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting track info for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse<TrackInfo>.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Gets the current playlist information for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current playlist information wrapped in ApiResponse.</returns>
    [HttpGet("{zoneId:int}/playlist")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PlaylistInfo>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PlaylistInfo>), 500)]
    public async Task<ActionResult<ApiResponse<PlaylistInfo>>> GetZonePlaylistInfo(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting playlist info for zone {ZoneId} via CQRS mediator", zoneId);
            var result = await this._mediator.SendQueryAsync<GetZonePlaylistInfoQuery, Result<PlaylistInfo>>(
                new GetZonePlaylistInfoQuery { ZoneId = zoneId },
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<PlaylistInfo>.CreateSuccess(result.Value));
            }

            this._logger.LogWarning(
                "Failed to get playlist info for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return this.NotFound(
                ApiResponse<PlaylistInfo>.CreateError(
                    "PLAYLIST_INFO_NOT_FOUND",
                    result.ErrorMessage ?? "Playlist information not found"
                )
            );
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting playlist info for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistInfo>.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    /// <summary>
    /// Test endpoint to publish a zone volume change notification.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="volume">The new volume level.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success message wrapped in ApiResponse.</returns>
    [HttpPost("{zoneId:int}/test-notification")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> TestZoneNotification(
        [Range(1, int.MaxValue)] int zoneId,
        [FromQuery] [Range(0, 100)] int volume = 75,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            this._logger.LogDebug(
                "Publishing test zone volume notification for Zone {ZoneId} with volume {Volume} via CQRS mediator",
                zoneId,
                volume
            );
            var notification = new ZoneVolumeChangedNotification { ZoneId = zoneId, Volume = volume };

            await this._mediator.PublishAsync(notification, cancellationToken);

            return this.Ok(ApiResponse.CreateSuccess());
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error publishing test notification for zone {ZoneId}", zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }
}

// Request DTOs
public record VolumeRequest
{
    [Range(0, 100)]
    public required int Volume { get; init; }
}

public record RepeatRequest
{
    public required bool Enabled { get; init; }
}

public record ShuffleRequest
{
    public required bool Enabled { get; init; }
}
