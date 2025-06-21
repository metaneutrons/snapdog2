using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Playlists management endpoints for creating, configuring, and managing music playlists.
/// Provides CRUD operations and playlist management functionality for the SnapDog2 multi-room audio system.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class PlaylistsController : ApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistsController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR instance for handling commands and queries.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    public PlaylistsController(IMediator mediator, ILogger<PlaylistsController> logger)
        : base(mediator, logger) { }

    /// <summary>
    /// Gets all playlists in the system.
    /// </summary>
    /// <param name="includePrivate">Whether to include private playlists in the results.</param>
    /// <param name="includeSystem">Whether to include system playlists in the results.</param>
    /// <param name="owner">Filter playlists by owner (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all playlists.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PlaylistResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<IEnumerable<PlaylistResponse>>> GetAllPlaylists(
        [FromQuery] bool includePrivate = false,
        [FromQuery] bool includeSystem = true,
        [FromQuery] string? owner = null,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation(
            "Getting all playlists with filters: includePrivate={IncludePrivate}, includeSystem={IncludeSystem}, owner={Owner}",
            includePrivate,
            includeSystem,
            owner
        );

        // TODO: Implement GetAllPlaylistsQuery when Server layer features are created
        // var query = new GetAllPlaylistsQuery
        // {
        //     IncludePrivate = includePrivate,
        //     IncludeSystem = includeSystem,
        //     Owner = owner
        // };
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return empty list
        var emptyPlaylists = new List<PlaylistResponse>();
        return SuccessResponse(emptyPlaylists.AsEnumerable());
    }

    /// <summary>
    /// Gets a specific playlist by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the playlist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The playlist with the specified ID.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<PlaylistResponse>> GetPlaylistById(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Playlist ID cannot be empty."));
        }

        Logger.LogInformation("Getting playlist by ID: {PlaylistId}", id);

        // TODO: Implement GetPlaylistByIdQuery when Server layer features are created
        // var query = new GetPlaylistByIdQuery(id);
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return not found
        return NotFound(ApiResponse<PlaylistResponse>.Fail("Playlist not found."));
    }

    /// <summary>
    /// Gets all tracks in a specific playlist.
    /// </summary>
    /// <param name="id">The unique identifier of the playlist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tracks in the specified playlist.</returns>
    [HttpGet("{id}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrackResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<IEnumerable<TrackResponse>>> GetPlaylistTracks(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<IEnumerable<TrackResponse>>.Fail("Playlist ID cannot be empty."));
        }

        Logger.LogInformation("Getting tracks for playlist: {PlaylistId}", id);

        // TODO: Implement GetPlaylistTracksQuery when Server layer features are created
        // var query = new GetPlaylistTracksQuery(id);
        // return await HandleRequestAsync(query, cancellationToken);

        // Temporary implementation - return empty list
        var emptyTracks = new List<TrackResponse>();
        return SuccessResponse(emptyTracks.AsEnumerable());
    }

    /// <summary>
    /// Creates a new playlist.
    /// </summary>
    /// <param name="request">The playlist creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created playlist.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<PlaylistResponse>> CreatePlaylist(
        [FromBody] CreatePlaylistRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Request body cannot be null."));
        }

        Logger.LogInformation("Creating new playlist: {PlaylistName}", request.Name);

        // TODO: Implement CreatePlaylistCommand when Server layer features are created
        // var command = new CreatePlaylistCommand(request.Name, request.Description)
        // {
        //     Owner = request.Owner ?? HttpContext.User?.Identity?.Name ?? "API",
        //     IsPublic = request.IsPublic ?? true,
        //     Tags = request.Tags,
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // var result = await HandleRequestAsync(command, cancellationToken);
        // if (result.Value != null && result.Value.Success)
        // {
        //     var createdPlaylist = result.Value.Data;
        //     return CreatedAtAction(nameof(GetPlaylistById), new { id = createdPlaylist.Id }, result.Value);
        // }
        // return result;

        // Temporary implementation - return bad request
        return BadRequest(ApiResponse<PlaylistResponse>.Fail("Playlist creation not yet implemented."));
    }

    /// <summary>
    /// Updates an existing playlist.
    /// </summary>
    /// <param name="id">The unique identifier of the playlist to update.</param>
    /// <param name="request">The playlist update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated playlist.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<PlaylistResponse>> UpdatePlaylist(
        string id,
        [FromBody] UpdatePlaylistRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Playlist ID cannot be empty."));
        }

        if (request == null)
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Request body cannot be null."));
        }

        Logger.LogInformation("Updating playlist: {PlaylistId}", id);

        // TODO: Implement UpdatePlaylistCommand when Server layer features are created
        // var command = new UpdatePlaylistCommand(id)
        // {
        //     Name = request.Name,
        //     Description = request.Description,
        //     IsPublic = request.IsPublic,
        //     Tags = request.Tags,
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return not found
        return NotFound(ApiResponse<PlaylistResponse>.Fail("Playlist not found."));
    }

    /// <summary>
    /// Deletes a playlist.
    /// </summary>
    /// <param name="id">The unique identifier of the playlist to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the playlist was deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), 204)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse> DeletePlaylist(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse.Fail("Playlist ID cannot be empty."));
        }

        Logger.LogInformation("Deleting playlist: {PlaylistId}", id);

        // TODO: Implement DeletePlaylistCommand when Server layer features are created
        // var command = new DeletePlaylistCommand(id)
        // {
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // var result = await HandleRequestAsync(command, cancellationToken);
        // if (result.Value?.Success == true)
        // {
        //     return NoContent();
        // }
        // return result;

        // Temporary implementation - return not found
        return NotFound(ApiResponse.Fail("Playlist not found."));
    }

    /// <summary>
    /// Adds a track to a playlist.
    /// </summary>
    /// <param name="id">The unique identifier of the playlist.</param>
    /// <param name="request">The add track request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the track was added.</returns>
    [HttpPost("{id}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<PlaylistResponse>> AddTrackToPlaylist(
        string id,
        [FromBody] AddTrackRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Playlist ID cannot be empty."));
        }

        if (request == null)
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Request body cannot be null."));
        }

        if (string.IsNullOrWhiteSpace(request.TrackId))
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Track ID cannot be empty."));
        }

        Logger.LogInformation(
            "Adding track {TrackId} to playlist {PlaylistId} at position {Position}",
            request.TrackId,
            id,
            request.Position
        );

        // TODO: Implement AddTrackToPlaylistCommand when Server layer features are created
        // var command = new AddTrackToPlaylistCommand(id, request.TrackId)
        // {
        //     Position = request.Position,
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return not found
        return NotFound(ApiResponse<PlaylistResponse>.Fail("Playlist not found."));
    }

    /// <summary>
    /// Removes a track from a playlist.
    /// </summary>
    /// <param name="id">The unique identifier of the playlist.</param>
    /// <param name="trackId">The unique identifier of the track to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response if the track was removed.</returns>
    [HttpDelete("{id}/tracks/{trackId}")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public ActionResult<ApiResponse<PlaylistResponse>> RemoveTrackFromPlaylist(
        string id,
        string trackId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Playlist ID cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(trackId))
        {
            return BadRequest(ApiResponse<PlaylistResponse>.Fail("Track ID cannot be empty."));
        }

        Logger.LogInformation("Removing track {TrackId} from playlist {PlaylistId}", trackId, id);

        // TODO: Implement RemoveTrackFromPlaylistCommand when Server layer features are created
        // var command = new RemoveTrackFromPlaylistCommand(id, trackId)
        // {
        //     RequestedBy = HttpContext.User?.Identity?.Name ?? "API"
        // };
        // return await HandleRequestAsync(command, cancellationToken);

        // Temporary implementation - return not found
        return NotFound(ApiResponse<PlaylistResponse>.Fail("Playlist or track not found."));
    }
}

/// <summary>
/// Response model for playlist operations.
/// </summary>
public class PlaylistResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the playlist.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the playlist.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the owner/creator of the playlist.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is a system playlist.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Gets or sets additional tags associated with the playlist.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the number of tracks in the playlist.
    /// </summary>
    public int TrackCount { get; set; }

    /// <summary>
    /// Gets or sets the total estimated duration in seconds.
    /// </summary>
    public int? TotalDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the playlist was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the playlist was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the playlist was last played.
    /// </summary>
    public DateTime? LastPlayedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times the playlist has been played.
    /// </summary>
    public int PlayCount { get; set; }

    /// <summary>
    /// Creates a PlaylistResponse from a Playlist entity.
    /// </summary>
    /// <param name="playlist">The playlist entity.</param>
    /// <returns>A new PlaylistResponse instance.</returns>
    public static PlaylistResponse FromEntity(Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        return new PlaylistResponse
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            Owner = playlist.Owner,
            IsPublic = playlist.IsPublic,
            IsSystem = playlist.IsSystem,
            Tags = playlist.Tags,
            TrackCount = playlist.TrackCount,
            TotalDurationSeconds = playlist.TotalDurationSeconds,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt,
            LastPlayedAt = playlist.LastPlayedAt,
            PlayCount = playlist.PlayCount,
        };
    }
}

/// <summary>
/// Response model for track operations.
/// </summary>
public class TrackResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the track.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the track.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the artist of the track.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Gets or sets the album of the track.
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets the duration of the track in seconds.
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the file path or URL of the track.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the track was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates a TrackResponse from a Track entity.
    /// </summary>
    /// <param name="track">The track entity.</param>
    /// <returns>A new TrackResponse instance.</returns>
    public static TrackResponse FromEntity(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return new TrackResponse
        {
            Id = track.Id,
            Title = track.Title,
            Artist = track.Artist,
            Album = track.Album,
            DurationSeconds = track.DurationSeconds,
            FilePath = track.FilePath,
            CreatedAt = track.CreatedAt,
        };
    }
}

/// <summary>
/// Request model for creating a new playlist.
/// </summary>
public class CreatePlaylistRequest
{
    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the playlist.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the owner/creator of the playlist.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Gets or sets additional tags associated with the playlist.
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// Request model for updating an existing playlist.
/// </summary>
public class UpdatePlaylistRequest
{
    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the playlist.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Gets or sets additional tags associated with the playlist.
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// Request model for adding a track to a playlist.
/// </summary>
public class AddTrackRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the track to add.
    /// </summary>
    public required string TrackId { get; set; }

    /// <summary>
    /// Gets or sets the position to insert the track at (optional, defaults to end of playlist).
    /// </summary>
    public int? Position { get; set; }
}
