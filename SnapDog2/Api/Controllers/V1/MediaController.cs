//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Api.Controllers.V1;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Server.Playlists.Handlers;
using SnapDog2.Server.Playlists.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Media controller for managing playlists and tracks.
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
    /// <param name="playlistIndex">Playlist identifier.</param>
    /// <returns>Playlist details with tracks.</returns>
    /// <response code="200">Playlist retrieved successfully.</response>
    /// <response code="400">Invalid playlist ID.</response>
    /// <response code="404">Playlist not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("playlists/{playlistIndex}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistWithTracks>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PlaylistWithTracks>>> GetPlaylist([FromRoute] string playlistIndex)
    {
        if (string.IsNullOrWhiteSpace(playlistIndex))
        {
            return this.BadRequest(
                ApiResponse<PlaylistWithTracks>.CreateError("INVALID_ID", "Playlist ID is required")
            );
        }

        LogGettingPlaylist(this._logger, playlistIndex);

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
            if (playlistIndex.Equals("radio", StringComparison.OrdinalIgnoreCase) || playlistIndex == "1")
            {
                var radioQuery = new GetPlaylistQuery { PlaylistIndex = 1 };
                var radioResult = await handler.Handle(radioQuery, CancellationToken.None);

                if (!radioResult.IsSuccess)
                {
                    LogGetPlaylistError(this._logger, playlistIndex, radioResult.ErrorMessage ?? "Unknown error");
                    return this.StatusCode(
                        500,
                        ApiResponse<PlaylistWithTracks>.CreateError(
                            "PLAYLIST_ERROR",
                            radioResult.ErrorMessage ?? "Failed to retrieve radio playlist"
                        )
                    );
                }

                LogPlaylistRetrieved(this._logger, playlistIndex, radioResult.Value?.Tracks.Count ?? 0);
                return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(radioResult.Value!));
            }

            // Try to parse as integer playlistIndex for backward compatibility
            if (int.TryParse(playlistIndex, out var parsedIndex) && parsedIndex > 0)
            {
                var indexQuery = new GetPlaylistQuery { PlaylistIndex = parsedIndex };
                var indexResult = await handler.Handle(indexQuery, CancellationToken.None);

                if (!indexResult.IsSuccess)
                {
                    if (indexResult.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        LogPlaylistNotFound(this._logger, playlistIndex);
                        return this.NotFound(
                            ApiResponse<PlaylistWithTracks>.CreateError(
                                "PLAYLIST_NOT_FOUND",
                                $"Playlist '{playlistIndex}' not found"
                            )
                        );
                    }

                    LogGetPlaylistError(this._logger, playlistIndex, indexResult.ErrorMessage ?? "Unknown error");
                    return this.StatusCode(
                        500,
                        ApiResponse<PlaylistWithTracks>.CreateError(
                            "PLAYLIST_ERROR",
                            indexResult.ErrorMessage ?? "Failed to retrieve playlist"
                        )
                    );
                }

                LogPlaylistRetrieved(this._logger, playlistIndex, indexResult.Value?.Tracks.Count ?? 0);
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
                LogGetPlaylistError(this._logger, playlistIndex, allPlaylistsResult.ErrorMessage ?? "Unknown error");
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
                p.SubsonicPlaylistId.Equals(playlistIndex, StringComparison.OrdinalIgnoreCase)
            );

            if (targetPlaylist == null || !targetPlaylist.Index.HasValue)
            {
                LogPlaylistNotFound(this._logger, playlistIndex);
                return this.NotFound(
                    ApiResponse<PlaylistWithTracks>.CreateError(
                        "PLAYLIST_NOT_FOUND",
                        $"Playlist '{playlistIndex}' not found"
                    )
                );
            }

            // Use the playlist's index to get the full details
            var query = new GetPlaylistQuery { PlaylistIndex = targetPlaylist.Index.Value };
            var result = await handler.Handle(query, CancellationToken.None);

            if (!result.IsSuccess)
            {
                LogGetPlaylistError(this._logger, playlistIndex, result.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<PlaylistWithTracks>.CreateError(
                        "PLAYLIST_ERROR",
                        result.ErrorMessage ?? "Failed to retrieve playlist"
                    )
                );
            }

            LogPlaylistRetrieved(this._logger, playlistIndex, result.Value?.Tracks.Count ?? 0);
            return this.Ok(ApiResponse<PlaylistWithTracks>.CreateSuccess(result.Value!));
        }
        catch (Exception ex)
        {
            LogGetPlaylistError(this._logger, playlistIndex, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<PlaylistWithTracks>.CreateError("INTERNAL_ERROR", "Failed to retrieve playlist")
            );
        }
    }

    /// <summary>
    /// Gets tracks from a specific playlist with pagination.
    /// </summary>
    /// <param name="playlistIndex">Playlist identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-100).</param>
    /// <returns>Paginated list of tracks from the playlist.</returns>
    /// <response code="200">Playlist tracks retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    /// <response code="404">Playlist not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("playlists/{playlistIndex}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<Page<TrackInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Page<TrackInfo>>>> GetPlaylistTracks(
        [FromRoute] string playlistIndex,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50
    )
    {
        if (string.IsNullOrWhiteSpace(playlistIndex))
        {
            return this.BadRequest(ApiResponse<Page<TrackInfo>>.CreateError("INVALID_ID", "Playlist ID is required"));
        }

        LogGettingPlaylistTracks(this._logger, playlistIndex, page, pageSize);

        try
        {
            // Get the full playlist first (reuse the logic from GetPlaylist)
            var playlistResult = await this.GetPlaylistInternal(playlistIndex);

            if (!playlistResult.IsSuccess)
            {
                if (playlistResult.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    LogPlaylistNotFound(this._logger, playlistIndex);
                    return this.NotFound(
                        ApiResponse<Page<TrackInfo>>.CreateError(
                            "PLAYLIST_NOT_FOUND",
                            $"Playlist '{playlistIndex}' not found"
                        )
                    );
                }

                LogGetPlaylistError(this._logger, playlistIndex, playlistResult.ErrorMessage ?? "Unknown error");
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

            LogPlaylistTracksRetrieved(this._logger, playlistIndex, paginatedTracks.Length, totalCount);
            return this.Ok(ApiResponse<Page<TrackInfo>>.CreateSuccess(paginatedResponse));
        }
        catch (Exception ex)
        {
            LogGetPlaylistError(this._logger, playlistIndex, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<Page<TrackInfo>>.CreateError("INTERNAL_ERROR", "Failed to retrieve playlist tracks")
            );
        }
    }

    /// <summary>
    /// Gets a specific track from a specific playlist.
    /// </summary>
    /// <param name="playlistIndex">Playlist identifier.</param>
    /// <param name="trackIndex">Track identifier within the playlist.</param>
    /// <returns>Track details from the playlist.</returns>
    /// <response code="200">Track retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    /// <response code="404">Playlist or track not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("playlists/{playlistIndex}/tracks/{trackIndex}")]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TrackInfo>>> GetPlaylistTrack(
        [FromRoute] string playlistIndex,
        [FromRoute] string trackIndex
    )
    {
        if (string.IsNullOrWhiteSpace(playlistIndex))
        {
            return this.BadRequest(
                ApiResponse<TrackInfo>.CreateError("INVALID_PLAYLIST_ID", "Playlist ID is required")
            );
        }

        if (string.IsNullOrWhiteSpace(trackIndex))
        {
            return this.BadRequest(ApiResponse<TrackInfo>.CreateError("INVALID_TRACK_ID", "Track ID is required"));
        }

        LogGettingPlaylistTrack(this._logger, playlistIndex, trackIndex);

        try
        {
            // First get the playlist
            var playlistResult = await this.GetPlaylistInternal(playlistIndex);
            if (!playlistResult.IsSuccess)
            {
                if (playlistResult.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    LogPlaylistNotFound(this._logger, playlistIndex);
                    return this.NotFound(
                        ApiResponse<TrackInfo>.CreateError(
                            "PLAYLIST_NOT_FOUND",
                            $"Playlist '{playlistIndex}' not found"
                        )
                    );
                }

                LogGetPlaylistError(this._logger, playlistIndex, playlistResult.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<TrackInfo>.CreateError(
                        "PLAYLIST_ERROR",
                        playlistResult.ErrorMessage ?? "Failed to retrieve playlist"
                    )
                );
            }

            var tracks = playlistResult.Value?.Tracks ?? new List<TrackInfo>();

            // Try to find track by index (1-based) or by ID
            TrackInfo? targetTrack;

            // Try parsing as 1-based index first
            if (int.TryParse(trackIndex, out var trackNumber) && trackNumber > 0 && trackNumber <= tracks.Count)
            {
                targetTrack = tracks[trackNumber - 1];
            }
            else
            {
                // Try finding by track index
                targetTrack = tracks.FirstOrDefault(t =>
                    (t.Index?.ToString() ?? "0").Equals(trackIndex, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (targetTrack == null)
            {
                LogTrackNotFound(this._logger, trackIndex, playlistIndex);
                return this.NotFound(
                    ApiResponse<TrackInfo>.CreateError(
                        "TRACK_NOT_FOUND",
                        $"Track '{trackIndex}' not found in playlist '{playlistIndex}'"
                    )
                );
            }

            LogPlaylistTrackRetrieved(this._logger, playlistIndex, trackIndex);
            return this.Ok(ApiResponse<TrackInfo>.CreateSuccess(targetTrack));
        }
        catch (Exception ex)
        {
            LogGetPlaylistTrackError(this._logger, playlistIndex, trackIndex, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<TrackInfo>.CreateError("INTERNAL_ERROR", "Failed to retrieve track from playlist")
            );
        }
    }

    /// <summary>
    /// Gets details for a specific track.
    /// </summary>
    /// <param name="trackIndex">Track identifier.</param>
    /// <returns>Track details.</returns>
    /// <response code="200">Track retrieved successfully.</response>
    /// <response code="400">Invalid track ID.</response>
    /// <response code="404">Track not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("tracks/{trackIndex}")]
    [ProducesResponseType(typeof(ApiResponse<TrackInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TrackInfo>>> GetTrack([FromRoute] string trackIndex)
    {
        if (string.IsNullOrWhiteSpace(trackIndex))
        {
            return this.BadRequest(ApiResponse<TrackInfo>.CreateError("INVALID_ID", "Track ID is required"));
        }

        LogGettingTrack(this._logger, trackIndex);

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

            var query = new GetTrackQuery { TrackId = trackIndex };
            var result = await handler.Handle(query, CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    LogTrackNotFound(this._logger, trackIndex);
                    return this.NotFound(
                        ApiResponse<TrackInfo>.CreateError("TRACK_NOT_FOUND", $"Track '{trackIndex}' not found")
                    );
                }

                LogGetTrackError(this._logger, trackIndex, result.ErrorMessage ?? "Unknown error");
                return this.StatusCode(
                    500,
                    ApiResponse<TrackInfo>.CreateError("TRACK_ERROR", result.ErrorMessage ?? "Failed to retrieve track")
                );
            }

            LogTrackRetrieved(this._logger, trackIndex);
            return this.Ok(ApiResponse<TrackInfo>.CreateSuccess(result.Value!));
        }
        catch (Exception ex)
        {
            LogGetTrackError(this._logger, trackIndex, ex.Message);
            return this.StatusCode(
                500,
                ApiResponse<TrackInfo>.CreateError("INTERNAL_ERROR", "Failed to retrieve track")
            );
        }
    }

    /// <summary>
    /// Internal helper method to get playlist without HTTP response wrapping.
    /// </summary>
    private async Task<Result<PlaylistWithTracks>> GetPlaylistInternal(string playlistIndex)
    {
        var handler = this._serviceProvider.GetService<GetPlaylistQueryHandler>();
        if (handler == null)
        {
            return Result<PlaylistWithTracks>.Failure("Playlist handler not available");
        }

        // Handle special case for radio playlist
        if (playlistIndex.Equals("radio", StringComparison.OrdinalIgnoreCase) || playlistIndex == "1")
        {
            var radioQuery = new GetPlaylistQuery { PlaylistIndex = 1 };
            return await handler.Handle(radioQuery, CancellationToken.None);
        }

        // Try to parse as integer playlistIndex for backward compatibility
        if (int.TryParse(playlistIndex, out var parsedPlaylistIndex) && parsedPlaylistIndex > 0)
        {
            var indexQuery = new GetPlaylistQuery { PlaylistIndex = parsedPlaylistIndex };
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
        var targetPlaylist = allPlaylists.FirstOrDefault(p =>
            p.SubsonicPlaylistId.Equals(playlistIndex, StringComparison.OrdinalIgnoreCase)
        );

        if (targetPlaylist == null || !targetPlaylist.Index.HasValue)
        {
            return Result<PlaylistWithTracks>.Failure($"Playlist '{playlistIndex}' not found");
        }

        // Use the playlist's index to get the full details
        var query = new GetPlaylistQuery { PlaylistIndex = targetPlaylist.Index.Value };
        return await handler.Handle(query, CancellationToken.None);
    }

    #region Logging

    [LoggerMessage(EventId = 113150, Level = LogLevel.Debug, Message = "Getting playlists (page {Page}, size {PageSize})"
)]
    private static partial void LogGettingPlaylists(ILogger logger, int page, int pageSize);

    [LoggerMessage(EventId = 113151, Level = LogLevel.Information, Message = "Retrieved {Count} playlists (total: {Total})"
)]
    private static partial void LogPlaylistsRetrieved(ILogger logger, int count, int total);

    [LoggerMessage(EventId = 113152, Level = LogLevel.Error, Message = "Failed → get playlists: {ErrorMessage}"
)]
    private static partial void LogGetPlaylistsError(ILogger logger, string errorMessage);

    [LoggerMessage(EventId = 113153, Level = LogLevel.Debug, Message = "Getting playlist: {PlaylistId}"
)]
    private static partial void LogGettingPlaylist(ILogger logger, string playlistId);

    [LoggerMessage(EventId = 113154, Level = LogLevel.Information, Message = "Retrieved playlist: {PlaylistId} with {TrackCount} tracks"
)]
    private static partial void LogPlaylistRetrieved(ILogger logger, string playlistId, int trackCount);

    [LoggerMessage(EventId = 113155, Level = LogLevel.Warning, Message = "Playlist not found: {PlaylistId}"
)]
    private static partial void LogPlaylistNotFound(ILogger logger, string playlistId);

    [LoggerMessage(EventId = 113156, Level = LogLevel.Error, Message = "Failed → get playlist: {PlaylistId}, error: {ErrorMessage}"
)]
    private static partial void LogGetPlaylistError(ILogger logger, string playlistId, string errorMessage);

    [LoggerMessage(EventId = 113157, Level = LogLevel.Debug, Message = "Getting tracks for playlist: {PlaylistId} (page {Page}, size {PageSize})"
)]
    private static partial void LogGettingPlaylistTracks(ILogger logger, string playlistId, int page, int pageSize);

    [LoggerMessage(EventId = 113158, Level = LogLevel.Information, Message = "Retrieved {Count} tracks for playlist: {PlaylistId} (total: {Total})"
)]
    private static partial void LogPlaylistTracksRetrieved(ILogger logger, string playlistId, int count, int total);

    [LoggerMessage(EventId = 113159, Level = LogLevel.Debug, Message = "Getting track: {TrackId}"
)]
    private static partial void LogGettingTrack(ILogger logger, string trackId);

    [LoggerMessage(EventId = 113160, Level = LogLevel.Information, Message = "Retrieved track: {TrackId}"
)]
    private static partial void LogTrackRetrieved(ILogger logger, string trackId);

    [LoggerMessage(EventId = 113161, Level = LogLevel.Warning, Message = "Track not found: {TrackId}"
)]
    private static partial void LogTrackNotFound(ILogger logger, string trackId);

    [LoggerMessage(EventId = 113162, Level = LogLevel.Error, Message = "Failed → get track: {TrackId}, error: {ErrorMessage}"
)]
    private static partial void LogGetTrackError(ILogger logger, string trackId, string errorMessage);

    [LoggerMessage(EventId = 113163, Level = LogLevel.Error, Message = "Handler not found: {HandlerName}"
)]
    private static partial void LogHandlerNotFound(ILogger logger, string handlerName);

    [LoggerMessage(EventId = 113164, Level = LogLevel.Debug, Message = "Getting track {TrackId} from playlist {PlaylistId}"
)]
    private static partial void LogGettingPlaylistTrack(ILogger logger, string playlistId, string trackId);

    [LoggerMessage(EventId = 113165, Level = LogLevel.Information, Message = "Retrieved track {TrackId} from playlist {PlaylistId}"
)]
    private static partial void LogPlaylistTrackRetrieved(ILogger logger, string playlistId, string trackId);

    [LoggerMessage(EventId = 113166, Level = LogLevel.Warning, Message = "Track {TrackId} not found in playlist {PlaylistId}"
)]
    private static partial void LogTrackNotFound(ILogger logger, string trackId, string playlistId);

    [LoggerMessage(EventId = 113167, Level = LogLevel.Error, Message = "Failed → get track {TrackId} from playlist {PlaylistId}: {ErrorMessage}"
)]
    private static partial void LogGetPlaylistTrackError(
        ILogger logger,
        string playlistId,
        string trackId,
        string errorMessage
    );

    #endregion
}
