namespace SnapDog2.Controllers;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for playlist and track management operations.
/// Follows CQRS pattern using Cortex.Mediator for enterprise-grade architecture compliance.
/// </summary>
[ApiController]
[Route("api/playlists")]
[Produces("application/json")]
public partial class PlaylistController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PlaylistController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistController"/> class.
    /// </summary>
    /// <param name="mediator">The Cortex.Mediator instance for CQRS command/query dispatch.</param>
    /// <param name="logger">The logger instance.</param>
    public PlaylistController(IMediator mediator, ILogger<PlaylistController> logger)
    {
        this._mediator = mediator;
        this._logger = logger;
    }

    /// <summary>
    /// Gets all available playlists.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of all playlists wrapped in ApiResponse.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PlaylistInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PlaylistInfo>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PlaylistInfo>>>> GetAllPlaylists(
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingAllPlaylists();

            var query = new GetAllPlaylistsQuery();
            var result = await this._mediator.SendQueryAsync<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<PlaylistInfo>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetAllPlaylists(result.ErrorMessage);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<PlaylistInfo>>.CreateError(
                    "PLAYLISTS_ERROR",
                    result.ErrorMessage ?? "Failed to retrieve playlists"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingAllPlaylists(ex);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<PlaylistInfo>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Gets tracks for a specific playlist by ID.
    /// </summary>
    /// <param name="playlistId">The playlist ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of tracks in the playlist wrapped in ApiResponse.</returns>
    [HttpGet("{playlistId}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackInfo>>), 404)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackInfo>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TrackInfo>>>> GetPlaylistTracksByPlaylistId(
        string playlistId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingPlaylistTracksById(playlistId);

            var query = new GetPlaylistTracksQuery { PlaylistId = playlistId };
            var result = await this._mediator.SendQueryAsync<GetPlaylistTracksQuery, Result<List<TrackInfo>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<TrackInfo>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetPlaylistTracksById(playlistId, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<IEnumerable<TrackInfo>>.CreateError(
                    "PLAYLIST_NOT_FOUND",
                    result.ErrorMessage ?? "Playlist not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingPlaylistTracksById(ex, playlistId);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<TrackInfo>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Gets tracks for a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of tracks in the playlist wrapped in ApiResponse.</returns>
    [HttpGet("by-index/{playlistIndex:int}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackInfo>>), 404)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackInfo>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TrackInfo>>>> GetPlaylistTracksByIndex(
        [Range(1, int.MaxValue)] int playlistIndex,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogGettingPlaylistTracksByIndex(playlistIndex);

            var query = new GetPlaylistTracksQuery { PlaylistIndex = playlistIndex };
            var result = await this._mediator.SendQueryAsync<GetPlaylistTracksQuery, Result<List<TrackInfo>>>(
                query,
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(ApiResponse<IEnumerable<TrackInfo>>.CreateSuccess(result.Value));
            }

            this.LogFailedToGetPlaylistTracksByIndex(playlistIndex, result.ErrorMessage);
            return this.NotFound(
                ApiResponse<IEnumerable<TrackInfo>>.CreateError(
                    "PLAYLIST_NOT_FOUND",
                    result.ErrorMessage ?? "Playlist not found"
                )
            );
        }
        catch (Exception ex)
        {
            this.LogErrorGettingPlaylistTracksByIndex(ex, playlistIndex);
            return this.StatusCode(
                500,
                ApiResponse<IEnumerable<TrackInfo>>.CreateError(
                    "INTERNAL_ERROR",
                    "An internal server error occurred",
                    ex.Message
                )
            );
        }
    }

    // LoggerMessage definitions for high-performance logging (ID range: 2796-2804)

    // Playlist operations
    [LoggerMessage(EventId = 2796, Level = LogLevel.Debug, Message = "Getting all playlists via CQRS mediator")]
    private partial void LogGettingAllPlaylists();

    [LoggerMessage(EventId = 2797, Level = LogLevel.Warning, Message = "Failed to get all playlists: {error}")]
    private partial void LogFailedToGetAllPlaylists(string? error);

    [LoggerMessage(EventId = 2798, Level = LogLevel.Error, Message = "Error getting all playlists")]
    private partial void LogErrorGettingAllPlaylists(Exception ex);

    // Playlist tracks by ID operations
    [LoggerMessage(
        EventId = 2799,
        Level = LogLevel.Debug,
        Message = "Getting tracks for playlist {playlistId} via CQRS mediator"
    )]
    private partial void LogGettingPlaylistTracksById(string playlistId);

    [LoggerMessage(
        EventId = 2800,
        Level = LogLevel.Warning,
        Message = "Failed to get tracks for playlist {playlistId}: {error}"
    )]
    private partial void LogFailedToGetPlaylistTracksById(string playlistId, string? error);

    [LoggerMessage(EventId = 2801, Level = LogLevel.Error, Message = "Error getting tracks for playlist {playlistId}")]
    private partial void LogErrorGettingPlaylistTracksById(Exception ex, string playlistId);

    // Playlist tracks by index operations
    [LoggerMessage(
        EventId = 2802,
        Level = LogLevel.Debug,
        Message = "Getting tracks for playlist index {playlistIndex} via CQRS mediator"
    )]
    private partial void LogGettingPlaylistTracksByIndex(int playlistIndex);

    [LoggerMessage(
        EventId = 2803,
        Level = LogLevel.Warning,
        Message = "Failed to get tracks for playlist index {playlistIndex}: {error}"
    )]
    private partial void LogFailedToGetPlaylistTracksByIndex(int playlistIndex, string? error);

    [LoggerMessage(
        EventId = 2804,
        Level = LogLevel.Error,
        Message = "Error getting tracks for playlist index {playlistIndex}"
    )]
    private partial void LogErrorGettingPlaylistTracksByIndex(Exception ex, int playlistIndex);
}
