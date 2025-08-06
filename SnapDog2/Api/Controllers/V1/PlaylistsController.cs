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
using SnapDog2.Server.Features.Playlists.Queries;

/// <summary>
/// Controller for playlist management operations (API v1).
/// Provides access to radio stations and Subsonic playlists.
/// </summary>
[ApiController]
[Route("api/v1/playlists")]
[Authorize]
[Produces("application/json")]
public partial class PlaylistsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlaylistsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistsController"/> class.
    /// </summary>
    public PlaylistsController(IServiceProvider serviceProvider, ILogger<PlaylistsController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Lists all available playlists (radio stations + Subsonic playlists).
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="sortBy">Sort field (default: index).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of playlists.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<PlaylistInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PlaylistInfo>>>> GetPlaylists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "index",
        CancellationToken cancellationToken = default
    )
    {
        LogGetPlaylistsRequest(_logger, page, pageSize, sortBy);

        var handler = _serviceProvider.GetService<Server.Features.Playlists.Handlers.GetAllPlaylistsQueryHandler>();
        if (handler == null)
        {
            LogGetPlaylistsError(_logger, "Handler not found");
            return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Playlist handler not available"));
        }

        var result = await handler.Handle(new GetAllPlaylistsQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            LogGetPlaylistsError(_logger, result.ErrorMessage ?? "Unknown error");
            return StatusCode(
                500,
                ApiResponse.CreateError("PLAYLISTS_ERROR", result.ErrorMessage ?? "Failed to get playlists")
            );
        }

        // Apply sorting
        var sortedPlaylists = sortBy.ToLowerInvariant() switch
        {
            "name" => (result.Value ?? []).OrderBy(p => p.Name).ToList(),
            "trackcount" => (result.Value ?? []).OrderByDescending(p => p.TrackCount).ToList(),
            "source" => (result.Value ?? []).OrderBy(p => p.Source).ThenBy(p => p.Index).ToList(),
            _ => (result.Value ?? []).OrderBy(p => p.Index ?? int.MaxValue).ToList(), // Default: index
        };

        // Apply pagination
        var totalCount = sortedPlaylists.Count;
        var pagedPlaylists = sortedPlaylists.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var paginatedResponse = new PaginatedResponse<PlaylistInfo>
        {
            Items = pagedPlaylists,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            },
        };

        LogGetPlaylistsSuccess(_logger, totalCount, pagedPlaylists.Count);
        return Ok(ApiResponse<PaginatedResponse<PlaylistInfo>>.CreateSuccess(paginatedResponse));
    }

    /// <summary>
    /// Gets a specific playlist with all its tracks.
    /// </summary>
    /// <param name="playlistId">The playlist identifier ("radio" for radio stations or Subsonic playlist ID).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Playlist with tracks.</returns>
    [HttpGet("{playlistId}")]
    [ProducesResponseType(typeof(ApiResponse<SnapDog2.Api.Models.PlaylistWithTracks>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<SnapDog2.Api.Models.PlaylistWithTracks>>> GetPlaylist(
        [FromRoute] [Required] string playlistId,
        CancellationToken cancellationToken = default
    )
    {
        LogGetPlaylistRequest(_logger, playlistId);

        var handler = _serviceProvider.GetService<Server.Features.Playlists.Handlers.GetPlaylistQueryHandler>();
        if (handler == null)
        {
            LogGetPlaylistError(_logger, playlistId, "Handler not found");
            return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Playlist handler not available"));
        }

        var result = await handler.Handle(new GetPlaylistQuery { PlaylistId = playlistId }, cancellationToken);

        if (!result.IsSuccess)
        {
            LogGetPlaylistError(_logger, playlistId, result.ErrorMessage ?? "Unknown error");

            // Check if it's a not found error
            if ((result.ErrorMessage ?? "").Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(ApiResponse.CreateError("PLAYLIST_NOT_FOUND", $"Playlist '{playlistId}' not found"));
            }

            return StatusCode(
                500,
                ApiResponse.CreateError("PLAYLIST_ERROR", result.ErrorMessage ?? "Failed to get playlist")
            );
        }

        LogGetPlaylistSuccess(_logger, playlistId, result.Value?.Tracks?.Count ?? 0);
        return Ok(ApiResponse<SnapDog2.Api.Models.PlaylistWithTracks>.CreateSuccess(result.Value!));
    }

    /// <summary>
    /// Gets the streaming URL for a specific track.
    /// </summary>
    /// <param name="trackId">The track identifier (Subsonic track ID or radio stream URL).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Streaming URL.</returns>
    [HttpGet("tracks/{trackId}/stream-url")]
    [ProducesResponseType(typeof(ApiResponse<StreamUrlResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<StreamUrlResponse>>> GetStreamUrl(
        [FromRoute] [Required] string trackId,
        CancellationToken cancellationToken = default
    )
    {
        LogGetStreamUrlRequest(_logger, trackId);

        var handler = _serviceProvider.GetService<Server.Features.Playlists.Handlers.GetStreamUrlQueryHandler>();
        if (handler == null)
        {
            LogGetStreamUrlError(_logger, trackId, "Handler not found");
            return StatusCode(500, ApiResponse.CreateError("HANDLER_NOT_FOUND", "Stream URL handler not available"));
        }

        var result = await handler.Handle(new GetStreamUrlQuery { TrackId = trackId }, cancellationToken);

        if (!result.IsSuccess)
        {
            LogGetStreamUrlError(_logger, trackId, result.ErrorMessage ?? "Unknown error");

            // Check if it's a not found error
            if ((result.ErrorMessage ?? "").Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(ApiResponse.CreateError("TRACK_NOT_FOUND", $"Track '{trackId}' not found"));
            }

            return StatusCode(
                500,
                ApiResponse.CreateError("STREAM_URL_ERROR", result.ErrorMessage ?? "Failed to get stream URL")
            );
        }

        var response = new StreamUrlResponse
        {
            TrackId = trackId,
            StreamUrl = result.Value ?? string.Empty,
            TimestampUtc = DateTime.UtcNow,
        };

        LogGetStreamUrlSuccess(_logger, trackId);
        return Ok(ApiResponse<StreamUrlResponse>.CreateSuccess(response));
    }

    /// <summary>
    /// Tests the connection to the Subsonic server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Connection test result.</returns>
    [HttpPost("test-connection")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ConnectionTestResponse>>> TestConnection(
        CancellationToken cancellationToken = default
    )
    {
        LogTestConnectionRequest(_logger);

        var handler =
            _serviceProvider.GetService<Server.Features.Playlists.Handlers.TestSubsonicConnectionQueryHandler>();
        if (handler == null)
        {
            LogTestConnectionError(_logger, "Handler not found");
            return StatusCode(
                500,
                ApiResponse.CreateError("HANDLER_NOT_FOUND", "Connection test handler not available")
            );
        }

        var result = await handler.Handle(new TestSubsonicConnectionQuery(), cancellationToken);

        var response = new ConnectionTestResponse
        {
            IsConnected = result.IsSuccess,
            Message = result.IsSuccess ? "Connection successful" : (result.ErrorMessage ?? "Connection failed"),
            TimestampUtc = DateTime.UtcNow,
        };

        if (result.IsSuccess)
        {
            LogTestConnectionSuccess(_logger);
            return Ok(ApiResponse<ConnectionTestResponse>.CreateSuccess(response));
        }
        else
        {
            LogTestConnectionError(_logger, result.ErrorMessage ?? "Unknown error");
            return StatusCode(500, ApiResponse<ConnectionTestResponse>.CreateSuccess(response));
        }
    }

    #region Logging

    [LoggerMessage(
        2960,
        LogLevel.Debug,
        "GET /api/v1/playlists - Page: {Page}, PageSize: {PageSize}, SortBy: {SortBy}"
    )]
    private static partial void LogGetPlaylistsRequest(ILogger logger, int page, int pageSize, string sortBy);

    [LoggerMessage(2961, LogLevel.Information, "Retrieved {TotalCount} playlists, returning {PagedCount} items")]
    private static partial void LogGetPlaylistsSuccess(ILogger logger, int totalCount, int pagedCount);

    [LoggerMessage(2962, LogLevel.Error, "Failed to get playlists: {Error}")]
    private static partial void LogGetPlaylistsError(ILogger logger, string error);

    [LoggerMessage(2963, LogLevel.Debug, "GET /api/v1/playlists/{PlaylistId}")]
    private static partial void LogGetPlaylistRequest(ILogger logger, string playlistId);

    [LoggerMessage(2964, LogLevel.Information, "Retrieved playlist '{PlaylistId}' with {TrackCount} tracks")]
    private static partial void LogGetPlaylistSuccess(ILogger logger, string playlistId, int trackCount);

    [LoggerMessage(2965, LogLevel.Error, "Failed to get playlist '{PlaylistId}': {Error}")]
    private static partial void LogGetPlaylistError(ILogger logger, string playlistId, string error);

    [LoggerMessage(2966, LogLevel.Debug, "GET /api/v1/playlists/tracks/{TrackId}/stream-url")]
    private static partial void LogGetStreamUrlRequest(ILogger logger, string trackId);

    [LoggerMessage(2967, LogLevel.Debug, "Retrieved stream URL for track '{TrackId}'")]
    private static partial void LogGetStreamUrlSuccess(ILogger logger, string trackId);

    [LoggerMessage(2968, LogLevel.Error, "Failed to get stream URL for track '{TrackId}': {Error}")]
    private static partial void LogGetStreamUrlError(ILogger logger, string trackId, string error);

    [LoggerMessage(2969, LogLevel.Debug, "POST /api/v1/playlists/test-connection")]
    private static partial void LogTestConnectionRequest(ILogger logger);

    [LoggerMessage(2970, LogLevel.Information, "Subsonic connection test successful")]
    private static partial void LogTestConnectionSuccess(ILogger logger);

    [LoggerMessage(2971, LogLevel.Error, "Subsonic connection test failed: {Error}")]
    private static partial void LogTestConnectionError(ILogger logger, string error);

    #endregion
}

/// <summary>
/// Response model for stream URL requests.
/// </summary>
public record StreamUrlResponse
{
    /// <summary>
    /// Gets the track identifier.
    /// </summary>
    public required string TrackId { get; init; }

    /// <summary>
    /// Gets the streaming URL.
    /// </summary>
    public required string StreamUrl { get; init; }

    /// <summary>
    /// Gets the timestamp when the URL was generated.
    /// </summary>
    public DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Response model for connection test requests.
/// </summary>
public record ConnectionTestResponse
{
    /// <summary>
    /// Gets whether the connection was successful.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// Gets the connection test message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the timestamp when the test was performed.
    /// </summary>
    public DateTime TimestampUtc { get; init; }
}
