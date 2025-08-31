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

using System.Linq;
using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Server.Zones.Commands.Control;
using SnapDog2.Server.Zones.Commands.Playback;
using SnapDog2.Server.Zones.Commands.Playlist;
using SnapDog2.Server.Zones.Commands.Track;
using SnapDog2.Server.Zones.Commands.Volume;
using SnapDog2.Server.Zones.Queries;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

/// <summary>
/// Modern simplified zones controller with direct primitive responses.
///
/// MODERN API DESIGN PRINCIPLES:
/// - Return primitives directly (int, bool, string) instead of wrapper objects
/// - Use direct parameter binding instead of single-property request objects
/// - Minimal DTOs only for complex multi-property requests
/// - Smart HTTP semantics: 200 for data, 204 for actions, proper error codes
/// - Perfect consistency with ultra-modern API design patterns
/// </summary>
[ApiController]
[Route("api/v1/zones")]
[Produces("application/json")]
[Tags("Zones")]
public partial class ZonesController(IMediator mediator, ILogger<ZonesController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<ZonesController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE LISTING - Clean paginated API with real data via mediator
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lists configured zones with clean pagination.
    /// Returns zone state format for direct consumption.
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetZonesCount()
    {
        var query = new GetZonesCountQuery();
        var result = await _mediator.SendQueryAsync<GetZonesCountQuery, Result<int>>(query);

        if (result.IsFailure)
        {
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType<Page<ZoneState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Page<ZoneState>>> GetZones([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1)
        {
            return this.BadRequest("Page must be greater than 0");
        }

        if (size < 1 || size > 100)
        {
            return this.BadRequest("Size must be between 1 and 100");
        }

        var query = new GetAllZonesQuery();
        var result = await this._mediator.SendQueryAsync<GetAllZonesQuery, Result<List<ZoneState>>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZones(result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        var zones = result.Value!;
        var pagedZones = zones.Skip((page - 1) * size).Take(size).ToArray();
        var pageResult = new Page<ZoneState>(pagedZones, zones.Count, size, page);

        return this.Ok(pageResult);
    }

    /// <summary>
    /// Get detailed information for a specific zone.
    /// Returns zone state format for direct consumption.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Zone state information</returns>
    [HttpGet("{zoneIndex:int}")]
    [ProducesResponseType<ZoneState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZoneState>> GetZone(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // VOLUME CONTROL - Direct primitive responses for maximum simplicity
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set zone volume level.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <returns>New volume level</returns>
    [HttpPut("{zoneIndex:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetVolume(int zoneIndex, [FromBody] int volume)
    {
        if (volume < 0 || volume > 100)
        {
            return this.BadRequest("Volume must be between 0 and 100");
        }

        var command = new SetZoneVolumeCommand { ZoneIndex = zoneIndex, Volume = volume };
        var result = await this._mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneVolume(zoneIndex, volume, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(volume);
    }

    /// <summary>
    /// Get current zone volume level.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current volume level (0-100)</returns>
    [HttpGet("{zoneIndex:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetVolume(int zoneIndex)
    {
        var query = new GetZoneVolumeQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneVolumeQuery, Result<int>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneVolume(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Increase zone volume by specified step.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="step">Volume increase step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{zoneIndex:int}/volume/up")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeUp(int zoneIndex, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
        {
            return this.BadRequest("Step must be between 1 and 50");
        }

        var command = new VolumeUpCommand { ZoneIndex = zoneIndex, Step = step };
        var result = await this._mediator.SendCommandAsync<VolumeUpCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToIncreaseZoneVolume(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(step);
    }

    /// <summary>
    /// Decrease zone volume by specified step.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="step">Volume decrease step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{zoneIndex:int}/volume/down")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeDown(int zoneIndex, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
        {
            return this.BadRequest("Step must be between 1 and 50");
        }

        var command = new VolumeDownCommand { ZoneIndex = zoneIndex, Step = step };
        var result = await this._mediator.SendCommandAsync<VolumeDownCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToDecreaseZoneVolume(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(step);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MUTE CONTROL - Direct boolean responses
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set zone mute state.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="muted">Mute state (true = muted, false = unmuted)</param>
    /// <returns>New mute state</returns>
    [HttpPut("{zoneIndex:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetMute(int zoneIndex, [FromBody] bool muted)
    {
        var command = new SetZoneMuteCommand { ZoneIndex = zoneIndex, Enabled = muted };
        var result = await this._mediator.SendCommandAsync<SetZoneMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneMute(zoneIndex, muted, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(muted);
    }

    /// <summary>
    /// Get current zone mute state.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current mute state</returns>
    [HttpGet("{zoneIndex:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetMute(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneMuteState(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Mute);
    }

    /// <summary>
    /// Toggle zone mute state.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New mute state</returns>
    [HttpPost("{zoneIndex:int}/mute/toggle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ToggleMute(int zoneIndex)
    {
        var command = new ToggleZoneMuteCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<ToggleZoneMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToToggleZoneMute(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The mute toggle will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYBACK CONTROL - 204 No Content for actions, direct responses for state
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Start playback in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/play")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Play(int zoneIndex)
    {
        var command = new PlayCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<PlayCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPlayZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted();
    }

    /// <summary>
    /// Pause playback in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(int zoneIndex)
    {
        var command = new PauseCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<PauseCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPauseZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted();
    }

    /// <summary>
    /// Stop playback in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Stop(int zoneIndex)
    {
        var command = new StopCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<StopCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToStopZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted();
    }

    /// <summary>
    /// Set playlist for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="playlistIndex">Playlist index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPut("{zoneIndex:int}/playlist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPlaylist(int zoneIndex, [FromBody] int playlistIndex)
    {
        if (playlistIndex < 1)
        {
            return this.BadRequest("Playlist index must be greater than 0");
        }

        var command = new SetPlaylistCommand { ZoneIndex = zoneIndex, PlaylistIndex = playlistIndex };
        var result = await this._mediator.SendCommandAsync<SetPlaylistCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZonePlaylist(zoneIndex, playlistIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(playlistIndex);
    }

    [LoggerMessage(
        EventId = 5300,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zones: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZones(string errorMessage);

    [LoggerMessage(
        EventId = 5301,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZone(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5302,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} volume to {Volume}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZoneVolume(int zoneIndex, int volume, string errorMessage);

    [LoggerMessage(
        EventId = 5303,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} volume: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneVolume(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5304,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to increase zone {ZoneIndex} volume: {ErrorMessage}"
    )]
    private partial void LogFailedToIncreaseZoneVolume(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5305,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to decrease zone {ZoneIndex} volume: {ErrorMessage}"
    )]
    private partial void LogFailedToDecreaseZoneVolume(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5306,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} mute to {Muted}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZoneMute(int zoneIndex, bool muted, string errorMessage);

    [LoggerMessage(
        EventId = 5307,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} mute state: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneMuteState(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5308,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to toggle zone {ZoneIndex} mute: {ErrorMessage}"
    )]
    private partial void LogFailedToToggleZoneMute(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5309,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to play zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToPlayZone(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5310,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to pause zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToPauseZone(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5311,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to stop zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToStopZone(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5312,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} playlist to {PlaylistIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZonePlaylist(int zoneIndex, int playlistIndex, string errorMessage);

    /// <summary>
    /// Set track for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="trackIndex">Track index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPut("{zoneIndex:int}/track")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTrack(int zoneIndex, [FromBody] int trackIndex)
    {
        if (trackIndex < 1)
        {
            return this.BadRequest("Track index must be greater than 0");
        }

        var command = new SetTrackCommand { ZoneIndex = zoneIndex, TrackIndex = trackIndex };
        var result = await this._mediator.SendCommandAsync<SetTrackCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneTrack(zoneIndex, trackIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(trackIndex);
    }

    [LoggerMessage(
        EventId = 5313,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} track to {TrackIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZoneTrack(int zoneIndex, int trackIndex, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════════
    // REPEAT CONTROL - New simplified API structure
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get track repeat mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Track repeat enabled state</returns>
    [HttpGet("{zoneIndex:int}/repeat/track")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetTrackRepeat(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackRepeat(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.TrackRepeat);
    }

    /// <summary>
    /// Set track repeat mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="enabled">Track repeat enabled state</param>
    /// <returns>New track repeat state</returns>
    [HttpPut("{zoneIndex:int}/repeat/track")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetTrackRepeat(int zoneIndex, [FromBody] bool enabled)
    {
        var command = new SetTrackRepeatCommand { ZoneIndex = zoneIndex, Enabled = enabled };
        var result = await this._mediator.SendCommandAsync<SetTrackRepeatCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneTrackRepeat(zoneIndex, enabled, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(enabled);
    }

    /// <summary>
    /// Toggle track repeat mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New track repeat state</returns>
    [HttpPost("{zoneIndex:int}/repeat/track/toggle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ToggleTrackRepeat(int zoneIndex)
    {
        var command = new ToggleTrackRepeatCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<ToggleTrackRepeatCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToToggleZoneTrackRepeat(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The track repeat toggle will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    /// <summary>
    /// Get playlist repeat mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Playlist repeat enabled state</returns>
    [HttpGet("{zoneIndex:int}/repeat")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetPlaylistRepeat(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZonePlaylistRepeat(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.PlaylistRepeat);
    }

    /// <summary>
    /// Set playlist repeat mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="enabled">Playlist repeat enabled state</param>
    /// <returns>New playlist repeat state</returns>
    [HttpPut("{zoneIndex:int}/repeat")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetPlaylistRepeat(int zoneIndex, [FromBody] bool enabled)
    {
        var command = new SetPlaylistRepeatCommand { ZoneIndex = zoneIndex, Enabled = enabled };
        var result = await this._mediator.SendCommandAsync<SetPlaylistRepeatCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZonePlaylistRepeat(zoneIndex, enabled, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(enabled);
    }

    /// <summary>
    /// Toggle playlist repeat mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New playlist repeat state</returns>
    [HttpPost("{zoneIndex:int}/repeat/toggle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> TogglePlaylistRepeat(int zoneIndex)
    {
        var command = new TogglePlaylistRepeatCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<TogglePlaylistRepeatCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToToggleZonePlaylistRepeat(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The playlist repeat toggle will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SHUFFLE CONTROL - Simplified API structure
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get playlist shuffle mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Playlist shuffle enabled state</returns>
    [HttpGet("{zoneIndex:int}/shuffle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetPlaylistShuffle(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZonePlaylistShuffle(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.PlaylistShuffle);
    }

    /// <summary>
    /// Set playlist shuffle mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="enabled">Playlist shuffle enabled state</param>
    /// <returns>New playlist shuffle state</returns>
    [HttpPut("{zoneIndex:int}/shuffle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetPlaylistShuffle(int zoneIndex, [FromBody] bool enabled)
    {
        var command = new SetPlaylistShuffleCommand { ZoneIndex = zoneIndex, Enabled = enabled };
        var result = await this._mediator.SendCommandAsync<SetPlaylistShuffleCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZonePlaylistShuffle(zoneIndex, enabled, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(enabled);
    }

    /// <summary>
    /// Toggle playlist shuffle mode for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New playlist shuffle state</returns>
    [HttpPost("{zoneIndex:int}/shuffle/toggle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> TogglePlaylistShuffle(int zoneIndex)
    {
        var command = new TogglePlaylistShuffleCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<TogglePlaylistShuffleCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToToggleZonePlaylistShuffle(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The playlist shuffle toggle will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // TRACK NAVIGATION - Following existing patterns
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Skip to the next track in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New track index</returns>
    [HttpPost("{zoneIndex:int}/next")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> TrackNext(int zoneIndex)
    {
        var command = new NextTrackCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<NextTrackCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSkipToNextTrack(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The track navigation will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    /// <summary>
    /// Skip to the previous track in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New track index</returns>
    [HttpPost("{zoneIndex:int}/previous")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> TrackPrevious(int zoneIndex)
    {
        var command = new PreviousTrackCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<PreviousTrackCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSkipToPreviousTrack(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The track navigation will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYLIST NAVIGATION - Following existing patterns
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Switch to the next playlist in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New playlist name</returns>
    [HttpPost("{zoneIndex:int}/next/playlist")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> PlaylistNext(int zoneIndex)
    {
        var command = new NextPlaylistCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<NextPlaylistCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSwitchToNextPlaylist(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The playlist navigation will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    /// <summary>
    /// Switch to the previous playlist in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>New playlist name</returns>
    [HttpPost("{zoneIndex:int}/previous/playlist")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> PlaylistPrevious(int zoneIndex)
    {
        var command = new PreviousPlaylistCommand { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<PreviousPlaylistCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSwitchToPreviousPlaylist(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The playlist navigation will be applied asynchronously and published via MQTT/KNX
        return this.Accepted();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // TRACK AND PLAYLIST STATUS - Simple getters from zone state
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get current track index in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track index (1-based)</returns>
    [HttpGet("{zoneIndex:int}/track")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetTrackIndex(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackIndex(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.Index ?? 1);
    }

    /// <summary>
    /// Get current playlist index in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current playlist index (1-based)</returns>
    [HttpGet("{zoneIndex:int}/playlist")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetPlaylistIndex(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZonePlaylistIndex(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Playlist?.Index ?? 1);
    }

    /// <summary>
    /// Get current playlist information in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current playlist information</returns>
    [HttpGet("{zoneIndex:int}/playlist/info")]
    [ProducesResponseType<PlaylistInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaylistInfo>> GetPlaylistInfo(int zoneIndex)
    {
        var query = new GetZonePlaylistInfoQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZonePlaylistInfoQuery, Result<PlaylistInfo>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZonePlaylistInfo(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Get current track metadata in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track metadata</returns>
    [HttpGet("{zoneIndex:int}/track/metadata")]
    [ProducesResponseType<TrackInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackInfo>> GetTrackMetadata(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackMetadata(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        var track = result.Value!.Track;
        if (track == null)
        {
            return this.Ok(
                new TrackInfo
                {
                    Index = 1,
                    Title = "No Track",
                    Artist = "Unknown",
                    Album = "Unknown",
                    Source = "none",
                    Url = "none://no-track",
                    PositionMs = 0,
                    Progress = 0.0f,
                    IsPlaying = false,
                }
            );
        }

        return Ok(track);
    }

    /// <summary>
    /// Get current track title in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track title</returns>
    [HttpGet("{zoneIndex:int}/track/title")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetTrackTitle(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackTitle(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.Title ?? "No Track");
    }

    /// <summary>
    /// Get current track artist in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track artist</returns>
    [HttpGet("{zoneIndex:int}/track/artist")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetTrackArtist(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackArtist(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.Artist ?? "Unknown");
    }

    /// <summary>
    /// Get current track album in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track album</returns>
    [HttpGet("{zoneIndex:int}/track/album")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetTrackAlbum(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackAlbum(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.Album ?? "Unknown");
    }

    /// <summary>
    /// Get current track cover art URL in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track cover art URL</returns>
    [HttpGet("{zoneIndex:int}/track/cover")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetTrackCover(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackCover(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.CoverArtUrl ?? "");
    }

    /// <summary>
    /// Get current track duration in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track duration in milliseconds</returns>
    [HttpGet("{zoneIndex:int}/track/duration")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<long>> GetTrackDuration(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackDuration(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.DurationMs ?? 0L);
    }

    /// <summary>
    /// Get current track playing status in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Is track currently playing</returns>
    [HttpGet("{zoneIndex:int}/track/playing")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetTrackPlaying(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackPlaying(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return this.Ok(result.Value!.PlaybackState == SnapDog2.Shared.Enums.PlaybackState.Playing);
    }

    /// <summary>
    /// Get current track position in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track position in milliseconds</returns>
    [HttpGet("{zoneIndex:int}/track/position")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<long>> GetTrackPosition(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackPosition(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.PositionMs ?? 0L);
    }

    /// <summary>
    /// Get current track progress in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current track progress as percentage (0.0-1.0)</returns>
    [HttpGet("{zoneIndex:int}/track/progress")]
    [ProducesResponseType<float>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<float>> GetTrackProgress(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneTrackProgress(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Track?.Progress ?? 0.0f);
    }

    /// <summary>
    /// Seek to a specific position in the current track.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="positionMs">Position in milliseconds</param>
    /// <returns>New track position in milliseconds</returns>
    [HttpPut("{zoneIndex:int}/track/position")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<long>> SetTrackPosition(int zoneIndex, [FromBody] long positionMs)
    {
        if (positionMs < 0)
        {
            return this.BadRequest("Position must be greater than or equal to 0");
        }

        var command = new SeekPositionCommand { ZoneIndex = zoneIndex, PositionMs = positionMs };
        var result = await this._mediator.SendCommandAsync<SeekPositionCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneTrackPosition(zoneIndex, positionMs, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(positionMs);
    }

    /// <summary>
    /// Seek to a specific progress percentage in the current track.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="progress">Progress percentage (0.0-1.0)</param>
    /// <returns>New track progress percentage</returns>
    [HttpPut("{zoneIndex:int}/track/progress")]
    [ProducesResponseType<float>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<float>> SetTrackProgress(int zoneIndex, [FromBody] float progress)
    {
        if (progress < 0.0f || progress > 1.0f)
        {
            return this.BadRequest("Progress must be between 0.0 and 1.0");
        }

        var command = new SeekProgressCommand { ZoneIndex = zoneIndex, Progress = progress };
        var result = await this._mediator.SendCommandAsync<SeekProgressCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneTrackProgress(zoneIndex, progress, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(progress);
    }

    /// <summary>
    /// Play a specific track by index and start playback immediately.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="trackIndex">Track index (1-based)</param>
    /// <returns>Track index that started playing</returns>
    [HttpPost("{zoneIndex:int}/play/track")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> PlayTrackByIndex(int zoneIndex, [FromBody] int trackIndex)
    {
        if (trackIndex < 1)
        {
            return this.BadRequest("Track index must be greater than 0");
        }

        var command = new PlayTrackByIndexCommand { ZoneIndex = zoneIndex, TrackIndex = trackIndex };
        var result = await this._mediator.SendCommandAsync<PlayTrackByIndexCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPlayTrackByIndex(zoneIndex, trackIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(trackIndex);
    }

    /// <summary>
    /// Play a direct URL stream and start playback immediately.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="url">URL to play</param>
    /// <returns>URL that started playing</returns>
    [HttpPost("{zoneIndex:int}/play/url")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> PlayUrl(int zoneIndex, [FromBody] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return this.BadRequest("URL cannot be empty");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return this.BadRequest("Invalid URL format");
        }

        var command = new PlayUrlCommand { ZoneIndex = zoneIndex, Url = url };
        var result = await this._mediator.SendCommandAsync<PlayUrlCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPlayUrl(zoneIndex, url, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(url);
    }

    /// <summary>
    /// Play a specific track from a specific playlist and start playback immediately.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="playlistIndex">Playlist index (1-based)</param>
    /// <param name="trackIndex">Track index within the playlist (1-based)</param>
    /// <returns>Track index that started playing</returns>
    [HttpPost("{zoneIndex:int}/play/playlist/{playlistIndex:int}/track")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> PlayTrackFromPlaylist(int zoneIndex, int playlistIndex, [FromBody] int trackIndex)
    {
        if (playlistIndex < 1)
        {
            return this.BadRequest("Playlist index must be greater than 0");
        }

        if (trackIndex < 1)
        {
            return this.BadRequest("Track index must be greater than 0");
        }

        var command = new PlayTrackFromPlaylistCommand
        {
            ZoneIndex = zoneIndex,
            PlaylistIndex = playlistIndex,
            TrackIndex = trackIndex
        };
        var result = await this._mediator.SendCommandAsync<PlayTrackFromPlaylistCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPlayTrackFromPlaylist(zoneIndex, playlistIndex, trackIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(trackIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGING METHODS FOR NEW ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(
        EventId = 5314,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track repeat: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackRepeat(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5315,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} track repeat to {Enabled}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZoneTrackRepeat(int zoneIndex, bool enabled, string errorMessage);

    [LoggerMessage(
        EventId = 5316,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to toggle zone {ZoneIndex} track repeat: {ErrorMessage}"
    )]
    private partial void LogFailedToToggleZoneTrackRepeat(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5317,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playlist repeat: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZonePlaylistRepeat(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5318,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} playlist repeat to {Enabled}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZonePlaylistRepeat(int zoneIndex, bool enabled, string errorMessage);

    [LoggerMessage(
        EventId = 5319,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to toggle zone {ZoneIndex} playlist repeat: {ErrorMessage}"
    )]
    private partial void LogFailedToToggleZonePlaylistRepeat(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5320,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playlist shuffle: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZonePlaylistShuffle(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5321,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} playlist shuffle to {Enabled}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZonePlaylistShuffle(int zoneIndex, bool enabled, string errorMessage);

    [LoggerMessage(
        EventId = 5322,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to toggle zone {ZoneIndex} playlist shuffle: {ErrorMessage}"
    )]
    private partial void LogFailedToToggleZonePlaylistShuffle(int zoneIndex, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGING METHODS FOR NEW TRACK NAVIGATION ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(
        EventId = 5323,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to skip zone {ZoneIndex} to next track: {ErrorMessage}"
    )]
    private partial void LogFailedToSkipToNextTrack(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5324,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to skip zone {ZoneIndex} to previous track: {ErrorMessage}"
    )]
    private partial void LogFailedToSkipToPreviousTrack(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5325,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to switch zone {ZoneIndex} to next playlist: {ErrorMessage}"
    )]
    private partial void LogFailedToSwitchToNextPlaylist(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5326,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to switch zone {ZoneIndex} to previous playlist: {ErrorMessage}"
    )]
    private partial void LogFailedToSwitchToPreviousPlaylist(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5327,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track index: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackIndex(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5328,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playlist index: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZonePlaylistIndex(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5337,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playlist info: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZonePlaylistInfo(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5329,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track metadata: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackMetadata(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5330,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track playing status: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackPlaying(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5331,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track position: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackPosition(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5332,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track progress: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackProgress(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5333,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} track position to {PositionMs}ms: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZoneTrackPosition(int zoneIndex, long positionMs, string errorMessage);

    [LoggerMessage(
        EventId = 5334,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to set zone {ZoneIndex} track progress to {Progress:P1}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZoneTrackProgress(int zoneIndex, float progress, string errorMessage);

    [LoggerMessage(
        EventId = 5335,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to play track {TrackIndex} for zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToPlayTrackByIndex(int zoneIndex, int trackIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5336,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to play URL '{Url}' for zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToPlayUrl(int zoneIndex, string url, string errorMessage);

    [LoggerMessage(
        EventId = 5337,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to play track {TrackIndex} from playlist {PlaylistIndex} for zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToPlayTrackFromPlaylist(int zoneIndex, int playlistIndex, int trackIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5339,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track title: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackTitle(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5339,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track artist: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackArtist(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5340,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track album: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackAlbum(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5341,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track cover: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackCover(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5342,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} track duration: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneTrackDuration(int zoneIndex, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE NAME - Read-only endpoint for zone identification
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the name of the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Zone name</returns>
    [HttpGet("{zoneIndex:int}/name")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetZoneName(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneName(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value!.Name ?? $"Zone {zoneIndex}");
    }

    [LoggerMessage(
        EventId = 5343,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} name: {ErrorMessage}"
    )]
    private partial void LogFailedToGetZoneName(int zoneIndex, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════════
    // ADDITIONAL STATUS ENDPOINTS - Blueprint-required status endpoints
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get current playback state for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current playback state</returns>
    [HttpGet("{zoneIndex:int}/playback")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlaybackState(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetPlaybackState(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        // Return playback state as string (Playing, Paused, Stopped)
        var playbackState = result.Value?.PlaybackState.ToString() ?? "Unknown";
        return Ok(playbackState);
    }

    /// <summary>
    /// Get current playlist name for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Current playlist name</returns>
    [HttpGet("{zoneIndex:int}/playlist/name")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlaylistName(int zoneIndex)
    {
        var query = new GetZonePlaylistInfoQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZonePlaylistInfoQuery, Result<PlaylistInfo>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetPlaylistName(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value?.Name ?? "Unknown Playlist");
    }

    /// <summary>
    /// Get current playlist track count for the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Number of tracks in current playlist</returns>
    [HttpGet("{zoneIndex:int}/playlist/count")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlaylistCount(int zoneIndex)
    {
        var query = new GetZonePlaylistInfoQuery { ZoneIndex = zoneIndex };
        var result = await this._mediator.SendQueryAsync<GetZonePlaylistInfoQuery, Result<PlaylistInfo>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetPlaylistCount(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Zone {zoneIndex} not found");
        }

        return Ok(result.Value?.TrackCount ?? 0);
    }

    [LoggerMessage(
        EventId = 5344,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playback state: {ErrorMessage}"
    )]
    private partial void LogFailedToGetPlaybackState(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5345,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playlist name: {ErrorMessage}"
    )]
    private partial void LogFailedToGetPlaylistName(int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5346,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to get zone {ZoneIndex} playlist count: {ErrorMessage}"
    )]
    private partial void LogFailedToGetPlaylistCount(int zoneIndex, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════════
    // UNIFIED CONTROL - Blueprint-compliant control command endpoint
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Execute a unified control command on the zone using blueprint vocabulary.
    /// Accepts commands like "play", "pause", "next", "shuffle_on", etc.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="command">Control command string (blueprint vocabulary)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/control")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ControlSet(int zoneIndex, [FromBody] string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return this.BadRequest("Command cannot be empty");
        }

        var controlCommand = new ControlSetCommand { ZoneIndex = zoneIndex, Command = command.Trim() };
        var result = await this._mediator.SendCommandAsync<ControlSetCommand, Result>(controlCommand);

        if (result.IsFailure)
        {
            LogFailedToExecuteControlCommand(zoneIndex, command, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted();
    }

    [LoggerMessage(
        EventId = 5347,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to execute control command '{Command}' on zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToExecuteControlCommand(int zoneIndex, string command, string errorMessage);
}
