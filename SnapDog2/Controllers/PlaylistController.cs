namespace SnapDog2.Controllers;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for playlist and track management operations.
/// </summary>
[ApiController]
[Route("api/playlists")]
[Produces("application/json")]
public class PlaylistController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlaylistController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistController"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    public PlaylistController(IServiceProvider serviceProvider, ILogger<PlaylistController> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Gets all available playlists.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of all playlists.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlaylistInfo>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<PlaylistInfo>>> GetAllPlaylists(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogDebug("Getting all playlists");

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetAllPlaylistsQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetAllPlaylistsQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetAllPlaylistsQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(result.Value);
            }

            this._logger.LogWarning("Failed to get all playlists: {Error}", result.ErrorMessage);
            return this.StatusCode(500, new { error = result.ErrorMessage ?? "Failed to retrieve playlists" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting all playlists");
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets tracks for a specific playlist by ID.
    /// </summary>
    /// <param name="playlistId">The playlist ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of tracks in the playlist.</returns>
    [HttpGet("{playlistId}/tracks")]
    [ProducesResponseType(typeof(IEnumerable<TrackInfo>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<TrackInfo>>> GetPlaylistTracksByPlaylistId(
        string playlistId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting tracks for playlist {PlaylistId}", playlistId);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetPlaylistTracksQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(
                new GetPlaylistTracksQuery { PlaylistId = playlistId },
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(result.Value);
            }

            this._logger.LogWarning(
                "Failed to get tracks for playlist {PlaylistId}: {Error}",
                playlistId,
                result.ErrorMessage
            );
            return this.NotFound(new { error = result.ErrorMessage ?? "Playlist not found" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting tracks for playlist {PlaylistId}", playlistId);
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets tracks for a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of tracks in the playlist.</returns>
    [HttpGet("by-index/{playlistIndex:int}/tracks")]
    [ProducesResponseType(typeof(IEnumerable<TrackInfo>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<TrackInfo>>> GetPlaylistTracksByIndex(
        [Range(1, int.MaxValue)] int playlistIndex,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogDebug("Getting tracks for playlist index {PlaylistIndex}", playlistIndex);

            var handler =
                this._serviceProvider.GetService<Server.Features.Zones.Handlers.GetPlaylistTracksQueryHandler>();
            if (handler == null)
            {
                this._logger.LogError("GetPlaylistTracksQueryHandler not found in DI container");
                return this.StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(
                new GetPlaylistTracksQuery { PlaylistIndex = playlistIndex },
                cancellationToken
            );

            if (result.IsSuccess && result.Value != null)
            {
                return this.Ok(result.Value);
            }

            this._logger.LogWarning(
                "Failed to get tracks for playlist index {PlaylistIndex}: {Error}",
                playlistIndex,
                result.ErrorMessage
            );
            return this.NotFound(new { error = result.ErrorMessage ?? "Playlist not found" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting tracks for playlist index {PlaylistIndex}", playlistIndex);
            return this.StatusCode(500, new { error = "Internal server error" });
        }
    }
}
