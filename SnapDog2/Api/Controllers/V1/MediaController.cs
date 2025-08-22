namespace SnapDog2.Api.Controllers.V1;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Playlists.Handlers;
using SnapDog2.Server.Features.Playlists.Queries;

/// <summary>
/// Media controller for managing playlists, tracks, and media sources.
/// Provides access to Subsonic integration and media library functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaController"/> class.
/// </remarks>
/// <param name="serviceProvider">Service provider for handler resolution.</param>
/// <param name="logger">Logger instance.</param>
[ApiController]
[Route("api/v1/media")]
[Authorize]
public partial class MediaController(IServiceProvider serviceProvider, ILogger<MediaController> logger) : ControllerBase
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<MediaController> _logger = logger;

    /// <summary>
    /// Gets all configured media sources.
    /// </summary>
    /// <returns>List of available media sources.</returns>
    /// <response code="200">Media sources retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(ApiResponse<List<MediaSourceInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponse<List<MediaSourceInfo>>>> GetMediaSources()
    {
        LogGettingMediaSources(this._logger);

        try
        {
            // For now, return configured sources based on enabled services
            // This could be expanded to query actual service status
            var sources = new List<MediaSourceInfo>
            {
                new MediaSourceInfo
                {
                    Id = "radio",
                    Name = "Radio Stations",
                    Type = "radio",
                    IsAvailable = true,
                    Description = "Configured radio stations from environment variables",
                },
            };

            // Add Subsonic source if available (this would be determined by service availability)
            // For now, we'll assume it's available if the endpoint is being called
            sources.Add(
                new MediaSourceInfo
                {
                    Id = "subsonic",
                    Name = "Subsonic Music Library",
                    Type = "subsonic",
                    IsAvailable = true,
                    Description = "Music library from Subsonic-compatible server",
                }
            );

            LogMediaSourcesRetrieved(this._logger, sources.Count);
            return Task.FromResult<ActionResult<ApiResponse<List<MediaSourceInfo>>>>(
                this.Ok(ApiResponse<List<MediaSourceInfo>>.CreateSuccess(sources))
            );
        }
        catch (Exception ex)
        {
            LogGetMediaSourcesError(this._logger, ex);
            return Task.FromResult<ActionResult<ApiResponse<List<MediaSourceInfo>>>>(
                this.StatusCode(
                    500,
                    ApiResponse<List<MediaSourceInfo>>.CreateError(
                        "MEDIA_SOURCES_ERROR",
                        "Failed to retrieve media sources"
                    )
                )
            );
        }
    }

    /// <summary>
    /// Gets all available playlists from all configured sources.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-100).</param>
    /// <returns>Paginated list of playlists.</returns>
    /// <response code="200">Playlists retrieved successfully.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("playlists")]
    [ProducesResponseType(typeof(ApiResponse<Page<PlaylistInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Page<PlaylistInfo>>>> GetPlaylists(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20
    )
    {
        LogGettingPlaylists(this._logger, page, pageSize);

        try
        {
            var handler = this._serviceProvider.GetService<GetAllPlaylistsQueryHandler>();
            if (handler == null)
            {
                LogHandlerNotFound(this._logger, nameof(GetAllPlaylistsQueryHandler));
                return this.StatusCode(
                    500,
                    ApiResponse<Page<PlaylistInfo>>.CreateError("HANDLER_NOT_FOUND", "Playlist handler not available")
                );
            }

            var query = new GetAllPlaylistsQuery();
            var result = await handler.Handle(query, CancellationToken.None);

            if (!result.IsSuccess)
            {
                LogGetPlaylistsError(this._logger, result.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<Page<PlaylistInfo>>.CreateError(
                        "PLAYLISTS_ERROR",
                        result.ErrorMessage ?? "Failed to retrieve playlists"
                    )
                );
            }

            var playlists = result.Value ?? new List<PlaylistInfo>();

            // Apply pagination
            var totalCount = playlists.Count;
            var paginatedPlaylists = playlists.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

            var paginatedResponse = new Page<PlaylistInfo>(
                Items: paginatedPlaylists,
                Total: totalCount,
                PageSize: pageSize,
                PageNumber: page
            );

            LogPlaylistsRetrieved(this._logger, paginatedPlaylists.Length, totalCount);
            return this.Ok(ApiResponse<Page<PlaylistInfo>>.CreateSuccess(paginatedResponse));
        }
        catch (Exception ex)
        {
            LogGetPlaylistsError(this._logger, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<Page<PlaylistInfo>>.CreateError("INTERNAL_ERROR", "Failed to retrieve playlists")
            );
        }
    }

    /// <summary>
    /// Gets a specific playlist with its tracks.
    /// </summary>
    /// <param name="index">Playlist identifier.</param>
    /// <returns>Playlist details with tracks.</returns>
    /// <response code="200">Playlist retrieved successfully.</response>
    /// <response code="400">Invalid playlist ID.</response>
    /// <response code="404">Playlist not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("playlists/{index}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistWithTracks>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PlaylistWithTracks>>> GetPlaylist([FromRoute] string index)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            return this.BadRequest(
                ApiResponse<PlaylistWithTracks>.CreateError("INVALID_ID", "Playlist ID is required")
            );
        }

        LogGettingPlaylist(this._logger, index);

        try
        {
            var handler = this._serviceProvider.GetService<GetPlaylistQueryHandler>();
            if (handler == null)
            {
                LogHandlerNotFound(this._logger, nameof(GetPlaylistQueryHandler));
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistWithTracks>.CreateError("HANDLER_NOT_FOUND", "Playlist handler not available")
                );
            }

            // Handle special case for radio playlist
            if (index.Equals("radio", StringComparison.OrdinalIgnoreCase) || index == "1")
            {
                var radioQuery = new GetPlaylistQuery { PlaylistIndex = 1 };
                var radioResult = await handler.Handle(radioQuery, CancellationToken.None);

                if (!radioResult.IsSuccess)
                {
                    LogGetPlaylistError(this._logger, index, radioResult.ErrorMessage ?? "Unknown error");
                    return this.StatusCode(
                        500,
                        ApiResponse<PlaylistWithTracks>.CreateError(
                            "PLAYLIST_ERROR",
                            radioResult.ErrorMessage ?? "Failed to retrieve radio playlist"
                        )
                    );
                }

                LogPlaylistRetrieved(this._logger, index, radioResult.Value?.Tracks?.Count ?? 0);
                return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(radioResult.Value!));
            }

            // Try to parse as integer index for backward compatibility
            if (int.TryParse(index, out var playlistIndex) && playlistIndex > 0)
            {
                var indexQuery = new GetPlaylistQuery { PlaylistIndex = playlistIndex };
                var indexResult = await handler.Handle(indexQuery, CancellationToken.None);

                if (!indexResult.IsSuccess)
                {
                    if (indexResult.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        LogPlaylistNotFound(this._logger, index);
                        return this.NotFound(
                            ApiResponse<PlaylistWithTracks>.CreateError(
                                "PLAYLIST_NOT_FOUND",
                                $"Playlist '{index}' not found"
                            )
                        );
                    }

                    LogGetPlaylistError(this._logger, index, indexResult.ErrorMessage ?? "Unknown error");
                    return this.StatusCode(
                        500,
                        ApiResponse<PlaylistWithTracks>.CreateError(
                            "PLAYLIST_ERROR",
                            indexResult.ErrorMessage ?? "Failed to retrieve playlist"
                        )
                    );
                }

                LogPlaylistRetrieved(this._logger, index, indexResult.Value?.Tracks?.Count ?? 0);
                return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(indexResult.Value!));
            }

            // Handle Subsonic playlist ID directly
            // First get all playlists to find the matching one
            var allPlaylistsHandler = this._serviceProvider.GetService<GetAllPlaylistsQueryHandler>();
            if (allPlaylistsHandler == null)
            {
                LogHandlerNotFound(this._logger, nameof(GetAllPlaylistsQueryHandler));
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistWithTracks>.CreateError("HANDLER_NOT_FOUND", "Playlist handler not available")
                );
            }

            var allPlaylistsQuery = new GetAllPlaylistsQuery();
            var allPlaylistsResult = await allPlaylistsHandler.Handle(allPlaylistsQuery, CancellationToken.None);

            if (!allPlaylistsResult.IsSuccess)
            {
                LogGetPlaylistError(this._logger, index, allPlaylistsResult.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistWithTracks>.CreateError(
                        "PLAYLISTS_ERROR",
                        allPlaylistsResult.ErrorMessage ?? "Failed to retrieve playlists"
                    )
                );
            }

            var allPlaylists = allPlaylistsResult.Value ?? new List<PlaylistInfo>();
            var targetPlaylist = allPlaylists.FirstOrDefault(p =>
                p.Id.Equals(index, StringComparison.OrdinalIgnoreCase)
            );

            if (targetPlaylist == null || !targetPlaylist.Index.HasValue)
            {
                LogPlaylistNotFound(this._logger, index);
                return this.NotFound(
                    ApiResponse<PlaylistWithTracks>.CreateError("PLAYLIST_NOT_FOUND", $"Playlist '{index}' not found")
                );
            }

            // Use the playlist's index to get the full details
            var query = new GetPlaylistQuery { PlaylistIndex = targetPlaylist.Index.Value };
            var result = await handler.Handle(query, CancellationToken.None);

            if (!result.IsSuccess)
            {
                LogGetPlaylistError(this._logger, index, result.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistWithTracks>.CreateError(
                        "PLAYLIST_ERROR",
                        result.ErrorMessage ?? "Failed to retrieve playlist"
                    )
                );
            }

            LogPlaylistRetrieved(this._logger, index, result.Value?.Tracks?.Count ?? 0);
            return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(result.Value!));
        }
        catch (Exception ex)
        {
            LogGetPlaylistError(this._logger, index, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistWithTracks>.CreateError("INTERNAL_ERROR", "Failed to retrieve playlist")
            );
        }
    }

    /// <summary>
    /// Gets tracks from a specific playlist with pagination.
    /// </summary>
    /// <param name="index">Playlist identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-100).</param>
    /// <returns>Paginated list of tracks from the playlist.</returns>
    /// <response code="200">Playlist tracks retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    /// <response code="404">Playlist not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("playlists/{index}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<Page<TrackInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Page<TrackInfo>>>> GetPlaylistTracks(
        [FromRoute] string index,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50
    )
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            return this.BadRequest(ApiResponse<Page<TrackInfo>>.CreateError("INVALID_ID", "Playlist ID is required"));
        }

        LogGettingPlaylistTracks(this._logger, index, page, pageSize);

        try
        {
            // Get the full playlist first (reuse the logic from GetPlaylist)
            var playlistResult = await this.GetPlaylistInternal(index);

            if (!playlistResult.IsSuccess)
            {
                if (playlistResult.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    LogPlaylistNotFound(this._logger, index);
                    return this.NotFound(
                        ApiResponse<Page<TrackInfo>>.CreateError("PLAYLIST_NOT_FOUND", $"Playlist '{index}' not found")
                    );
                }

                LogGetPlaylistError(this._logger, index, playlistResult.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<Page<TrackInfo>>.CreateError(
                        "PLAYLIST_ERROR",
                        playlistResult.ErrorMessage ?? "Failed to retrieve playlist tracks"
                    )
                );
            }

            var tracks = playlistResult.Value?.Tracks ?? new List<TrackInfo>();

            // Apply pagination
            var totalCount = tracks.Count;
            var paginatedTracks = tracks.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

            var paginatedResponse = new Page<TrackInfo>(
                Items: paginatedTracks,
                Total: totalCount,
                PageSize: pageSize,
                PageNumber: page
            );

            LogPlaylistTracksRetrieved(this._logger, index, paginatedTracks.Length, totalCount);
            return this.Ok(ApiResponse<Page<TrackInfo>>.CreateSuccess(paginatedResponse));
        }
        catch (Exception ex)
        {
            LogGetPlaylistError(this._logger, index, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<Page<TrackInfo>>.CreateError("INTERNAL_ERROR", "Failed to retrieve playlist tracks")
            );
        }
    }

    /// <summary>
    /// Gets details for a specific track.
    /// </summary>
    /// <param name="index">Track identifier.</param>
    /// <returns>Track details.</returns>
    /// <response code="200">Track retrieved successfully.</response>
    /// <response code="400">Invalid track ID.</response>
    /// <response code="404">Track not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("tracks/{index}")]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TrackInfo>>> GetTrack([FromRoute] string index)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            return this.BadRequest(ApiResponse<TrackInfo>.CreateError("INVALID_ID", "Track ID is required"));
        }

        LogGettingTrack(this._logger, index);

        try
        {
            var handler = this._serviceProvider.GetService<GetTrackQueryHandler>();
            if (handler == null)
            {
                LogHandlerNotFound(this._logger, nameof(GetTrackQueryHandler));
                return this.StatusCode(
                    500,
                    ApiResponse<TrackInfo>.CreateError("HANDLER_NOT_FOUND", "Track handler not available")
                );
            }

            var query = new GetTrackQuery { TrackId = index };
            var result = await handler.Handle(query, CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    LogTrackNotFound(this._logger, index);
                    return this.NotFound(
                        ApiResponse<TrackInfo>.CreateError("TRACK_NOT_FOUND", $"Track '{index}' not found")
                    );
                }

                LogGetTrackError(this._logger, index, result.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<TrackInfo>.CreateError("TRACK_ERROR", result.ErrorMessage ?? "Failed to retrieve track")
                );
            }

            LogTrackRetrieved(this._logger, index);
            return this.Ok(ApiResponse<TrackInfo>.CreateSuccess(result.Value!));
        }
        catch (Exception ex)
        {
            LogGetTrackError(this._logger, index, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<TrackInfo>.CreateError("INTERNAL_ERROR", "Failed to retrieve track")
            );
        }
    }

    /// <summary>
    /// Internal helper method to get playlist without HTTP response wrapping.
    /// </summary>
    private async Task<Result<PlaylistWithTracks>> GetPlaylistInternal(string index)
    {
        var handler = this._serviceProvider.GetService<GetPlaylistQueryHandler>();
        if (handler == null)
        {
            return Result<PlaylistWithTracks>.Failure("Playlist handler not available");
        }

        // Handle special case for radio playlist
        if (index.Equals("radio", StringComparison.OrdinalIgnoreCase) || index == "1")
        {
            var radioQuery = new GetPlaylistQuery { PlaylistIndex = 1 };
            return await handler.Handle(radioQuery, CancellationToken.None);
        }

        // Try to parse as integer index for backward compatibility
        if (int.TryParse(index, out var playlistIndex) && playlistIndex > 0)
        {
            var indexQuery = new GetPlaylistQuery { PlaylistIndex = playlistIndex };
            return await handler.Handle(indexQuery, CancellationToken.None);
        }

        // Handle Subsonic playlist ID directly
        var allPlaylistsHandler = this._serviceProvider.GetService<GetAllPlaylistsQueryHandler>();
        if (allPlaylistsHandler == null)
        {
            return Result<PlaylistWithTracks>.Failure("Playlist handler not available");
        }

        var allPlaylistsQuery = new GetAllPlaylistsQuery();
        var allPlaylistsResult = await allPlaylistsHandler.Handle(allPlaylistsQuery, CancellationToken.None);

        if (!allPlaylistsResult.IsSuccess)
        {
            return Result<PlaylistWithTracks>.Failure(
                allPlaylistsResult.ErrorMessage ?? "Failed to retrieve playlists"
            );
        }

        var allPlaylists = allPlaylistsResult.Value ?? new List<PlaylistInfo>();
        var targetPlaylist = allPlaylists.FirstOrDefault(p => p.Id.Equals(index, StringComparison.OrdinalIgnoreCase));

        if (targetPlaylist == null || !targetPlaylist.Index.HasValue)
        {
            return Result<PlaylistWithTracks>.Failure($"Playlist '{index}' not found");
        }

        // Use the playlist's index to get the full details
        var query = new GetPlaylistQuery { PlaylistIndex = targetPlaylist.Index.Value };
        return await handler.Handle(query, CancellationToken.None);
    }

    #region Logging

    [LoggerMessage(3000, LogLevel.Debug, "Getting media sources")]
    private static partial void LogGettingMediaSources(ILogger logger);

    [LoggerMessage(3001, LogLevel.Information, "Retrieved {Count} media sources")]
    private static partial void LogMediaSourcesRetrieved(ILogger logger, int count);

    [LoggerMessage(3002, LogLevel.Error, "Failed to get media sources")]
    private static partial void LogGetMediaSourcesError(ILogger logger, Exception ex);

    [LoggerMessage(3003, LogLevel.Debug, "Getting playlists (page {Page}, size {PageSize})")]
    private static partial void LogGettingPlaylists(ILogger logger, int page, int pageSize);

    [LoggerMessage(3004, LogLevel.Information, "Retrieved {Count} playlists (total: {Total})")]
    private static partial void LogPlaylistsRetrieved(ILogger logger, int count, int total);

    [LoggerMessage(3005, LogLevel.Error, "Failed to get playlists: {ErrorMessage}")]
    private static partial void LogGetPlaylistsError(ILogger logger, string errorMessage);

    [LoggerMessage(3006, LogLevel.Debug, "Getting playlist: {PlaylistId}")]
    private static partial void LogGettingPlaylist(ILogger logger, string playlistId);

    [LoggerMessage(3007, LogLevel.Information, "Retrieved playlist: {PlaylistId} with {TrackCount} tracks")]
    private static partial void LogPlaylistRetrieved(ILogger logger, string playlistId, int trackCount);

    [LoggerMessage(3008, LogLevel.Warning, "Playlist not found: {PlaylistId}")]
    private static partial void LogPlaylistNotFound(ILogger logger, string playlistId);

    [LoggerMessage(3009, LogLevel.Error, "Failed to get playlist: {PlaylistId}, error: {ErrorMessage}")]
    private static partial void LogGetPlaylistError(ILogger logger, string playlistId, string errorMessage);

    [LoggerMessage(3010, LogLevel.Debug, "Getting tracks for playlist: {PlaylistId} (page {Page}, size {PageSize})")]
    private static partial void LogGettingPlaylistTracks(ILogger logger, string playlistId, int page, int pageSize);

    [LoggerMessage(3011, LogLevel.Information, "Retrieved {Count} tracks for playlist: {PlaylistId} (total: {Total})")]
    private static partial void LogPlaylistTracksRetrieved(ILogger logger, string playlistId, int count, int total);

    [LoggerMessage(3012, LogLevel.Debug, "Getting track: {TrackId}")]
    private static partial void LogGettingTrack(ILogger logger, string trackId);

    [LoggerMessage(3013, LogLevel.Information, "Retrieved track: {TrackId}")]
    private static partial void LogTrackRetrieved(ILogger logger, string trackId);

    [LoggerMessage(3014, LogLevel.Warning, "Track not found: {TrackId}")]
    private static partial void LogTrackNotFound(ILogger logger, string trackId);

    [LoggerMessage(3015, LogLevel.Error, "Failed to get track: {TrackId}, error: {ErrorMessage}")]
    private static partial void LogGetTrackError(ILogger logger, string trackId, string errorMessage);

    [LoggerMessage(3016, LogLevel.Error, "Handler not found: {HandlerName}")]
    private static partial void LogHandlerNotFound(ILogger logger, string handlerName);

    #endregion
}
