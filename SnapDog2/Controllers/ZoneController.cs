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
public partial class ZoneController : ControllerBase
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
            this.LogGettingZoneState(zoneId);

            var query = new GetZoneStateQuery { ZoneId = zoneId };
            var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ZoneState>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetZoneState(zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<ZoneState>.CreateError("ZONE_NOT_FOUND", result.ErrorMessage ?? "Zone not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingZoneState(ex, zoneId);
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
            this.LogGettingAllZoneStates();

            var query = new GetAllZoneStatesQuery();
            var result = await this._mediator.SendQueryAsync<GetAllZoneStatesQuery, Result<IEnumerable<ZoneState>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ZoneState>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetAllZoneStates(result.ErrorMessage);
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
            this.LogErrorGettingAllZoneStates(ex);
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
            this.LogStartingPlayback(zoneId);

            var command = new PlayCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<PlayCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPlayZone(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("PLAY_ERROR", result.ErrorMessage ?? "Failed to start playback")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorStartingPlayback(ex, zoneId);
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
            this.LogPausingPlayback(zoneId);

            var command = new PauseCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<PauseCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPauseZone(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("PAUSE_ERROR", result.ErrorMessage ?? "Failed to pause playback")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorPausingPlayback(ex, zoneId);
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
            this.LogSettingVolume(zoneId, request.Volume);

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

            this.LogFailedToSetVolume(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("VOLUME_SET_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingVolume(ex, zoneId);
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
            this.LogStoppingPlayback(zoneId);

            var command = new StopCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<StopCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToStopZone(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("STOP_ERROR", result.ErrorMessage ?? "Failed to stop playback")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingPlayback(ex, zoneId);
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
            this.LogPlayingNextTrack(zoneId);

            var command = new NextTrackCommand { ZoneId = zoneId, Source = CommandSource.Api };
            var result = await this._mediator.SendCommandAsync<NextTrackCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPlayNextTrack(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("NEXT_TRACK_ERROR", result.ErrorMessage ?? "Failed to play next track")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorPlayingNextTrack(ex, zoneId);
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
            this.LogPlayingPreviousTrack(zoneId);
            var command = new PreviousTrackCommand { ZoneId = zoneId, Source = CommandSource.Api };

            var result = await this._mediator.SendCommandAsync<PreviousTrackCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPlayPreviousTrack(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to play previous track")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorPlayingPreviousTrack(ex, zoneId);
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
            this.LogSettingTrackRepeat(zoneId, request.Enabled);
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

            this.LogFailedToSetTrackRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to set track repeat")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingTrackRepeat(ex, zoneId);
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
            this.LogTogglingTrackRepeat(zoneId);
            var command = new ToggleTrackRepeatCommand { ZoneId = zoneId, Source = CommandSource.Api };

            var result = await this._mediator.SendCommandAsync<ToggleTrackRepeatCommand, Result>(
                command,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse.CreateSuccess());
            }

            this.LogFailedToToggleTrackRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to toggle track repeat")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingTrackRepeat(ex, zoneId);
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
            this.LogSettingPlaylistShuffle(zoneId, request.Enabled);
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

            this.LogFailedToSetPlaylistShuffle(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to set playlist shuffle")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingPlaylistShuffle(ex, zoneId);
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
            this.LogSettingPlaylistRepeat(zoneId, request.Enabled);
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

            this.LogFailedToSetPlaylistRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("OPERATION_ERROR", result.ErrorMessage ?? "Failed to set playlist repeat")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingPlaylistRepeat(ex, zoneId);
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
            this.LogGettingAllZones();
            var result = await this._mediator.SendQueryAsync<GetAllZonesQuery, Result<List<ZoneState>>>(
                new GetAllZonesQuery(),
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<ZoneState>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetAllZones(result.ErrorMessage);
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
            this.LogErrorGettingAllZones(ex);
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
            this.LogGettingTrackInfo(zoneId);
            var result = await this._mediator.SendQueryAsync<GetZoneTrackInfoQuery, Result<TrackInfo>>(
                new GetZoneTrackInfoQuery { ZoneId = zoneId },
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<TrackInfo>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetTrackInfo(zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<TrackInfo>.CreateError(
                    "TRACK_INFO_NOT_FOUND",
                    result.ErrorMessage ?? "Track information not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingTrackInfo(ex, zoneId);
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
            this.LogGettingPlaylistInfo(zoneId);
            var result = await this._mediator.SendQueryAsync<GetZonePlaylistInfoQuery, Result<PlaylistInfo>>(
                new GetZonePlaylistInfoQuery { ZoneId = zoneId },
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<PlaylistInfo>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetPlaylistInfo(zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<PlaylistInfo>.CreateError(
                    "PLAYLIST_INFO_NOT_FOUND",
                    result.ErrorMessage ?? "Playlist information not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingPlaylistInfo(ex, zoneId);
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
            this.LogPublishingTestNotification(zoneId, volume);
            var notification = new ZoneVolumeChangedNotification { ZoneId = zoneId, Volume = volume };

            await this._mediator.PublishAsync(notification, cancellationToken);

            return this.Ok(ApiResponse.CreateSuccess());
        }
        catch (Exception ex)
        {
            this.LogErrorPublishingTestNotification(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse.CreateError("INTERNAL_ERROR", "An internal server error occurred", ex.Message)
            );
        }
    }

    // LoggerMessage definitions for high-performance logging
    [LoggerMessage(
        EventId = 2630,
        Level = LogLevel.Debug,
        Message = "Getting zone state for zone {zoneId} via CQRS mediator via CQRS mediator"
    )]
    private partial void LogGettingZoneState(int zoneId);

    [LoggerMessage(
        EventId = 2631,
        Level = LogLevel.Warning,
        Message = "Failed to get zone state for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToGetZoneState(int zoneId, string? error);

    [LoggerMessage(EventId = 2632, Level = LogLevel.Error, Message = "Error getting zone state for zone {zoneId}")]
    private partial void LogErrorGettingZoneState(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2633, Level = LogLevel.Debug, Message = "Getting all zone states via CQRS mediator")]
    private partial void LogGettingAllZoneStates();

    [LoggerMessage(EventId = 2634, Level = LogLevel.Warning, Message = "Failed to get all zone states: {error}")]
    private partial void LogFailedToGetAllZoneStates(string? error);

    [LoggerMessage(EventId = 2635, Level = LogLevel.Error, Message = "Error getting all zone states")]
    private partial void LogErrorGettingAllZoneStates(Exception ex);

    [LoggerMessage(
        EventId = 2636,
        Level = LogLevel.Debug,
        Message = "Starting playback for zone {zoneId} via CQRS mediator via CQRS mediator"
    )]
    private partial void LogStartingPlayback(int zoneId);

    [LoggerMessage(EventId = 2637, Level = LogLevel.Warning, Message = "Failed to play zone {zoneId}: {error}")]
    private partial void LogFailedToPlayZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2638, Level = LogLevel.Error, Message = "Error starting playback for zone {zoneId}")]
    private partial void LogErrorStartingPlayback(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2639,
        Level = LogLevel.Debug,
        Message = "Pausing playback for zone {zoneId} via CQRS mediator via CQRS mediator"
    )]
    private partial void LogPausingPlayback(int zoneId);

    [LoggerMessage(EventId = 2640, Level = LogLevel.Warning, Message = "Failed to pause zone {zoneId}: {error}")]
    private partial void LogFailedToPauseZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2641, Level = LogLevel.Error, Message = "Error pausing playback for zone {zoneId}")]
    private partial void LogErrorPausingPlayback(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2642,
        Level = LogLevel.Debug,
        Message = "Setting volume for zone {zoneId} to {volume} via CQRS mediator"
    )]
    private partial void LogSettingVolume(int zoneId, int volume);

    [LoggerMessage(
        EventId = 2643,
        Level = LogLevel.Warning,
        Message = "Failed to set volume for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetVolume(int zoneId, string? error);

    [LoggerMessage(EventId = 2644, Level = LogLevel.Error, Message = "Error setting volume for zone {zoneId}")]
    private partial void LogErrorSettingVolume(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2645,
        Level = LogLevel.Debug,
        Message = "Stopping playback for zone {zoneId} via CQRS mediator via CQRS mediator"
    )]
    private partial void LogStoppingPlayback(int zoneId);

    [LoggerMessage(EventId = 2646, Level = LogLevel.Warning, Message = "Failed to stop zone {zoneId}: {error}")]
    private partial void LogFailedToStopZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2647, Level = LogLevel.Error, Message = "Error stopping playback for zone {zoneId}")]
    private partial void LogErrorStoppingPlayback(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2648,
        Level = LogLevel.Debug,
        Message = "Playing next track for zone {zoneId} via CQRS mediator via CQRS mediator"
    )]
    private partial void LogPlayingNextTrack(int zoneId);

    [LoggerMessage(
        EventId = 2649,
        Level = LogLevel.Warning,
        Message = "Failed to play next track for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToPlayNextTrack(int zoneId, string? error);

    [LoggerMessage(EventId = 2650, Level = LogLevel.Error, Message = "Error playing next track for zone {zoneId}")]
    private partial void LogErrorPlayingNextTrack(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2651,
        Level = LogLevel.Debug,
        Message = "Playing previous track for zone {zoneId} via CQRS mediator"
    )]
    private partial void LogPlayingPreviousTrack(int zoneId);

    [LoggerMessage(
        EventId = 2652,
        Level = LogLevel.Warning,
        Message = "Failed to play previous track for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToPlayPreviousTrack(int zoneId, string? error);

    [LoggerMessage(EventId = 2653, Level = LogLevel.Error, Message = "Error playing previous track for zone {zoneId}")]
    private partial void LogErrorPlayingPreviousTrack(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2654,
        Level = LogLevel.Debug,
        Message = "Setting track repeat for zone {zoneId} to {enabled}"
    )]
    private partial void LogSettingTrackRepeat(int zoneId, bool enabled);

    [LoggerMessage(
        EventId = 2655,
        Level = LogLevel.Warning,
        Message = "Failed to set track repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetTrackRepeat(int zoneId, string? error);

    [LoggerMessage(EventId = 2656, Level = LogLevel.Error, Message = "Error setting track repeat for zone {zoneId}")]
    private partial void LogErrorSettingTrackRepeat(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2657,
        Level = LogLevel.Debug,
        Message = "Toggling track repeat for zone {zoneId} via CQRS mediator"
    )]
    private partial void LogTogglingTrackRepeat(int zoneId);

    [LoggerMessage(
        EventId = 2658,
        Level = LogLevel.Warning,
        Message = "Failed to toggle track repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToToggleTrackRepeat(int zoneId, string? error);

    [LoggerMessage(EventId = 2659, Level = LogLevel.Error, Message = "Error toggling track repeat for zone {zoneId}")]
    private partial void LogErrorTogglingTrackRepeat(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2660,
        Level = LogLevel.Debug,
        Message = "Setting playlist shuffle for zone {zoneId} to {enabled}"
    )]
    private partial void LogSettingPlaylistShuffle(int zoneId, bool enabled);

    [LoggerMessage(
        EventId = 2661,
        Level = LogLevel.Warning,
        Message = "Failed to set playlist shuffle for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetPlaylistShuffle(int zoneId, string? error);

    [LoggerMessage(
        EventId = 2662,
        Level = LogLevel.Error,
        Message = "Error setting playlist shuffle for zone {zoneId}"
    )]
    private partial void LogErrorSettingPlaylistShuffle(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2663,
        Level = LogLevel.Debug,
        Message = "Setting playlist repeat for zone {zoneId} to {enabled}"
    )]
    private partial void LogSettingPlaylistRepeat(int zoneId, bool enabled);

    [LoggerMessage(
        EventId = 2664,
        Level = LogLevel.Warning,
        Message = "Failed to set playlist repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetPlaylistRepeat(int zoneId, string? error);

    [LoggerMessage(EventId = 2665, Level = LogLevel.Error, Message = "Error setting playlist repeat for zone {zoneId}")]
    private partial void LogErrorSettingPlaylistRepeat(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2666, Level = LogLevel.Debug, Message = "Getting all zones via CQRS mediator")]
    private partial void LogGettingAllZones();

    [LoggerMessage(EventId = 2667, Level = LogLevel.Warning, Message = "Failed to get all zones: {error}")]
    private partial void LogFailedToGetAllZones(string? error);

    [LoggerMessage(EventId = 2668, Level = LogLevel.Error, Message = "Error getting all zones")]
    private partial void LogErrorGettingAllZones(Exception ex);

    [LoggerMessage(
        EventId = 2669,
        Level = LogLevel.Debug,
        Message = "Getting track info for zone {zoneId} via CQRS mediator"
    )]
    private partial void LogGettingTrackInfo(int zoneId);

    [LoggerMessage(
        EventId = 2670,
        Level = LogLevel.Warning,
        Message = "Failed to get track info for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToGetTrackInfo(int zoneId, string? error);

    [LoggerMessage(EventId = 2671, Level = LogLevel.Error, Message = "Error getting track info for zone {zoneId}")]
    private partial void LogErrorGettingTrackInfo(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2672,
        Level = LogLevel.Debug,
        Message = "Getting playlist info for zone {zoneId} via CQRS mediator"
    )]
    private partial void LogGettingPlaylistInfo(int zoneId);

    [LoggerMessage(
        EventId = 2673,
        Level = LogLevel.Warning,
        Message = "Failed to get playlist info for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToGetPlaylistInfo(int zoneId, string? error);

    [LoggerMessage(EventId = 2674, Level = LogLevel.Error, Message = "Error getting playlist info for zone {zoneId}")]
    private partial void LogErrorGettingPlaylistInfo(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2675,
        Level = LogLevel.Debug,
        Message = "Publishing test zone volume notification for Zone {zoneId} with volume {volume} via CQRS mediator"
    )]
    private partial void LogPublishingTestNotification(int zoneId, int volume);

    [LoggerMessage(
        EventId = 2676,
        Level = LogLevel.Error,
        Message = "Error publishing test notification for zone {zoneId}"
    )]
    private partial void LogErrorPublishingTestNotification(Exception ex, int zoneId);
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
