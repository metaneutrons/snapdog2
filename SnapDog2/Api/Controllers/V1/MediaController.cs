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
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for media management operations (API v1).
/// </summary>
[ApiController]
[Route("api/v1/media")]
[Authorize]
[Produces("application/json")]
public class MediaController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MediaController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaController"/> class.
    /// </summary>
    public MediaController(IServiceProvider serviceProvider, ILogger<MediaController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Lists configured media sources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of media sources.</returns>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(ApiResponse<List<MediaSourceInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<MediaSourceInfo>>>> GetMediaSources(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting media sources");

            // For now, return static media sources based on typical SnapDog configuration
            var mediaSources = new List<MediaSourceInfo>
            {
                new("radio", "Radio", "Internet Radio Streams"),
                new("subsonic", "Subsonic", "Navidrome Music Library")
            };

            await Task.CompletedTask; // Satisfy async requirement

            return Ok(ApiResponse<List<MediaSourceInfo>>.CreateSuccess(mediaSources));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media sources");
            return StatusCode(500, ApiResponse<List<MediaSourceInfo>>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Lists all available playlists.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of playlists.</returns>
    [HttpGet("playlists")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<PlaylistInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PlaylistInfo>>>> GetPlaylists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting playlists - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetAllPlaylistsQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetAllPlaylistsQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateError("HANDLER_NOT_FOUND", "Playlist handler not available"));
            }

            var result = await handler.Handle(new GetAllPlaylistsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var playlists = result.Value;
                
                // Apply pagination
                var totalItems = playlists.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var skip = (page - 1) * pageSize;
                var pagedPlaylists = playlists.Skip(skip).Take(pageSize).ToList();

                var paginatedResponse = new PaginatedResponse<PlaylistInfo>
                {
                    Items = pagedPlaylists,
                    Pagination = new PaginationMetadata
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    }
                };

                return Ok(ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateSuccess(paginatedResponse));
            }

            _logger.LogWarning("Failed to get playlists: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateError("PLAYLISTS_ERROR", result.ErrorMessage ?? "Failed to retrieve playlists"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting playlists");
            return StatusCode(500, ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets details for a playlist.
    /// </summary>
    /// <param name="playlistIdOrIndex">The playlist ID or 1-based index.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Playlist with tracks.</returns>
    [HttpGet("playlists/{playlistIdOrIndex}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistWithTracks>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PlaylistWithTracks>>> GetPlaylist(string playlistIdOrIndex, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting playlist {PlaylistIdOrIndex}", playlistIdOrIndex);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetPlaylistTracksQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<PlaylistWithTracks>.CreateError("HANDLER_NOT_FOUND", "Playlist tracks handler not available"));
            }

            // Try to parse as index first, then use as ID
            GetPlaylistTracksQuery query;
            if (int.TryParse(playlistIdOrIndex, out var index) && index > 0)
            {
                query = new GetPlaylistTracksQuery { PlaylistIndex = index };
            }
            else
            {
                query = new GetPlaylistTracksQuery { PlaylistId = playlistIdOrIndex };
            }

            var result = await handler.Handle(query, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                // Get playlist info
                var playlistHandler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetAllPlaylistsQueryHandler>();
                if (playlistHandler != null)
                {
                    var playlistsResult = await playlistHandler.Handle(new GetAllPlaylistsQuery(), cancellationToken);
                    if (playlistsResult.IsSuccess && playlistsResult.Value != null)
                    {
                        PlaylistInfo? playlistInfo = null;
                        
                        if (int.TryParse(playlistIdOrIndex, out var idx) && idx > 0 && idx <= playlistsResult.Value.Count)
                        {
                            playlistInfo = playlistsResult.Value[idx - 1]; // Convert to 0-based
                        }
                        else
                        {
                            playlistInfo = playlistsResult.Value.FirstOrDefault(p => p.Id == playlistIdOrIndex);
                        }

                        if (playlistInfo != null)
                        {
                            var playlistWithTracks = new PlaylistWithTracks
                            {
                                Info = playlistInfo,
                                Tracks = result.Value
                            };

                            return Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(playlistWithTracks));
                        }
                    }
                }

                // Fallback - create basic playlist info
                var fallbackInfo = new PlaylistInfo
                {
                    Id = playlistIdOrIndex,
                    Name = $"Playlist {playlistIdOrIndex}",
                    TrackCount = result.Value.Count,
                    Source = "unknown"
                };

                var fallbackPlaylist = new PlaylistWithTracks
                {
                    Info = fallbackInfo,
                    Tracks = result.Value
                };

                return Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(fallbackPlaylist));
            }

            _logger.LogWarning("Playlist {PlaylistIdOrIndex} not found", playlistIdOrIndex);
            return NotFound(ApiResponse<PlaylistWithTracks>.CreateError("PLAYLIST_NOT_FOUND", $"Playlist {playlistIdOrIndex} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting playlist {PlaylistIdOrIndex}", playlistIdOrIndex);
            return StatusCode(500, ApiResponse<PlaylistWithTracks>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Lists tracks in a playlist.
    /// </summary>
    /// <param name="playlistIdOrIndex">The playlist ID or 1-based index.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of tracks.</returns>
    [HttpGet("playlists/{playlistIdOrIndex}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<TrackInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TrackInfo>>>> GetPlaylistTracks(
        string playlistIdOrIndex,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting tracks for playlist {PlaylistIdOrIndex} - Page: {Page}, PageSize: {PageSize}", playlistIdOrIndex, page, pageSize);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetPlaylistTracksQueryHandler not found in DI container");
                return StatusCode(500, ApiResponse<PaginatedResponse<TrackInfo>>.CreateError("HANDLER_NOT_FOUND", "Playlist tracks handler not available"));
            }

            // Try to parse as index first, then use as ID
            GetPlaylistTracksQuery query;
            if (int.TryParse(playlistIdOrIndex, out var index) && index > 0)
            {
                query = new GetPlaylistTracksQuery { PlaylistIndex = index };
            }
            else
            {
                query = new GetPlaylistTracksQuery { PlaylistId = playlistIdOrIndex };
            }

            var result = await handler.Handle(query, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var tracks = result.Value;
                
                // Apply pagination
                var totalItems = tracks.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var skip = (page - 1) * pageSize;
                var pagedTracks = tracks.Skip(skip).Take(pageSize).ToList();

                var paginatedResponse = new PaginatedResponse<TrackInfo>
                {
                    Items = pagedTracks,
                    Pagination = new PaginationMetadata
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    }
                };

                return Ok(ApiResponse<PaginatedResponse<TrackInfo>>.CreateSuccess(paginatedResponse));
            }

            _logger.LogWarning("Playlist {PlaylistIdOrIndex} not found", playlistIdOrIndex);
            return NotFound(ApiResponse<PaginatedResponse<TrackInfo>>.CreateError("PLAYLIST_NOT_FOUND", $"Playlist {playlistIdOrIndex} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracks for playlist {PlaylistIdOrIndex}", playlistIdOrIndex);
            return StatusCode(500, ApiResponse<PaginatedResponse<TrackInfo>>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Gets details for a track.
    /// </summary>
    /// <param name="trackId">The track ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Track information.</returns>
    [HttpGet("tracks/{trackId}")]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<TrackInfo>>> GetTrack(string trackId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting track {TrackId}", trackId);

            // For now, we don't have a direct track lookup handler
            // This would typically query the media source (Subsonic/Navidrome) directly
            // For the MVP, we'll return a placeholder response

            var trackInfo = new TrackInfo
            {
                Index = 1,
                Id = trackId,
                Title = $"Track {trackId}",
                Artist = "Unknown Artist",
                Album = "Unknown Album",
                DurationSec = 180, // 3 minutes in seconds
                Source = "unknown"
            };

            await Task.CompletedTask; // Satisfy async requirement

            return Ok(ApiResponse<TrackInfo>.CreateSuccess(trackInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting track {TrackId}", trackId);
            return StatusCode(500, ApiResponse<TrackInfo>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }
}
