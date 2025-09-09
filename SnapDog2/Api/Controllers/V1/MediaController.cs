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
    private readonly ISubsonicService _subsonicService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaController"/> class.
    /// </summary>
    /// <param name="subsonicService">The Subsonic service.</param>
    public MediaController(ISubsonicService subsonicService)
    {
        _subsonicService = subsonicService;
    }

    /// <summary>
    /// Gets available media playlists.
    /// </summary>
    [HttpGet("playlists")]
    [StatusId("MEDIA_PLAYLISTS")]
    [ProducesResponseType<object[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object[]>> GetPlaylists()
    {
        var result = await _subsonicService.GetPlaylistsAsync(CancellationToken.None);
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
        var result = await _subsonicService.GetPlaylistAsync(playlistIndex, CancellationToken.None);
        if (result.IsFailure)
        {
            return NotFound($"Playlist {playlistIndex} not found");
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets playlist tracks.
    /// </summary>
    [HttpGet("playlists/{playlistIndex}/tracks")]
    [StatusId("MEDIA_PLAYLIST_TRACKS")]
    [ProducesResponseType<object[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object[]>> GetPlaylistTracks(string playlistIndex)
    {
        var result = await _subsonicService.GetPlaylistAsync(playlistIndex, CancellationToken.None);
        if (result.IsFailure)
        {
            return NotFound($"Playlist {playlistIndex} not found");
        }
        return Ok(result.Value?.Tracks?.ToArray() ?? Array.Empty<object>());
    }

    /// <summary>
    /// Gets playlist track information.
    /// </summary>
    [HttpGet("playlists/{playlistIndex}/tracks/{trackIndex}")]
    [StatusId("MEDIA_PLAYLIST_TRACK_INFO")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPlaylistTrackInfo(string playlistIndex, string trackIndex)
    {
        var playlistResult = await _subsonicService.GetPlaylistAsync(playlistIndex, CancellationToken.None);
        if (playlistResult.IsFailure)
        {
            return NotFound($"Playlist {playlistIndex} not found");
        }

        var track = playlistResult.Value?.Tracks?.FirstOrDefault(t => t.Title == trackIndex);
        if (track == null)
        {
            return NotFound($"Track {trackIndex} not found in playlist {playlistIndex}");
        }

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
        // Try to find track in playlists since there's no direct GetTrack method
        var playlistsResult = await _subsonicService.GetPlaylistsAsync(CancellationToken.None);
        if (playlistsResult.IsFailure || playlistsResult.Value == null)
        {
            return NotFound($"Track {trackIndex} not found");
        }

        // Search through all playlists for the track
        foreach (var playlist in playlistsResult.Value)
        {
            var playlistResult = await _subsonicService.GetPlaylistAsync(playlist.SubsonicPlaylistId, CancellationToken.None);
            if (playlistResult.IsSuccess && playlistResult.Value != null && playlistResult.Value.Tracks != null)
            {
                var track = playlistResult.Value.Tracks.FirstOrDefault(t =>
                    t.Title == trackIndex || (t.Url != null && t.Url.Contains(trackIndex)));
                if (track != null)
                {
                    return Ok(track);
                }
            }
        }

        return NotFound($"Track {trackIndex} not found");
    }
}
