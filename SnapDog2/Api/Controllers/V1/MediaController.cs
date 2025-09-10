namespace SnapDog2.Api.Controllers.V1;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;

/// <summary>
/// Media status controller for blueprint compliance.
/// </summary>
[ApiController]
[Route("api/v1/media")]
[Authorize]
[Produces("application/json")]
[Tags("Media")]
public partial class MediaController : ControllerBase
{
    private readonly IPlaylistManager _playlistManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaController"/> class.
    /// </summary>
    /// <param name="playlistManager">The playlist manager.</param>
    public MediaController(IPlaylistManager playlistManager)
    {
        _playlistManager = playlistManager;
    }

    /// <summary>
    /// Gets available media playlists.
    /// </summary>
    [HttpGet("playlists")]
    [StatusId("MEDIA_PLAYLISTS")]
    [ProducesResponseType<object[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object[]>> GetPlaylists()
    {
        var result = await _playlistManager.GetAllPlaylistsAsync();
        if (result.IsFailure)
        {
            return Problem(result.ErrorMessage);
        }
        return Ok(result.Value?.ToArray() ?? Array.Empty<object>());
    }

    /// <summary>
    /// Gets playlist information.
    /// </summary>
    [HttpGet("playlists/{playlistIndex}")]
    [StatusId("MEDIA_PLAYLIST_INFO")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPlaylistInfo(string playlistIndex)
    {
        if (!int.TryParse(playlistIndex, out var index))
        {
            return NotFound($"Invalid playlist index: {playlistIndex}");
        }

        var playlistResult = await _playlistManager.GetPlaylistAsync(index);
        if (playlistResult.IsFailure)
        {
            return NotFound($"Playlist {playlistIndex} not found");
        }

        var tracksResult = await _playlistManager.GetPlaylistTracksAsync(index);
        if (tracksResult.IsFailure)
        {
            return NotFound($"Playlist {playlistIndex} tracks not found");
        }

        return Ok(new
        {
            info = playlistResult.Value,
            tracks = tracksResult.Value
        });
    }

    /// <summary>
    /// Gets playlist tracks.
    /// </summary>
    [HttpGet("playlists/{playlistIndex}/tracks")]
    [StatusId("MEDIA_PLAYLIST_TRACKS")]
    [ProducesResponseType<object[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object[]>> GetPlaylistTracks(string playlistIndex)
    {
        if (!int.TryParse(playlistIndex, out var index))
        {
            return NotFound($"Invalid playlist index: {playlistIndex}");
        }

        var result = await _playlistManager.GetPlaylistTracksAsync(index);
        if (result.IsFailure)
        {
            return NotFound($"Playlist {playlistIndex} not found");
        }
        return Ok(result.Value?.ToArray() ?? Array.Empty<object>());
    }

    /// <summary>
    /// Gets playlist track information.
    /// </summary>
    [HttpGet("playlists/{playlistIndex}/tracks/{trackIndex}")]
    [StatusId("MEDIA_PLAYLIST_TRACK_INFO")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPlaylistTrackInfo(string playlistIndex, string trackIndex)
    {
        if (!int.TryParse(playlistIndex, out var pIndex) || !int.TryParse(trackIndex, out var tIndex))
        {
            return NotFound($"Invalid playlist or track index");
        }

        var result = await _playlistManager.GetPlaylistTracksAsync(pIndex);
        if (result.IsFailure || result.Value == null)
        {
            return NotFound($"Playlist {playlistIndex} not found");
        }

        var tracks = result.Value.ToArray();
        if (tIndex < 1 || tIndex > tracks.Length)
        {
            return NotFound($"Track {trackIndex} not found in playlist {playlistIndex}");
        }

        var track = tracks[tIndex - 1]; // Convert 1-based to 0-based
        return Ok(track);
    }

    /// <summary>
    /// Gets track information.
    /// </summary>
    [HttpGet("tracks/{trackIndex}")]
    [StatusId("MEDIA_TRACK_INFO")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTrackInfo(string trackIndex)
    {
        // Search through all playlists for the track
        var playlistsResult = await _playlistManager.GetAllPlaylistsAsync();
        if (playlistsResult.IsFailure || playlistsResult.Value == null)
        {
            return NotFound($"Track {trackIndex} not found");
        }

        foreach (var playlist in playlistsResult.Value)
        {
            if (playlist.Index.HasValue)
            {
                var tracksResult = await _playlistManager.GetPlaylistTracksAsync(playlist.Index.Value);
                if (tracksResult.IsSuccess && tracksResult.Value != null)
                {
                    var track = tracksResult.Value.FirstOrDefault(t =>
                        t.Title == trackIndex || (t.Url != null && t.Url.Contains(trackIndex)));
                    if (track != null)
                    {
                        return Ok(track);
                    }
                }
            }
        }

        return NotFound($"Track {trackIndex} not found");
    }
}
