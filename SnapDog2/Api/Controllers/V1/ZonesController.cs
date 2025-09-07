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
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
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
public partial class ZonesController(IZoneManager zoneManager, ILogger<ZonesController> logger) : ControllerBase
{
    private readonly IZoneManager _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
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
        return Ok(result.Value!.Count);
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

        var zones = result.Value!;
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
            return NotFound($"Zone {zoneIndex} not found");
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
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.SetVolumeAsync(volume);
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
            return NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Volume);
    }

    [HttpPost("{zoneIndex:int}/volume/up")]
    [CommandId("VOLUME_UP")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VolumeUp(int zoneIndex, [FromQuery] int step = 5)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.VolumeUpAsync(step);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/volume/down")]
    [CommandId("VOLUME_DOWN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VolumeDown(int zoneIndex, [FromQuery] int step = 5)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.VolumeDownAsync(step);
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
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.SetMuteAsync(muted);
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
            return NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Mute);
    }

    [HttpPost("{zoneIndex:int}/mute/toggle")]
    [CommandId("MUTE_TOGGLE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleMute(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.ToggleMuteAsync();
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
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.PlayAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/pause")]
    [CommandId("PAUSE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.PauseAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/stop")]
    [CommandId("STOP")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Stop(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.StopAsync();
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
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.SetPlaylistAsync(playlistIndex);
        if (result.IsFailure)
        {
            LogFailedToSetZonePlaylist(zoneIndex, playlistIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }
        return Accepted(playlistIndex);
    }

    [HttpPost("{zoneIndex:int}/playlist/next")]
    [CommandId("PLAYLIST_NEXT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextPlaylist(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.NextPlaylistAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/playlist/previous")]
    [CommandId("PLAYLIST_PREVIOUS")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviousPlaylist(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.PreviousPlaylistAsync();
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
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.SetTrackAsync(trackIndex);
        return result.IsSuccess ? Accepted(trackIndex) : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/track/next")]
    [CommandId("TRACK_NEXT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextTrack(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.NextTrackAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/track/previous")]
    [CommandId("TRACK_PREVIOUS")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviousTrack(int zoneIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.PreviousTrackAsync();
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/track/play")]
    [CommandId("TRACK_PLAY")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PlayTrack(int zoneIndex, [FromBody] int trackIndex)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.PlayTrackAsync(trackIndex);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/seek/position")]
    [CommandId("TRACK_SEEK_POSITION")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeekPosition(int zoneIndex, [FromBody] long positionMs)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.SeekToPositionAsync(TimeSpan.FromMilliseconds(positionMs));
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
    }

    [HttpPost("{zoneIndex:int}/seek/progress")]
    [CommandId("TRACK_SEEK_PROGRESS")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeekProgress(int zoneIndex, [FromBody] double progress)
    {
        var zoneResult = await _zoneManager.GetZoneAsync(zoneIndex);
        if (zoneResult.IsFailure)
        {
            return NotFound($"Zone {zoneIndex} not found");
        }

        var result = await zoneResult.Value!.SeekToProgressAsync(progress);
        return result.IsSuccess ? NoContent() : Problem(result.ErrorMessage);
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
