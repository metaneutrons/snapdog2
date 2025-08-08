namespace SnapDog2.Api.Controllers.V1;

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
public partial class MediaController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MediaController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaController"/> class.
    /// </summary>
    public MediaController(IServiceProvider serviceProvider, ILogger<MediaController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Lists configured media sources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of media sources.</returns>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(ApiResponse<List<MediaSourceInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<MediaSourceInfo>>>> GetMediaSources(
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingMediaSources();

            // For now, return static media sources based on typical SnapDog configuration
            var mediaSources = new List<MediaSourceInfo>
            {
                new("radio", "Radio", "Internet Radio Streams"),
                new("subsonic", "Subsonic", "Navidrome Music Library"),
            };

            await Task.CompletedTask; // Satisfy async requirement

            return this.Ok(ApiResponse<List<MediaSourceInfo>>.CreateSuccess(mediaSources));
        }
        catch (Exception ex)
        {
            this.LogErrorGettingMediaSources(ex);
            return this.StatusCode(
                500,
                ApiResponse<List<MediaSourceInfo>>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            this.LogGettingPlaylists(page, pageSize);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetAllPlaylistsQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetAllPlaylistsQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Playlist handler not available"
                    )
                );
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
                        TotalPages = totalPages,
                    },
                };

                return this.Ok(ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateSuccess(paginatedResponse));
            }

            this.LogFailedToGetPlaylists(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateError(
                    "PLAYLISTS_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve playlists"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingPlaylists(ex);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<PlaylistWithTracks>>> GetPlaylist(
        string playlistIdOrIndex,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingPlaylist(playlistIdOrIndex);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetPlaylistTracksQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistWithTracks>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Playlist tracks handler not available"
                    )
                );
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
                var playlistHandler =
                    this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetAllPlaylistsQueryHandler>();
                if (playlistHandler != null)
                {
                    var playlistsResult = await playlistHandler.Handle(new GetAllPlaylistsQuery(), cancellationToken);
                    if (playlistsResult.IsSuccess && playlistsResult.Value != null)
                    {
                        PlaylistInfo? playlistInfo = null;

                        if (
                            int.TryParse(playlistIdOrIndex, out var idx)
                            && idx > 0
                            && idx <= playlistsResult.Value.Count
                        )
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
                                Tracks = result.Value,
                            };

                            return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(playlistWithTracks));
                        }
                    }
                }

                // Fallback - create basic playlist info
                var fallbackInfo = new PlaylistInfo
                {
                    Id = playlistIdOrIndex,
                    Name = $"Playlist {playlistIdOrIndex}",
                    TrackCount = result.Value.Count,
                    Source = "unknown",
                };

                var fallbackPlaylist = new PlaylistWithTracks { Info = fallbackInfo, Tracks = result.Value };

                return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(fallbackPlaylist));
            }

            this.LogPlaylistNotFound(playlistIdOrIndex);
            return this.NotFound(
                ApiResponse<PlaylistWithTracks>.CreateError(
                    "PLAYLIST_NOT_FOUND",
                    $"Playlist {playlistIdOrIndex} not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingPlaylist(playlistIdOrIndex, ex);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistWithTracks>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            this.LogGettingPlaylistTracks(playlistIdOrIndex, page, pageSize);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();
            if (handler == null)
            {
                this.LogCriticalHandlerNotFound("GetPlaylistTracksQueryHandler");
                return this.StatusCode(
                    500,
                    ApiResponse<PaginatedResponse<TrackInfo>>.CreateError(
                        "HANDLER_NOT_FOUND",
                        "Playlist tracks handler not available"
                    )
                );
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
                        TotalPages = totalPages,
                    },
                };

                return this.Ok(ApiResponse<PaginatedResponse<TrackInfo>>.CreateSuccess(paginatedResponse));
            }

            this.LogPlaylistNotFound(playlistIdOrIndex);
            return this.NotFound(
                ApiResponse<PaginatedResponse<TrackInfo>>.CreateError(
                    "PLAYLIST_NOT_FOUND",
                    $"Playlist {playlistIdOrIndex} not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingPlaylistTracks(playlistIdOrIndex, ex);
            return this.StatusCode(
                500,
                ApiResponse<PaginatedResponse<TrackInfo>>.CreateError("INTERNAL_ERROR", "Internal server error")
            );
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
    public async Task<ActionResult<ApiResponse<TrackInfo>>> GetTrack(
        string trackId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingTrack(trackId);

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
                Source = "unknown",
            };

            await Task.CompletedTask; // Satisfy async requirement

            return this.Ok(ApiResponse<TrackInfo>.CreateSuccess(trackInfo));
        }
        catch (Exception ex)
        {
            this.LogErrorGettingTrack(trackId, ex);
            return this.StatusCode(500, ApiResponse<TrackInfo>.CreateError("INTERNAL_ERROR", "Internal server error"));
        }
    }

    #region Logging

    // ARCHITECTURAL PROBLEM - This should never happen in production
    [LoggerMessage(
        2401,
        LogLevel.Critical,
        "🚨 CRITICAL: Handler {HandlerType} not found in DI container - This is a configuration BUG!"
    )]
    private partial void LogCriticalHandlerNotFound(string handlerType);

    // Media Sources (2410-2419)
    [LoggerMessage(2410, LogLevel.Debug, "Getting media sources")]
    private partial void LogGettingMediaSources();

    [LoggerMessage(2411, LogLevel.Error, "Error getting media sources")]
    private partial void LogErrorGettingMediaSources(Exception exception);

    // Playlists (2420-2429)
    [LoggerMessage(2420, LogLevel.Debug, "Getting playlists - Page: {Page}, PageSize: {PageSize}")]
    private partial void LogGettingPlaylists(int page, int pageSize);

    [LoggerMessage(2421, LogLevel.Warning, "Failed to get playlists: {Error}")]
    private partial void LogFailedToGetPlaylists(string? error);

    [LoggerMessage(2422, LogLevel.Error, "Error getting playlists")]
    private partial void LogErrorGettingPlaylists(Exception exception);

    // Specific Playlist (2430-2439)
    [LoggerMessage(2430, LogLevel.Debug, "Getting playlist {PlaylistId}")]
    private partial void LogGettingPlaylist(string playlistId);

    [LoggerMessage(2431, LogLevel.Warning, "Failed to get playlist {PlaylistId}: {Error}")]
    private partial void LogFailedToGetPlaylist(string playlistId, string error);

    [LoggerMessage(2433, LogLevel.Warning, "Playlist {PlaylistId} not found")]
    private partial void LogPlaylistNotFound(string playlistId);

    [LoggerMessage(2432, LogLevel.Error, "Error getting playlist {PlaylistId}")]
    private partial void LogErrorGettingPlaylist(string playlistId, Exception exception);

    // Playlist Tracks (2440-2449)
    [LoggerMessage(
        2440,
        LogLevel.Debug,
        "Getting tracks for playlist {PlaylistId} - Page: {Page}, PageSize: {PageSize}"
    )]
    private partial void LogGettingPlaylistTracks(string playlistId, int page, int pageSize);

    [LoggerMessage(2441, LogLevel.Warning, "Failed to get tracks for playlist {PlaylistId}: {Error}")]
    private partial void LogFailedToGetPlaylistTracks(string playlistId, string error);

    [LoggerMessage(2442, LogLevel.Error, "Error getting tracks for playlist {PlaylistId}")]
    private partial void LogErrorGettingPlaylistTracks(string playlistId, Exception exception);

    // Track Details (2450-2459)
    [LoggerMessage(2450, LogLevel.Debug, "Getting track {TrackId}")]
    private partial void LogGettingTrack(string trackId);

    [LoggerMessage(2451, LogLevel.Warning, "Failed to get track {TrackId}: {Error}")]
    private partial void LogFailedToGetTrack(string trackId, string error);

    [LoggerMessage(2452, LogLevel.Error, "Error getting track {TrackId}")]
    private partial void LogErrorGettingTrack(string trackId, Exception exception);

    #endregion
}
