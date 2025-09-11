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
using SnapDog2.Api.Models;
using SnapDog2.Application.Services;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Modern zones controller with direct service calls.
/// Eliminates mediator pattern for improved performance and cleaner architecture.
/// </summary>
[ApiController]
[Route("api/v1/zones")]
[Authorize]
[Produces("application/json")]
[Tags("Zones")]
public partial class ZonesController(
    IZoneManager zoneManager,
    IKnxCommandHandler knxCommandHandler,
    ILogger<ZonesController> logger) : ControllerBase
{
    private readonly IZoneManager _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
    private readonly IKnxCommandHandler _knxCommandHandler = knxCommandHandler ?? throw new ArgumentNullException(nameof(knxCommandHandler));
    private readonly ILogger<ZonesController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE LISTING - Direct service calls

    [HttpGet("count")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> GetZonesCount()
    {
        var result = await _zoneManager.GetAllZoneStatesAsync();
        if (result.IsFailure)
        {
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }
        return Ok(result.Value?.Count ?? 0);
    }

    [HttpGet]
    [ProducesResponseType<Page<ZoneState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Page<ZoneState>>> GetZones([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1)
        {
            return BadRequest("Page must be greater than 0");
        }

        if (size < 1 || size > 100)
        {
            return BadRequest("Size must be between 1 and 100");
        }

        var result = await _zoneManager.GetAllZoneStatesAsync();
        if (result.IsFailure)
        {
            LogFailedToGetZones(result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        var zones = result.Value ?? new List<ZoneState>();
        var pagedZones = zones.Skip((page - 1) * size).Take(size).ToArray();
        var pageResult = new Page<ZoneState>(pagedZones, zones.Count, size, page);
        return Ok(pageResult);
    }

    [HttpGet("{zoneIndex:int}")]
    [ProducesResponseType<ZoneState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZoneState>> GetZone(int zoneIndex)
    {
        var result = await _zoneManager.GetZoneStateAsync(zoneIndex);
        if (result.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found: {result.ErrorMessage}");
        }
        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // VOLUME CONTROL - Direct service calls

    [HttpPut("{zoneIndex:int}/volume")]
    [CommandId("VOLUME")]
    [ProducesResponseType<int>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetVolume(int zoneIndex, [FromBody] int volume)
    {
        if (volume < 0 || volume > 100)
        {
            return BadRequest("Volume must be between 0 and 100");
        }

        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.SetVolumeAsync(volume);
        if (result.IsFailure)
        {
            LogFailedToSetZoneVolume(zoneIndex, volume, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }
        return Accepted(volume);
    }

    [HttpGet("{zoneIndex:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetVolume(int zoneIndex)
    {
        var result = await _zoneManager.GetZoneStateAsync(zoneIndex);
        if (result.IsFailure)
        {
            return Ok("Zone " + zoneIndex);
        }

        return Ok(result.Value?.Volume ?? 0);
    }

    [HttpPost("{zoneIndex:int}/volume/up")]
    [CommandId("VOLUME_UP")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VolumeUp(int zoneIndex, [FromQuery] int step = 5)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.VolumeUpAsync(step);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/volume/down")]
    [CommandId("VOLUME_DOWN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VolumeDown(int zoneIndex, [FromQuery] int step = 5)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.VolumeDownAsync(step);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MUTE CONTROL - Direct service calls

    [HttpPut("{zoneIndex:int}/mute")]
    [CommandId("MUTE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMute(int zoneIndex, [FromBody] bool muted)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.SetMuteAsync(muted);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpGet("{zoneIndex:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetMute(int zoneIndex)
    {
        var result = await _zoneManager.GetZoneStateAsync(zoneIndex);
        if (result.IsFailure)
        {
            return Ok("Zone " + zoneIndex);
        }

        return Ok(result.Value?.Mute ?? false);
    }

    [HttpPost("{zoneIndex:int}/mute/toggle")]
    [CommandId("MUTE_TOGGLE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleMute(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.ToggleMuteAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYBACK CONTROL - Direct service calls

    [HttpPost("{zoneIndex:int}/play")]
    [CommandId("PLAY")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Play(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PlayAsync();

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("PLAY", zoneIndex);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/pause")]
    [CommandId("PAUSE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PauseAsync();

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("PAUSE", zoneIndex);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/stop")]
    [CommandId("STOP")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Stop(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.StopAsync();

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("STOP", zoneIndex);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYLIST CONTROL - Direct service calls

    [HttpPut("{zoneIndex:int}/playlist")]
    [CommandId("PLAYLIST")]
    [ProducesResponseType<int>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetPlaylist(int zoneIndex, [FromBody] int playlistIndex)
    {
        if (playlistIndex < 1)
        {
            return BadRequest("Playlist index must be greater than 0");
        }

        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.SetPlaylistAsync(playlistIndex);
        if (result.IsFailure)
        {
            LogFailedToSetZonePlaylist(zoneIndex, playlistIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("PLAYLIST", zoneIndex, playlistIndex);

        return Accepted(playlistIndex);
    }

    [HttpPost("{zoneIndex:int}/next/playlist")]
    [CommandId("PLAYLIST_NEXT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextPlaylist(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.NextPlaylistAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/previous/playlist")]
    [CommandId("PLAYLIST_PREVIOUS")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviousPlaylist(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PreviousPlaylistAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // TRACK CONTROL - Direct service calls

    [HttpPut("{zoneIndex:int}/track")]
    [CommandId("TRACK")]
    [ProducesResponseType<int>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetTrack(int zoneIndex, [FromBody] int trackIndex)
    {
        if (trackIndex < 1)
        {
            return BadRequest("Track index must be greater than 0");
        }

        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PlayTrackAsync(trackIndex);
        return result.IsSuccess ? Accepted(trackIndex) : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/next")]
    [CommandId("TRACK_NEXT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextTrack(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.NextTrackAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/previous")]
    [CommandId("TRACK_PREVIOUS")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviousTrack(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PreviousTrackAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/play/track")]
    [CommandId("TRACK_PLAY_INDEX")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PlayTrack(int zoneIndex, [FromBody] int trackIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PlayTrackAsync(trackIndex);

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("TRACK_PLAY_INDEX", zoneIndex, trackIndex);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/play/url")]
    [CommandId("TRACK_PLAY_URL")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PlayUrl(int zoneIndex, [FromBody] string url)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.PlayUrlAsync(url);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/play/playlist/{playlistIndex:int}/track")]
    [CommandId("TRACK_PLAY_PLAYLIST")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PlayPlaylistTrack(int zoneIndex, int playlistIndex, [FromBody] int trackIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        // First set the playlist
        var playlistResult = await zoneResult.Value.SetPlaylistAsync(playlistIndex);
        if (playlistResult.IsFailure)
        {
            return Problem(playlistResult.ErrorMessage);
        }

        // Then play the track
        var result = await zoneResult.Value.PlayTrackAsync(trackIndex, CancellationToken.None);

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("TRACK_PLAY_PLAYLIST", zoneIndex, new { playlistIndex, trackIndex });

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPut("{zoneIndex:int}/track/position")]
    [CommandId("TRACK_POSITION")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeekPosition(int zoneIndex, [FromBody] long positionMs)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.SeekToPositionAsync(TimeSpan.FromMilliseconds(positionMs));
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPut("{zoneIndex:int}/track/progress")]
    [CommandId("TRACK_PROGRESS")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeekProgress(int zoneIndex, [FromBody] double progress)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);

        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        var result = await zoneResult.Value.SeekToProgressAsync(progress);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPut("{zoneIndex:int}/repeat/track")]
    [CommandId("TRACK_REPEAT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTrackRepeat(int zoneIndex, [FromBody] bool repeat)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.SetTrackRepeatAsync(repeat, CancellationToken.None);

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("TRACK_REPEAT", zoneIndex, repeat);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/repeat/track/toggle")]
    [CommandId("TRACK_REPEAT_TOGGLE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleTrackRepeat(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.ToggleTrackRepeatAsync(CancellationToken.None);

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("TRACK_REPEAT_TOGGLE", zoneIndex);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPut("{zoneIndex:int}/shuffle")]
    [CommandId("PLAYLIST_SHUFFLE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPlaylistShuffle(int zoneIndex, [FromBody] bool shuffle)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.SetPlaylistShuffleAsync(shuffle, CancellationToken.None);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/shuffle/toggle")]
    [CommandId("PLAYLIST_SHUFFLE_TOGGLE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TogglePlaylistShuffle(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.TogglePlaylistShuffleAsync(CancellationToken.None);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPut("{zoneIndex:int}/repeat")]
    [CommandId("PLAYLIST_REPEAT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPlaylistRepeat(int zoneIndex, [FromBody] bool repeat)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.SetPlaylistRepeatAsync(repeat, CancellationToken.None);

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("PLAYLIST_REPEAT", zoneIndex, repeat);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/repeat/toggle")]
    [CommandId("PLAYLIST_REPEAT_TOGGLE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TogglePlaylistRepeat(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value.TogglePlaylistRepeatAsync(CancellationToken.None);

        // Send KNX command
        await _knxCommandHandler.HandleCommandAsync("PLAYLIST_REPEAT_TOGGLE", zoneIndex);

        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/control")]
    [CommandId("CONTROL")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetControl(int zoneIndex, [FromBody] object controlState)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        // Generic control endpoint - for now just validate zone exists and return success
        var result = Result.Success();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // STATUS ENDPOINTS - Blueprint compliance

    [HttpGet("{zoneIndex:int}/name")]
    [StatusId("ZONE_NAME_STATUS")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetZoneName(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Name ?? "");
    }

    [HttpGet("{zoneIndex:int}/track/title")]
    [StatusId("TRACK_METADATA_TITLE")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetTrackTitle(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.Title ?? "");
    }

    [HttpGet("{zoneIndex:int}/track/playing")]
    [StatusId("TRACK_PLAYING_STATUS")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> GetTrackPlayingStatus(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.PlaybackState == PlaybackState.Playing);
    }

    [HttpGet("{zoneIndex:int}/playlist/name")]
    [StatusId("PLAYLIST_NAME_STATUS")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetPlaylistName(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Playlist?.Name ?? "");
    }

    [HttpGet("{zoneIndex:int}/playback")]
    [StatusId("PLAYBACK_STATE")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetPlaybackState(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.PlaybackState.ToString() ?? "Unknown");
    }

    [HttpGet("{zoneIndex:int}/track")]
    [StatusId("TRACK_STATUS")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTrackStatus(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track);
    }

    [HttpGet("{zoneIndex:int}/track/metadata")]
    [StatusId("TRACK_METADATA")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTrackMetadata(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track);
    }

    [HttpGet("{zoneIndex:int}/track/duration")]
    [StatusId("TRACK_METADATA_DURATION")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    public async Task<ActionResult<long>> GetTrackDuration(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.DurationMs ?? 0);
    }

    [HttpGet("{zoneIndex:int}/track/artist")]
    [StatusId("TRACK_METADATA_ARTIST")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetTrackArtist(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.Artist ?? "");
    }

    [HttpGet("{zoneIndex:int}/track/album")]
    [StatusId("TRACK_METADATA_ALBUM")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetTrackAlbum(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.Album ?? "");
    }

    [HttpGet("{zoneIndex:int}/track/cover")]
    [StatusId("TRACK_METADATA_COVER")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetTrackCover(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.CoverArtUrl ?? "");
    }

    [HttpGet("{zoneIndex:int}/track/position")]
    [StatusId("TRACK_POSITION_STATUS")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    public async Task<ActionResult<long>> GetTrackPosition(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.PositionMs ?? 0);
    }

    [HttpGet("{zoneIndex:int}/track/progress")]
    [StatusId("TRACK_PROGRESS_STATUS")]
    [ProducesResponseType<float>(StatusCodes.Status200OK)]
    public async Task<ActionResult<float>> GetTrackProgress(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Track?.Progress ?? 0.0f);
    }

    [HttpGet("{zoneIndex:int}/repeat/track")]
    [StatusId("TRACK_REPEAT_STATUS")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> GetTrackRepeat(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.TrackRepeat ?? false);
    }

    [HttpGet("{zoneIndex:int}/playlist")]
    [StatusId("PLAYLIST_STATUS")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPlaylistStatus(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Playlist);
    }

    [HttpGet("{zoneIndex:int}/playlist/info")]
    [StatusId("PLAYLIST_INFO")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPlaylistInfo(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Playlist);
    }

    [HttpGet("{zoneIndex:int}/playlist/count")]
    [StatusId("PLAYLIST_COUNT_STATUS")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetPlaylistCount(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.Playlist?.TrackCount ?? 0);
    }

    [HttpGet("{zoneIndex:int}/shuffle")]
    [StatusId("PLAYLIST_SHUFFLE_STATUS")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> GetPlaylistShuffle(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok(zoneResult.Value?.PlaylistShuffle ?? false);
    }

    [HttpGet("{zoneIndex:int}/repeat")]
    [StatusId("PLAYLIST_REPEAT_STATUS")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetPlaylistRepeat(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex, CancellationToken.None);
        if (zoneResult.IsFailure || zoneResult.Value == null)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }
        return Ok("None"); // PlaylistInfo doesn't have repeat mode
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGER METHODS

    [LoggerMessage(EventId = 113500, Level = LogLevel.Warning, Message = "Failed to get zones: {ErrorMessage}")]
    private partial void LogFailedToGetZones(string errorMessage);

    [LoggerMessage(EventId = 113501, Level = LogLevel.Warning, Message = "Failed to set zone {ZoneIndex} volume to {Volume}: {ErrorMessage}")]
    private partial void LogFailedToSetZoneVolume(int zoneIndex, int volume, string errorMessage);

    [LoggerMessage(EventId = 113502, Level = LogLevel.Warning, Message = "Failed to set zone {ZoneIndex} playlist to {PlaylistIndex}: {ErrorMessage}")]
    private partial void LogFailedToSetZonePlaylist(int zoneIndex, int playlistIndex, string errorMessage);
}
