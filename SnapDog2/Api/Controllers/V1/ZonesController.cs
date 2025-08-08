namespace SnapDog2.Api.Controllers.V1;

using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for zone management operations (API v1).
/// </summary>
[ApiController]
[Route("api/v1/zones")]
[Authorize]
[Produces("application/json")]
public partial class ZonesController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZonesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZonesController"/> class.
    /// </summary>
    public ZonesController(IServiceProvider serviceProvider, ILogger<ZonesController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Lists configured zones.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="sortBy">Sort field (default: name).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of zones.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ZoneInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ZoneInfo>>>> GetZones(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            this.LogGettingZonesList(page, pageSize, sortBy);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetAllZonesQueryHandler>();
            if (handler == null)
            {
                this.LogGetAllZonesQueryHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<PaginatedResponse<ZoneInfo>>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Zone handler not available"
                    )
                );
            }

            var result = await handler.Handle(new GetAllZonesQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var zones = result.Value.Select(z => new ZoneInfo(z.Id, z.Name, z.PlaybackState)).ToList();

                // Apply pagination
                var totalItems = zones.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var skip = (page - 1) * pageSize;
                var pagedZones = zones.Skip(skip).Take(pageSize).ToList();

                var paginatedResponse = new PaginatedResponse<ZoneInfo>
                {
                    Items = pagedZones,
                    Pagination = new PaginationMetadata
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                    },
                };

                return this.Ok(ApiResponse<PaginatedResponse<ZoneInfo>>.CreateSuccess(paginatedResponse));
            }

            this.LogFailedToGetZones(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<ZoneInfo>>.CreateError(
                    "ZONES_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve zones"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingZones(ex);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<ZoneInfo>>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Gets details and full state for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Zone state information.</returns>
    [HttpGet("{zoneId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ZoneState>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ZoneState>>> GetZone(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingZone(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
            if (handler == null)
            {
                this.LogGetZoneStateQueryHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<ZoneState>.CreateError("HANDLER_NOT_FOUND", "Zone state handler not available")
                );
            }

            var result = await handler.Handle(new GetZoneStateQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<ZoneState>.CreateSuccess(result.Value));
            }

            this.LogZoneNotFound(zoneId);
            return this.NotFound(ApiResponse<ZoneState>.CreateError("ZONE_NOT_FOUND", $"Zone {zoneId} not found"));
        }
        catch (Exception ex)
        {
            this.LogErrorGettingZone(ex, zoneId);
            return this.StatusCode(500, ApiResponse<ZoneState>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets full state JSON for a zone (alias for GET /{zoneId}).
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Zone state information.</returns>
    [HttpGet("{zoneId:int}/state")]
    [ProducesResponseType(typeof(ApiResponse<ZoneState>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ZoneState>>> GetZoneState(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        return await this.GetZone(zoneId, cancellationToken);
    }

    /// <summary>
    /// Starts or resumes playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">Optional play request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPost("{zoneId:int}/commands/play")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> PlayZone(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] PlayRequest? request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogPlayingZone(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.PlayCommandHandler>();
            if (handler == null)
            {
                this.LogPlayCommandHandlerNotFound();
                return this.StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Play handler not available"));
            }

            var command = new PlayCommand
            {
                ZoneId = zoneId,
                MediaUrl = request?.MediaUrl,
                TrackIndex = request?.TrackIndex,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPlayZone(zoneId, result.ErrorMessage);
            return this.BadRequest(ApiResponse.CreateError("PLAY_ERROR", result.ErrorMessage ?? "Failed to play zone"));
        }
        catch (Exception ex)
        {
            this.LogErrorPlayingZone(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Pauses playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPost("{zoneId:int}/commands/pause")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> PauseZone(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogPausingZone(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.PauseCommandHandler>();
            if (handler == null)
            {
                this.LogPauseCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Pause handler not available")
                );
            }

            var command = new PauseCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPauseZone(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("PAUSE_ERROR", result.ErrorMessage ?? "Failed to pause zone")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorPausingZone(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Stops playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPost("{zoneId:int}/commands/stop")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> StopZone(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogStoppingZone(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.StopCommandHandler>();
            if (handler == null)
            {
                this.LogStopCommandHandlerNotFound();
                return this.StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Stop handler not available"));
            }

            var command = new StopCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToStopZone(zoneId, result.ErrorMessage);
            return this.BadRequest(ApiResponse.CreateError("STOP_ERROR", result.ErrorMessage ?? "Failed to stop zone"));
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingZone(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Plays the next track in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPost("{zoneId:int}/commands/next_track")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> NextTrack(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogNextTrack(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.NextTrackCommandHandler>();
            if (handler == null)
            {
                this.LogNextTrackCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Next track handler not available")
                );
            }

            var command = new NextTrackCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToNextTrack(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("NEXT_TRACK_ERROR", result.ErrorMessage ?? "Failed to go to next track")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorNextTrack(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Plays the previous track in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPost("{zoneId:int}/commands/prev_track")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> PreviousTrack(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogPreviousTrack(zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.PreviousTrackCommandHandler>();
            if (handler == null)
            {
                this.LogPreviousTrackCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Previous track handler not available")
                );
            }

            var command = new PreviousTrackCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToPreviousTrack(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("PREVIOUS_TRACK_ERROR", result.ErrorMessage ?? "Failed to go to previous track")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorPreviousTrack(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Sets track by 1-based index.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The track request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPut("{zoneId:int}/track")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetTrack(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] SetTrackRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingTrack(request.Index, zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetTrackCommandHandler>();
            if (handler == null)
            {
                this.LogSetTrackCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Set track handler not available")
                );
            }

            var command = new SetTrackCommand
            {
                ZoneId = zoneId,
                TrackIndex = request.Index,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToSetTrack(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("SET_TRACK_ERROR", result.ErrorMessage ?? "Failed to set track")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingTrack(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Sets playlist by 1-based index or ID.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The playlist request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPut("{zoneId:int}/playlist")]
    [ProducesResponseType(typeof(ApiResponse), 202)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SetPlaylist(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] SetPlaylistRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingPlaylist(zoneId, request.Id, request.Index);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetPlaylistCommandHandler>();
            if (handler == null)
            {
                this.LogSetPlaylistCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Set playlist handler not available")
                );
            }

            var command = new SetPlaylistCommand
            {
                ZoneId = zoneId,
                PlaylistId = request.Id,
                PlaylistIndex = request.Index,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Accepted(ApiResponse.CreateSuccess());
            }

            this.LogFailedToSetPlaylist(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse.CreateError("SET_PLAYLIST_ERROR", result.ErrorMessage ?? "Failed to set playlist")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingPlaylist(ex, zoneId);
            return this.StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    // VOLUME SETTINGS ENDPOINTS

    /// <summary>
    /// Sets zone volume.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The volume request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Volume response.</returns>
    [HttpPut("{zoneId:int}/settings/volume")]
    [ProducesResponseType(typeof(ApiResponse<VolumeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> SetZoneVolume(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] VolumeSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingVolume(request.Level, zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (handler == null)
            {
                this.LogSetZoneVolumeCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume handler not available")
                );
            }

            var command = new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = request.Level,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(request.Level)));
            }

            this.LogFailedToSetVolume(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<VolumeResponse>.CreateError("VOLUME_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingVolume(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Gets current zone volume.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Volume response.</returns>
    [HttpGet("{zoneId:int}/settings/volume")]
    [ProducesResponseType(typeof(ApiResponse<VolumeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> GetZoneVolume(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingVolume(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
            if (handler == null)
            {
                this.LogGetZoneVolumeQueryHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume query handler not available")
                );
            }

            var result = await handler.Handle(new GetZoneVolumeQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(result.Value)));
            }

            this.LogFailedToGetVolume(zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<VolumeResponse>.CreateError("ZONE_NOT_FOUND", result.ErrorMessage ?? "Zone not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingVolume(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Increases zone volume.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">Optional step request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New volume response.</returns>
    [HttpPost("{zoneId:int}/settings/volume/up")]
    [ProducesResponseType(typeof(ApiResponse<VolumeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> VolumeUp(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] StepRequest? request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var step = request?.Step ?? 5;
            this.LogIncreasingVolume(step, zoneId);

            // First get current volume
            var volumeHandler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
            if (volumeHandler == null)
            {
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume query handler not available")
                );
            }

            var currentVolumeResult = await volumeHandler.Handle(
                new GetZoneVolumeQuery { ZoneId = zoneId },
                cancellationToken
            );
            if (currentVolumeResult.IsFailure)
            {
                return this.NotFound(ApiResponse<VolumeResponse>.CreateError("ZONE_NOT_FOUND", "Zone not found"));
            }

            var newVolume = Math.Min(100, currentVolumeResult.Value + step);

            // Set new volume
            var setHandler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (setHandler == null)
            {
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume handler not available")
                );
            }

            var command = new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = newVolume,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await setHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(newVolume)));
            }

            return this.BadRequest(
                ApiResponse<VolumeResponse>.CreateError(
                    "VOLUME_ERROR",
                    result.ErrorMessage ?? "Failed to increase volume"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorIncreasingVolume(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Decreases zone volume.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">Optional step request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New volume response.</returns>
    [HttpPost("{zoneId:int}/settings/volume/down")]
    [ProducesResponseType(typeof(ApiResponse<VolumeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<VolumeResponse>>> VolumeDown(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] StepRequest? request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var step = request?.Step ?? 5;
            this.LogDecreasingVolume(step, zoneId);

            // First get current volume
            var volumeHandler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
            if (volumeHandler == null)
            {
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume query handler not available")
                );
            }

            var currentVolumeResult = await volumeHandler.Handle(
                new GetZoneVolumeQuery { ZoneId = zoneId },
                cancellationToken
            );
            if (currentVolumeResult.IsFailure)
            {
                return this.NotFound(ApiResponse<VolumeResponse>.CreateError("ZONE_NOT_FOUND", "Zone not found"));
            }

            var newVolume = Math.Max(0, currentVolumeResult.Value - step);

            // Set new volume
            var setHandler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (setHandler == null)
            {
                return this.StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume handler not available")
                );
            }

            var command = new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = newVolume,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await setHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(newVolume)));
            }

            return this.BadRequest(
                ApiResponse<VolumeResponse>.CreateError(
                    "VOLUME_ERROR",
                    result.ErrorMessage ?? "Failed to decrease volume"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorDecreasingVolume(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    // MUTE SETTINGS ENDPOINTS

    /// <summary>
    /// Sets zone mute state.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The mute request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Mute response.</returns>
    [HttpPut("{zoneId:int}/settings/mute")]
    [ProducesResponseType(typeof(ApiResponse<MuteResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<MuteResponse>>> SetZoneMute(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] MuteSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingMute(request.Enabled, zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetZoneMuteCommandHandler>();
            if (handler == null)
            {
                this.LogSetZoneMuteCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Mute handler not available")
                );
            }

            var command = new SetZoneMuteCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(request.Enabled)));
            }

            this.LogFailedToSetMute(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<MuteResponse>.CreateError("MUTE_ERROR", result.ErrorMessage ?? "Failed to set mute")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingMute(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Gets current zone mute state.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Mute response.</returns>
    [HttpGet("{zoneId:int}/settings/mute")]
    [ProducesResponseType(typeof(ApiResponse<MuteResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<MuteResponse>>> GetZoneMute(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingMute(zoneId);

            var handler = this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
            if (handler == null)
            {
                this.LogGetZoneStateQueryHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Zone state handler not available")
                );
            }

            var result = await handler.Handle(new GetZoneStateQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(result.Value.Mute)));
            }

            this.LogFailedToGetMute(zoneId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<MuteResponse>.CreateError("ZONE_NOT_FOUND", result.ErrorMessage ?? "Zone not found")
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingMute(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Toggles zone mute state.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New mute response.</returns>
    [HttpPost("{zoneId:int}/settings/mute/toggle")]
    [ProducesResponseType(typeof(ApiResponse<MuteResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<MuteResponse>>> ToggleZoneMute(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogTogglingMute(zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.ToggleZoneMuteCommandHandler>();
            if (handler == null)
            {
                this.LogToggleZoneMuteCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Toggle mute handler not available")
                );
            }

            var command = new ToggleZoneMuteCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Get the new mute state
                var stateHandler =
                    this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return this.Ok(
                            ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(stateResult.Value.Mute))
                        );
                    }
                }

                // Fallback - assume toggle worked
                return this.Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(true)));
            }

            this.LogFailedToToggleMute(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<MuteResponse>.CreateError(
                    "TOGGLE_MUTE_ERROR",
                    result.ErrorMessage ?? "Failed to toggle mute"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingMute(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    // TRACK REPEAT SETTINGS ENDPOINTS

    /// <summary>
    /// Sets track repeat mode.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The mode request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Track repeat response.</returns>
    [HttpPut("{zoneId:int}/settings/track_repeat")]
    [ProducesResponseType(typeof(ApiResponse<TrackRepeatResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<TrackRepeatResponse>>> SetTrackRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] ModeSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingTrackRepeat(request.Enabled, zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetTrackRepeatCommandHandler>();
            if (handler == null)
            {
                this.LogSetTrackRepeatCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<TrackRepeatResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Track repeat handler not available"
                    )
                );
            }

            var command = new SetTrackRepeatCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(
                    ApiResponse<TrackRepeatResponse>.CreateSuccess(new TrackRepeatResponse(request.Enabled))
                );
            }

            this.LogFailedToSetTrackRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<TrackRepeatResponse>.CreateError(
                    "TRACK_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to set track repeat"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingTrackRepeat(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<TrackRepeatResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Toggles track repeat mode.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New track repeat response.</returns>
    [HttpPost("{zoneId:int}/settings/track_repeat/toggle")]
    [ProducesResponseType(typeof(ApiResponse<TrackRepeatResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<TrackRepeatResponse>>> ToggleTrackRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogTogglingTrackRepeat(zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.ToggleTrackRepeatCommandHandler>();
            if (handler == null)
            {
                this.LogToggleTrackRepeatCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<TrackRepeatResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Toggle track repeat handler not available"
                    )
                );
            }

            var command = new ToggleTrackRepeatCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Get the new state
                var stateHandler =
                    this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return this.Ok(
                            ApiResponse<TrackRepeatResponse>.CreateSuccess(
                                new TrackRepeatResponse(stateResult.Value.TrackRepeat)
                            )
                        );
                    }
                }

                return this.Ok(ApiResponse<TrackRepeatResponse>.CreateSuccess(new TrackRepeatResponse(true)));
            }

            this.LogFailedToToggleTrackRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<TrackRepeatResponse>.CreateError(
                    "TOGGLE_TRACK_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to toggle track repeat"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingTrackRepeat(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<TrackRepeatResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    // PLAYLIST REPEAT SETTINGS ENDPOINTS

    /// <summary>
    /// Sets playlist repeat mode.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The mode request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Playlist repeat response.</returns>
    [HttpPut("{zoneId:int}/settings/playlist_repeat")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistRepeatResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PlaylistRepeatResponse>>> SetPlaylistRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] ModeSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingPlaylistRepeat(request.Enabled, zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetPlaylistRepeatCommandHandler>();
            if (handler == null)
            {
                this.LogSetPlaylistRepeatCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistRepeatResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Playlist repeat handler not available"
                    )
                );
            }

            var command = new SetPlaylistRepeatCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(
                    ApiResponse<PlaylistRepeatResponse>.CreateSuccess(new PlaylistRepeatResponse(request.Enabled))
                );
            }

            this.LogFailedToSetPlaylistRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<PlaylistRepeatResponse>.CreateError(
                    "PLAYLIST_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to set playlist repeat"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingPlaylistRepeat(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistRepeatResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Toggles playlist repeat mode.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New playlist repeat response.</returns>
    [HttpPost("{zoneId:int}/settings/playlist_repeat/toggle")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistRepeatResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PlaylistRepeatResponse>>> TogglePlaylistRepeat(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogTogglingPlaylistRepeat(zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.TogglePlaylistRepeatCommandHandler>();
            if (handler == null)
            {
                this.LogTogglePlaylistRepeatCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistRepeatResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Toggle playlist repeat handler not available"
                    )
                );
            }

            var command = new TogglePlaylistRepeatCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Get the new state
                var stateHandler =
                    this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return this.Ok(
                            ApiResponse<PlaylistRepeatResponse>.CreateSuccess(
                                new PlaylistRepeatResponse(stateResult.Value.PlaylistRepeat)
                            )
                        );
                    }
                }

                return this.Ok(ApiResponse<PlaylistRepeatResponse>.CreateSuccess(new PlaylistRepeatResponse(true)));
            }

            this.LogFailedToTogglePlaylistRepeat(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<PlaylistRepeatResponse>.CreateError(
                    "TOGGLE_PLAYLIST_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to toggle playlist repeat"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingPlaylistRepeat(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistRepeatResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    // PLAYLIST SHUFFLE SETTINGS ENDPOINTS

    /// <summary>
    /// Sets playlist shuffle mode.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The mode request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Playlist shuffle response.</returns>
    [HttpPut("{zoneId:int}/settings/playlist_shuffle")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistShuffleResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PlaylistShuffleResponse>>> SetPlaylistShuffle(
        [Range(1, int.MaxValue)] int zoneId,
        [FromBody] ModeSetRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogSettingPlaylistShuffle(request.Enabled, zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.SetPlaylistShuffleCommandHandler>();
            if (handler == null)
            {
                this.LogSetPlaylistShuffleCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistShuffleResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Playlist shuffle handler not available"
                    )
                );
            }

            var command = new SetPlaylistShuffleCommand
            {
                ZoneId = zoneId,
                Enabled = request.Enabled,
                Source = Core.Enums.CommandSource.Api,
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return this.Ok(
                    ApiResponse<PlaylistShuffleResponse>.CreateSuccess(new PlaylistShuffleResponse(request.Enabled))
                );
            }

            this.LogFailedToSetPlaylistShuffle(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<PlaylistShuffleResponse>.CreateError(
                    "PLAYLIST_SHUFFLE_ERROR",
                    result.ErrorMessage ?? "Failed to set playlist shuffle"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorSettingPlaylistShuffle(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistShuffleResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    /// <summary>
    /// Toggles playlist shuffle mode.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New playlist shuffle response.</returns>
    [HttpPost("{zoneId:int}/settings/playlist_shuffle/toggle")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistShuffleResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PlaylistShuffleResponse>>> TogglePlaylistShuffle(
        [Range(1, int.MaxValue)] int zoneId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogTogglingPlaylistShuffle(zoneId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.TogglePlaylistShuffleCommandHandler>();
            if (handler == null)
            {
                this.LogTogglePlaylistShuffleCommandHandlerNotFound();
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistShuffleResponse>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Toggle playlist shuffle handler not available"
                    )
                );
            }

            var command = new TogglePlaylistShuffleCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Get the new state
                var stateHandler =
                    this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return this.Ok(
                            ApiResponse<PlaylistShuffleResponse>.CreateSuccess(
                                new PlaylistShuffleResponse(stateResult.Value.PlaylistShuffle)
                            )
                        );
                    }
                }

                return this.Ok(ApiResponse<PlaylistShuffleResponse>.CreateSuccess(new PlaylistShuffleResponse(true)));
            }

            this.LogFailedToTogglePlaylistShuffle(zoneId, result.ErrorMessage);
            return this.BadRequest(
                ApiResponse<PlaylistShuffleResponse>.CreateError(
                    "TOGGLE_PLAYLIST_SHUFFLE_ERROR",
                    result.ErrorMessage ?? "Failed to toggle playlist shuffle"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorTogglingPlaylistShuffle(ex, zoneId);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistShuffleResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }

    // LoggerMessage definitions for high-performance logging (ID range: 2809-2892)

    // Handler not found errors - critical architectural issues
    [LoggerMessage(
        EventId = 2809,
        Level = LogLevel.Error,
        Message = "GetAllZonesQueryHandler not found in DI container"
    )]
    private partial void LogGetAllZonesQueryHandlerNotFound();

    [LoggerMessage(
        EventId = 2810,
        Level = LogLevel.Error,
        Message = "GetZoneStateQueryHandler not found in DI container"
    )]
    private partial void LogGetZoneStateQueryHandlerNotFound();

    [LoggerMessage(EventId = 2811, Level = LogLevel.Error, Message = "PlayCommandHandler not found in DI container")]
    private partial void LogPlayCommandHandlerNotFound();

    [LoggerMessage(EventId = 2812, Level = LogLevel.Error, Message = "PauseCommandHandler not found in DI container")]
    private partial void LogPauseCommandHandlerNotFound();

    [LoggerMessage(EventId = 2813, Level = LogLevel.Error, Message = "StopCommandHandler not found in DI container")]
    private partial void LogStopCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2814,
        Level = LogLevel.Error,
        Message = "NextTrackCommandHandler not found in DI container"
    )]
    private partial void LogNextTrackCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2815,
        Level = LogLevel.Error,
        Message = "PreviousTrackCommandHandler not found in DI container"
    )]
    private partial void LogPreviousTrackCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2816,
        Level = LogLevel.Error,
        Message = "SetTrackCommandHandler not found in DI container"
    )]
    private partial void LogSetTrackCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2817,
        Level = LogLevel.Error,
        Message = "SetPlaylistCommandHandler not found in DI container"
    )]
    private partial void LogSetPlaylistCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2818,
        Level = LogLevel.Error,
        Message = "SetZoneVolumeCommandHandler not found in DI container"
    )]
    private partial void LogSetZoneVolumeCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2819,
        Level = LogLevel.Error,
        Message = "GetZoneVolumeQueryHandler not found in DI container"
    )]
    private partial void LogGetZoneVolumeQueryHandlerNotFound();

    [LoggerMessage(
        EventId = 2820,
        Level = LogLevel.Error,
        Message = "SetZoneMuteCommandHandler not found in DI container"
    )]
    private partial void LogSetZoneMuteCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2821,
        Level = LogLevel.Error,
        Message = "ToggleZoneMuteCommandHandler not found in DI container"
    )]
    private partial void LogToggleZoneMuteCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2822,
        Level = LogLevel.Error,
        Message = "SetTrackRepeatCommandHandler not found in DI container"
    )]
    private partial void LogSetTrackRepeatCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2823,
        Level = LogLevel.Error,
        Message = "ToggleTrackRepeatCommandHandler not found in DI container"
    )]
    private partial void LogToggleTrackRepeatCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2824,
        Level = LogLevel.Error,
        Message = "SetPlaylistRepeatCommandHandler not found in DI container"
    )]
    private partial void LogSetPlaylistRepeatCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2825,
        Level = LogLevel.Error,
        Message = "TogglePlaylistRepeatCommandHandler not found in DI container"
    )]
    private partial void LogTogglePlaylistRepeatCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2826,
        Level = LogLevel.Error,
        Message = "SetPlaylistShuffleCommandHandler not found in DI container"
    )]
    private partial void LogSetPlaylistShuffleCommandHandlerNotFound();

    [LoggerMessage(
        EventId = 2827,
        Level = LogLevel.Error,
        Message = "TogglePlaylistShuffleCommandHandler not found in DI container"
    )]
    private partial void LogTogglePlaylistShuffleCommandHandlerNotFound();

    // Zone listing and state operations
    [LoggerMessage(
        EventId = 2828,
        Level = LogLevel.Debug,
        Message = "Getting zones list - Page: {page}, PageSize: {pageSize}, SortBy: {sortBy}"
    )]
    private partial void LogGettingZonesList(int page, int pageSize, string sortBy);

    [LoggerMessage(EventId = 2829, Level = LogLevel.Warning, Message = "Failed to get zones: {error}")]
    private partial void LogFailedToGetZones(string? error);

    [LoggerMessage(EventId = 2830, Level = LogLevel.Error, Message = "Error getting zones")]
    private partial void LogErrorGettingZones(Exception ex);

    [LoggerMessage(EventId = 2831, Level = LogLevel.Debug, Message = "Getting zone {zoneId}")]
    private partial void LogGettingZone(int zoneId);

    [LoggerMessage(EventId = 2832, Level = LogLevel.Warning, Message = "Zone {zoneId} not found")]
    private partial void LogZoneNotFound(int zoneId);

    [LoggerMessage(EventId = 2833, Level = LogLevel.Error, Message = "Error getting zone {zoneId}")]
    private partial void LogErrorGettingZone(Exception ex, int zoneId);

    // Playback control operations
    [LoggerMessage(EventId = 2834, Level = LogLevel.Debug, Message = "Playing zone {zoneId}")]
    private partial void LogPlayingZone(int zoneId);

    [LoggerMessage(EventId = 2835, Level = LogLevel.Warning, Message = "Failed to play zone {zoneId}: {error}")]
    private partial void LogFailedToPlayZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2836, Level = LogLevel.Error, Message = "Error playing zone {zoneId}")]
    private partial void LogErrorPlayingZone(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2837, Level = LogLevel.Debug, Message = "Pausing zone {zoneId}")]
    private partial void LogPausingZone(int zoneId);

    [LoggerMessage(EventId = 2838, Level = LogLevel.Warning, Message = "Failed to pause zone {zoneId}: {error}")]
    private partial void LogFailedToPauseZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2839, Level = LogLevel.Error, Message = "Error pausing zone {zoneId}")]
    private partial void LogErrorPausingZone(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2840, Level = LogLevel.Debug, Message = "Stopping zone {zoneId}")]
    private partial void LogStoppingZone(int zoneId);

    [LoggerMessage(EventId = 2841, Level = LogLevel.Warning, Message = "Failed to stop zone {zoneId}: {error}")]
    private partial void LogFailedToStopZone(int zoneId, string? error);

    [LoggerMessage(EventId = 2842, Level = LogLevel.Error, Message = "Error stopping zone {zoneId}")]
    private partial void LogErrorStoppingZone(Exception ex, int zoneId);

    // Track navigation operations
    [LoggerMessage(EventId = 2843, Level = LogLevel.Debug, Message = "Next track for zone {zoneId}")]
    private partial void LogNextTrack(int zoneId);

    [LoggerMessage(
        EventId = 2844,
        Level = LogLevel.Warning,
        Message = "Failed to go to next track for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToNextTrack(int zoneId, string? error);

    [LoggerMessage(EventId = 2845, Level = LogLevel.Error, Message = "Error going to next track for zone {zoneId}")]
    private partial void LogErrorNextTrack(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2846, Level = LogLevel.Debug, Message = "Previous track for zone {zoneId}")]
    private partial void LogPreviousTrack(int zoneId);

    [LoggerMessage(
        EventId = 2847,
        Level = LogLevel.Warning,
        Message = "Failed to go to previous track for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToPreviousTrack(int zoneId, string? error);

    [LoggerMessage(EventId = 2848, Level = LogLevel.Error, Message = "Error going to previous track for zone {zoneId}")]
    private partial void LogErrorPreviousTrack(Exception ex, int zoneId);

    // Track and playlist setting operations
    [LoggerMessage(EventId = 2849, Level = LogLevel.Debug, Message = "Setting track {trackIndex} for zone {zoneId}")]
    private partial void LogSettingTrack(int trackIndex, int zoneId);

    [LoggerMessage(
        EventId = 2850,
        Level = LogLevel.Warning,
        Message = "Failed to set track for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetTrack(int zoneId, string? error);

    [LoggerMessage(EventId = 2851, Level = LogLevel.Error, Message = "Error setting track for zone {zoneId}")]
    private partial void LogErrorSettingTrack(Exception ex, int zoneId);

    [LoggerMessage(
        EventId = 2852,
        Level = LogLevel.Debug,
        Message = "Setting playlist for zone {zoneId} - ID: {playlistId}, Index: {playlistIndex}"
    )]
    private partial void LogSettingPlaylist(int zoneId, string? playlistId, int? playlistIndex);

    [LoggerMessage(
        EventId = 2853,
        Level = LogLevel.Warning,
        Message = "Failed to set playlist for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetPlaylist(int zoneId, string? error);

    [LoggerMessage(EventId = 2854, Level = LogLevel.Error, Message = "Error setting playlist for zone {zoneId}")]
    private partial void LogErrorSettingPlaylist(Exception ex, int zoneId);

    // Volume operations
    [LoggerMessage(EventId = 2855, Level = LogLevel.Debug, Message = "Setting volume {level} for zone {zoneId}")]
    private partial void LogSettingVolume(int level, int zoneId);

    [LoggerMessage(
        EventId = 2856,
        Level = LogLevel.Warning,
        Message = "Failed to set volume for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetVolume(int zoneId, string? error);

    [LoggerMessage(EventId = 2857, Level = LogLevel.Error, Message = "Error setting volume for zone {zoneId}")]
    private partial void LogErrorSettingVolume(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2858, Level = LogLevel.Debug, Message = "Getting volume for zone {zoneId}")]
    private partial void LogGettingVolume(int zoneId);

    [LoggerMessage(
        EventId = 2859,
        Level = LogLevel.Warning,
        Message = "Failed to get volume for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToGetVolume(int zoneId, string? error);

    [LoggerMessage(EventId = 2860, Level = LogLevel.Error, Message = "Error getting volume for zone {zoneId}")]
    private partial void LogErrorGettingVolume(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2861, Level = LogLevel.Debug, Message = "Increasing volume by {step} for zone {zoneId}")]
    private partial void LogIncreasingVolume(int step, int zoneId);

    [LoggerMessage(EventId = 2862, Level = LogLevel.Error, Message = "Error increasing volume for zone {zoneId}")]
    private partial void LogErrorIncreasingVolume(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2863, Level = LogLevel.Debug, Message = "Decreasing volume by {step} for zone {zoneId}")]
    private partial void LogDecreasingVolume(int step, int zoneId);

    [LoggerMessage(EventId = 2864, Level = LogLevel.Error, Message = "Error decreasing volume for zone {zoneId}")]
    private partial void LogErrorDecreasingVolume(Exception ex, int zoneId);

    // Mute operations
    [LoggerMessage(EventId = 2865, Level = LogLevel.Debug, Message = "Setting mute {enabled} for zone {zoneId}")]
    private partial void LogSettingMute(bool enabled, int zoneId);

    [LoggerMessage(EventId = 2866, Level = LogLevel.Warning, Message = "Failed to set mute for zone {zoneId}: {error}")]
    private partial void LogFailedToSetMute(int zoneId, string? error);

    [LoggerMessage(EventId = 2867, Level = LogLevel.Error, Message = "Error setting mute for zone {zoneId}")]
    private partial void LogErrorSettingMute(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2868, Level = LogLevel.Debug, Message = "Getting mute state for zone {zoneId}")]
    private partial void LogGettingMute(int zoneId);

    [LoggerMessage(
        EventId = 2869,
        Level = LogLevel.Warning,
        Message = "Failed to get mute state for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToGetMute(int zoneId, string? error);

    [LoggerMessage(EventId = 2870, Level = LogLevel.Error, Message = "Error getting mute state for zone {zoneId}")]
    private partial void LogErrorGettingMute(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2871, Level = LogLevel.Debug, Message = "Toggling mute for zone {zoneId}")]
    private partial void LogTogglingMute(int zoneId);

    [LoggerMessage(
        EventId = 2872,
        Level = LogLevel.Warning,
        Message = "Failed to toggle mute for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToToggleMute(int zoneId, string? error);

    [LoggerMessage(EventId = 2873, Level = LogLevel.Error, Message = "Error toggling mute for zone {zoneId}")]
    private partial void LogErrorTogglingMute(Exception ex, int zoneId);

    // Track repeat operations
    [LoggerMessage(
        EventId = 2874,
        Level = LogLevel.Debug,
        Message = "Setting track repeat {enabled} for zone {zoneId}"
    )]
    private partial void LogSettingTrackRepeat(bool enabled, int zoneId);

    [LoggerMessage(
        EventId = 2875,
        Level = LogLevel.Warning,
        Message = "Failed to set track repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetTrackRepeat(int zoneId, string? error);

    [LoggerMessage(EventId = 2876, Level = LogLevel.Error, Message = "Error setting track repeat for zone {zoneId}")]
    private partial void LogErrorSettingTrackRepeat(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2877, Level = LogLevel.Debug, Message = "Toggling track repeat for zone {zoneId}")]
    private partial void LogTogglingTrackRepeat(int zoneId);

    [LoggerMessage(
        EventId = 2878,
        Level = LogLevel.Warning,
        Message = "Failed to toggle track repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToToggleTrackRepeat(int zoneId, string? error);

    [LoggerMessage(EventId = 2879, Level = LogLevel.Error, Message = "Error toggling track repeat for zone {zoneId}")]
    private partial void LogErrorTogglingTrackRepeat(Exception ex, int zoneId);

    // Playlist repeat operations
    [LoggerMessage(
        EventId = 2880,
        Level = LogLevel.Debug,
        Message = "Setting playlist repeat {enabled} for zone {zoneId}"
    )]
    private partial void LogSettingPlaylistRepeat(bool enabled, int zoneId);

    [LoggerMessage(
        EventId = 2881,
        Level = LogLevel.Warning,
        Message = "Failed to set playlist repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetPlaylistRepeat(int zoneId, string? error);

    [LoggerMessage(EventId = 2882, Level = LogLevel.Error, Message = "Error setting playlist repeat for zone {zoneId}")]
    private partial void LogErrorSettingPlaylistRepeat(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2883, Level = LogLevel.Debug, Message = "Toggling playlist repeat for zone {zoneId}")]
    private partial void LogTogglingPlaylistRepeat(int zoneId);

    [LoggerMessage(
        EventId = 2884,
        Level = LogLevel.Warning,
        Message = "Failed to toggle playlist repeat for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToTogglePlaylistRepeat(int zoneId, string? error);

    [LoggerMessage(
        EventId = 2885,
        Level = LogLevel.Error,
        Message = "Error toggling playlist repeat for zone {zoneId}"
    )]
    private partial void LogErrorTogglingPlaylistRepeat(Exception ex, int zoneId);

    // Playlist shuffle operations
    [LoggerMessage(
        EventId = 2886,
        Level = LogLevel.Debug,
        Message = "Setting playlist shuffle {enabled} for zone {zoneId}"
    )]
    private partial void LogSettingPlaylistShuffle(bool enabled, int zoneId);

    [LoggerMessage(
        EventId = 2887,
        Level = LogLevel.Warning,
        Message = "Failed to set playlist shuffle for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToSetPlaylistShuffle(int zoneId, string? error);

    [LoggerMessage(
        EventId = 2888,
        Level = LogLevel.Error,
        Message = "Error setting playlist shuffle for zone {zoneId}"
    )]
    private partial void LogErrorSettingPlaylistShuffle(Exception ex, int zoneId);

    [LoggerMessage(EventId = 2889, Level = LogLevel.Debug, Message = "Toggling playlist shuffle for zone {zoneId}")]
    private partial void LogTogglingPlaylistShuffle(int zoneId);

    [LoggerMessage(
        EventId = 2890,
        Level = LogLevel.Warning,
        Message = "Failed to toggle playlist shuffle for zone {zoneId}: {error}"
    )]
    private partial void LogFailedToTogglePlaylistShuffle(int zoneId, string? error);

    [LoggerMessage(
        EventId = 2891,
        Level = LogLevel.Error,
        Message = "Error toggling playlist shuffle for zone {zoneId}"
    )]
    private partial void LogErrorTogglingPlaylistShuffle(Exception ex, int zoneId);
}
