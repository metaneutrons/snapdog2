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
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for zone management operations (API v1).
/// </summary>
[ApiController]
[Route("api/v1/zones")]
[Authorize]
[Produces("application/json")]
public class ZonesController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZonesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZonesController"/> class.
    /// </summary>
    public ZonesController(IServiceProvider serviceProvider, ILogger<ZonesController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            _logger.LogDebug(
                "Getting zones list - Page: {Page}, PageSize: {PageSize}, SortBy: {SortBy}",
                page,
                pageSize,
                sortBy
            );

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetAllZonesQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetAllZonesQueryHandler not found in DI container");
                return StatusCode(
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

                return Ok(ApiResponse<PaginatedResponse<ZoneInfo>>.CreateSuccess(paginatedResponse));
            }

            _logger.LogWarning("Failed to get zones: {Error}", result.ErrorMessage);
            return StatusCode(
                500,
                ApiResponse<PaginatedResponse<ZoneInfo>>.CreateError(
                    "ZONES_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve zones"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting zones");
            return StatusCode(
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
            _logger.LogDebug("Getting zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetZoneStateQueryHandler not found in DI container");
                return StatusCode(
                    500,
                    ApiResponse<ZoneState>.CreateError("HANDLER_NOT_FOUND", "Zone state handler not available")
                );
            }

            var result = await handler.Handle(new GetZoneStateQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<ZoneState>.CreateSuccess(result.Value));
            }

            _logger.LogWarning("Zone {ZoneId} not found", zoneId);
            return NotFound(ApiResponse<ZoneState>.CreateError("ZONE_NOT_FOUND", $"Zone {zoneId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<ZoneState>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
        return await GetZone(zoneId, cancellationToken);
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
            _logger.LogDebug("Playing zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.PlayCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("PlayCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Play handler not available"));
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
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to play zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(ApiResponse.CreateError("PLAY_ERROR", result.ErrorMessage ?? "Failed to play zone"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Pausing zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.PauseCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("PauseCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Pause handler not available"));
            }

            var command = new PauseCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to pause zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(ApiResponse.CreateError("PAUSE_ERROR", result.ErrorMessage ?? "Failed to pause zone"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Stopping zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.StopCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("StopCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Stop handler not available"));
            }

            var command = new StopCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to stop zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(ApiResponse.CreateError("STOP_ERROR", result.ErrorMessage ?? "Failed to stop zone"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Next track for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.NextTrackCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("NextTrackCommandHandler not found in DI container");
                return StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Next track handler not available")
                );
            }

            var command = new NextTrackCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to go to next track for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse.CreateError("NEXT_TRACK_ERROR", result.ErrorMessage ?? "Failed to go to next track")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error going to next track for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Previous track for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.PreviousTrackCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("PreviousTrackCommandHandler not found in DI container");
                return StatusCode(
                    500,
                    ApiResponse.CreateError("HANDLER_NOT_FOUND", "Previous track handler not available")
                );
            }

            var command = new PreviousTrackCommand { ZoneId = zoneId, Source = Core.Enums.CommandSource.Api };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning(
                "Failed to go to previous track for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return BadRequest(
                ApiResponse.CreateError("PREVIOUS_TRACK_ERROR", result.ErrorMessage ?? "Failed to go to previous track")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error going to previous track for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Setting track {TrackIndex} for zone {ZoneId}", request.Index, zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetTrackCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetTrackCommandHandler not found in DI container");
                return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Set track handler not available"));
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
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to set track for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(ApiResponse.CreateError("SET_TRACK_ERROR", result.ErrorMessage ?? "Failed to set track"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting track for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug(
                "Setting playlist for zone {ZoneId} - ID: {PlaylistId}, Index: {PlaylistIndex}",
                zoneId,
                request.Id,
                request.Index
            );

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetPlaylistCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetPlaylistCommandHandler not found in DI container");
                return StatusCode(
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
                return Accepted(ApiResponse.CreateSuccess());
            }

            _logger.LogWarning("Failed to set playlist for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse.CreateError("SET_PLAYLIST_ERROR", result.ErrorMessage ?? "Failed to set playlist")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting playlist for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Setting volume {Volume} for zone {ZoneId}", request.Level, zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetZoneVolumeCommandHandler not found in DI container");
                return StatusCode(
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
                return Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(request.Level)));
            }

            _logger.LogWarning("Failed to set volume for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse<VolumeResponse>.CreateError("VOLUME_ERROR", result.ErrorMessage ?? "Failed to set volume")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting volume for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Getting volume for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetZoneVolumeQueryHandler not found in DI container");
                return StatusCode(
                    500,
                    ApiResponse<VolumeResponse>.CreateError("HANDLER_NOT_FOUND", "Volume query handler not available")
                );
            }

            var result = await handler.Handle(new GetZoneVolumeQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(result.Value)));
            }

            _logger.LogWarning("Failed to get volume for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return NotFound(
                ApiResponse<VolumeResponse>.CreateError("ZONE_NOT_FOUND", result.ErrorMessage ?? "Zone not found")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting volume for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Increasing volume by {Step} for zone {ZoneId}", step, zoneId);

            // First get current volume
            var volumeHandler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
            if (volumeHandler == null)
            {
                return StatusCode(
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
                return NotFound(ApiResponse<VolumeResponse>.CreateError("ZONE_NOT_FOUND", "Zone not found"));
            }

            var newVolume = Math.Min(100, currentVolumeResult.Value + step);

            // Set new volume
            var setHandler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (setHandler == null)
            {
                return StatusCode(
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
                return Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(newVolume)));
            }

            return BadRequest(
                ApiResponse<VolumeResponse>.CreateError(
                    "VOLUME_ERROR",
                    result.ErrorMessage ?? "Failed to increase volume"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error increasing volume for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Decreasing volume by {Step} for zone {ZoneId}", step, zoneId);

            // First get current volume
            var volumeHandler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneVolumeQueryHandler>();
            if (volumeHandler == null)
            {
                return StatusCode(
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
                return NotFound(ApiResponse<VolumeResponse>.CreateError("ZONE_NOT_FOUND", "Zone not found"));
            }

            var newVolume = Math.Max(0, currentVolumeResult.Value - step);

            // Set new volume
            var setHandler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (setHandler == null)
            {
                return StatusCode(
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
                return Ok(ApiResponse<VolumeResponse>.CreateSuccess(new VolumeResponse(newVolume)));
            }

            return BadRequest(
                ApiResponse<VolumeResponse>.CreateError(
                    "VOLUME_ERROR",
                    result.ErrorMessage ?? "Failed to decrease volume"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decreasing volume for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<VolumeResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Setting mute {Enabled} for zone {ZoneId}", request.Enabled, zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetZoneMuteCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetZoneMuteCommandHandler not found in DI container");
                return StatusCode(
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
                return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(request.Enabled)));
            }

            _logger.LogWarning("Failed to set mute for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse<MuteResponse>.CreateError("MUTE_ERROR", result.ErrorMessage ?? "Failed to set mute")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting mute for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Getting mute state for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetZoneStateQueryHandler not found in DI container");
                return StatusCode(
                    500,
                    ApiResponse<MuteResponse>.CreateError("HANDLER_NOT_FOUND", "Zone state handler not available")
                );
            }

            var result = await handler.Handle(new GetZoneStateQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(result.Value.Mute)));
            }

            _logger.LogWarning("Failed to get mute state for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return NotFound(
                ApiResponse<MuteResponse>.CreateError("ZONE_NOT_FOUND", result.ErrorMessage ?? "Zone not found")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mute state for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Toggling mute for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.ToggleZoneMuteCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("ToggleZoneMuteCommandHandler not found in DI container");
                return StatusCode(
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
                    _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(stateResult.Value.Mute)));
                    }
                }

                // Fallback - assume toggle worked
                return Ok(ApiResponse<MuteResponse>.CreateSuccess(new MuteResponse(true)));
            }

            _logger.LogWarning("Failed to toggle mute for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse<MuteResponse>.CreateError(
                    "TOGGLE_MUTE_ERROR",
                    result.ErrorMessage ?? "Failed to toggle mute"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling mute for zone {ZoneId}", zoneId);
            return StatusCode(500, ApiResponse<MuteResponse>.CreateError("INTERNAL_ERROR", "Internal server error"));
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
            _logger.LogDebug("Setting track repeat {Enabled} for zone {ZoneId}", request.Enabled, zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetTrackRepeatCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetTrackRepeatCommandHandler not found in DI container");
                return StatusCode(
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
                return Ok(ApiResponse<TrackRepeatResponse>.CreateSuccess(new TrackRepeatResponse(request.Enabled)));
            }

            _logger.LogWarning("Failed to set track repeat for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse<TrackRepeatResponse>.CreateError(
                    "TRACK_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to set track repeat"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting track repeat for zone {ZoneId}", zoneId);
            return StatusCode(
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
            _logger.LogDebug("Toggling track repeat for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.ToggleTrackRepeatCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("ToggleTrackRepeatCommandHandler not found in DI container");
                return StatusCode(
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
                    _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return Ok(
                            ApiResponse<TrackRepeatResponse>.CreateSuccess(
                                new TrackRepeatResponse(stateResult.Value.TrackRepeat)
                            )
                        );
                    }
                }

                return Ok(ApiResponse<TrackRepeatResponse>.CreateSuccess(new TrackRepeatResponse(true)));
            }

            _logger.LogWarning("Failed to toggle track repeat for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse<TrackRepeatResponse>.CreateError(
                    "TOGGLE_TRACK_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to toggle track repeat"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling track repeat for zone {ZoneId}", zoneId);
            return StatusCode(
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
            _logger.LogDebug("Setting playlist repeat {Enabled} for zone {ZoneId}", request.Enabled, zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetPlaylistRepeatCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetPlaylistRepeatCommandHandler not found in DI container");
                return StatusCode(
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
                return Ok(
                    ApiResponse<PlaylistRepeatResponse>.CreateSuccess(new PlaylistRepeatResponse(request.Enabled))
                );
            }

            _logger.LogWarning("Failed to set playlist repeat for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(
                ApiResponse<PlaylistRepeatResponse>.CreateError(
                    "PLAYLIST_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to set playlist repeat"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting playlist repeat for zone {ZoneId}", zoneId);
            return StatusCode(
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
            _logger.LogDebug("Toggling playlist repeat for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.TogglePlaylistRepeatCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("TogglePlaylistRepeatCommandHandler not found in DI container");
                return StatusCode(
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
                    _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return Ok(
                            ApiResponse<PlaylistRepeatResponse>.CreateSuccess(
                                new PlaylistRepeatResponse(stateResult.Value.PlaylistRepeat)
                            )
                        );
                    }
                }

                return Ok(ApiResponse<PlaylistRepeatResponse>.CreateSuccess(new PlaylistRepeatResponse(true)));
            }

            _logger.LogWarning(
                "Failed to toggle playlist repeat for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return BadRequest(
                ApiResponse<PlaylistRepeatResponse>.CreateError(
                    "TOGGLE_PLAYLIST_REPEAT_ERROR",
                    result.ErrorMessage ?? "Failed to toggle playlist repeat"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling playlist repeat for zone {ZoneId}", zoneId);
            return StatusCode(
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
            _logger.LogDebug("Setting playlist shuffle {Enabled} for zone {ZoneId}", request.Enabled, zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetPlaylistShuffleCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetPlaylistShuffleCommandHandler not found in DI container");
                return StatusCode(
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
                return Ok(
                    ApiResponse<PlaylistShuffleResponse>.CreateSuccess(new PlaylistShuffleResponse(request.Enabled))
                );
            }

            _logger.LogWarning(
                "Failed to set playlist shuffle for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return BadRequest(
                ApiResponse<PlaylistShuffleResponse>.CreateError(
                    "PLAYLIST_SHUFFLE_ERROR",
                    result.ErrorMessage ?? "Failed to set playlist shuffle"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting playlist shuffle for zone {ZoneId}", zoneId);
            return StatusCode(
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
            _logger.LogDebug("Toggling playlist shuffle for zone {ZoneId}", zoneId);

            var handler =
                _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.TogglePlaylistShuffleCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("TogglePlaylistShuffleCommandHandler not found in DI container");
                return StatusCode(
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
                    _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
                if (stateHandler != null)
                {
                    var stateResult = await stateHandler.Handle(
                        new GetZoneStateQuery { ZoneId = zoneId },
                        cancellationToken
                    );
                    if (stateResult.IsSuccess && stateResult.Value != null)
                    {
                        return Ok(
                            ApiResponse<PlaylistShuffleResponse>.CreateSuccess(
                                new PlaylistShuffleResponse(stateResult.Value.PlaylistShuffle)
                            )
                        );
                    }
                }

                return Ok(ApiResponse<PlaylistShuffleResponse>.CreateSuccess(new PlaylistShuffleResponse(true)));
            }

            _logger.LogWarning(
                "Failed to toggle playlist shuffle for zone {ZoneId}: {Error}",
                zoneId,
                result.ErrorMessage
            );
            return BadRequest(
                ApiResponse<PlaylistShuffleResponse>.CreateError(
                    "TOGGLE_PLAYLIST_SHUFFLE_ERROR",
                    result.ErrorMessage ?? "Failed to toggle playlist shuffle"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling playlist shuffle for zone {ZoneId}", zoneId);
            return StatusCode(
                500,
                ApiResponse<PlaylistShuffleResponse>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
        }
    }
}
