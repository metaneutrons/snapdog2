namespace SnapDog2.Api.Controllers.V1;

using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using SnapDog2.Server.Features.Zones.Queries;

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
[Authorize]
[Produces("application/json")]
[Tags("Zones")]
public partial class ZonesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ZonesController> _logger;

    public ZonesController(IMediator mediator, ILogger<ZonesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE LISTING - Clean paginated API with real data via mediator
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lists configured zones with clean pagination.
    /// Returns data directly without wrapper objects.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<Page<ZoneState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Page<ZoneState>>> GetZones([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1)
            return BadRequest("Page must be greater than 0");

        if (size < 1 || size > 100)
            return BadRequest("Size must be between 1 and 100");

        var query = new GetAllZonesQuery();
        var result = await _mediator.SendQueryAsync<GetAllZonesQuery, Result<List<ZoneState>>>(query);

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

    /// <summary>
    /// Get detailed information for a specific zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Zone state information</returns>
    [HttpGet("{zoneIndex:int}")]
    [ProducesResponseType<ZoneState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZoneState>> GetZone(int zoneIndex)
    {
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var result = await _mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Zone {zoneIndex} not found");
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
            return BadRequest("Volume must be between 0 and 100");

        var command = new SetZoneVolumeCommand { ZoneIndex = zoneIndex, Volume = volume };
        var result = await _mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneVolume(zoneIndex, volume, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(volume);
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
        var result = await _mediator.SendQueryAsync<GetZoneVolumeQuery, Result<int>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneVolume(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Zone {zoneIndex} not found");
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
            return BadRequest("Step must be between 1 and 50");

        var command = new VolumeUpCommand { ZoneIndex = zoneIndex, Step = step };
        var result = await _mediator.SendCommandAsync<VolumeUpCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToIncreaseZoneVolume(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // Get the new volume to return
        var volumeQuery = new GetZoneVolumeQuery { ZoneIndex = zoneIndex };
        var volumeResult = await _mediator.SendQueryAsync<GetZoneVolumeQuery, Result<int>>(volumeQuery);

        return Ok(volumeResult.IsSuccess ? volumeResult.Value : 0);
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
            return BadRequest("Step must be between 1 and 50");

        var command = new VolumeDownCommand { ZoneIndex = zoneIndex, Step = step };
        var result = await _mediator.SendCommandAsync<VolumeDownCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToDecreaseZoneVolume(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // Get the new volume to return
        var volumeQuery = new GetZoneVolumeQuery { ZoneIndex = zoneIndex };
        var volumeResult = await _mediator.SendQueryAsync<GetZoneVolumeQuery, Result<int>>(volumeQuery);

        return Ok(volumeResult.IsSuccess ? volumeResult.Value : 0);
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
        var result = await _mediator.SendCommandAsync<SetZoneMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZoneMute(zoneIndex, muted, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(muted);
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
        var result = await _mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetZoneMuteState(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Zone {zoneIndex} not found");
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
        var result = await _mediator.SendCommandAsync<ToggleZoneMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToToggleZoneMute(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // Get the new state to return
        var query = new GetZoneStateQuery { ZoneIndex = zoneIndex };
        var stateResult = await _mediator.SendQueryAsync<GetZoneStateQuery, Result<ZoneState>>(query);

        return Ok(stateResult.IsSuccess ? stateResult.Value!.Mute : false);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYBACK CONTROL - 204 No Content for actions, direct responses for state
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Start playback in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/playback/play")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Play(int zoneIndex)
    {
        var command = new PlayCommand { ZoneIndex = zoneIndex };
        var result = await _mediator.SendCommandAsync<PlayCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPlayZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    /// <summary>
    /// Pause playback in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/playback/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(int zoneIndex)
    {
        var command = new PauseCommand { ZoneIndex = zoneIndex };
        var result = await _mediator.SendCommandAsync<PauseCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToPauseZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    /// <summary>
    /// Stop playback in the zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPost("{zoneIndex:int}/playback/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Stop(int zoneIndex)
    {
        var command = new StopCommand { ZoneIndex = zoneIndex };
        var result = await _mediator.SendCommandAsync<StopCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToStopZone(zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
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
            return BadRequest("Playlist index must be greater than 0");

        var command = new SetPlaylistCommand { ZoneIndex = zoneIndex, PlaylistIndex = playlistIndex };
        var result = await _mediator.SendCommandAsync<SetPlaylistCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetZonePlaylist(zoneIndex, playlistIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    [LoggerMessage(12001, LogLevel.Warning, "Failed to get zones: {ErrorMessage}")]
    private partial void LogFailedToGetZones(string errorMessage);

    [LoggerMessage(12002, LogLevel.Warning, "Failed to get zone {ZoneIndex}: {ErrorMessage}")]
    private partial void LogFailedToGetZone(int zoneIndex, string errorMessage);

    [LoggerMessage(12003, LogLevel.Warning, "Failed to set zone {ZoneIndex} volume to {Volume}: {ErrorMessage}")]
    private partial void LogFailedToSetZoneVolume(int zoneIndex, int volume, string errorMessage);

    [LoggerMessage(12004, LogLevel.Warning, "Failed to get zone {ZoneIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToGetZoneVolume(int zoneIndex, string errorMessage);

    [LoggerMessage(12005, LogLevel.Warning, "Failed to increase zone {ZoneIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToIncreaseZoneVolume(int zoneIndex, string errorMessage);

    [LoggerMessage(12006, LogLevel.Warning, "Failed to decrease zone {ZoneIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToDecreaseZoneVolume(int zoneIndex, string errorMessage);

    [LoggerMessage(12007, LogLevel.Warning, "Failed to set zone {ZoneIndex} mute to {Muted}: {ErrorMessage}")]
    private partial void LogFailedToSetZoneMute(int zoneIndex, bool muted, string errorMessage);

    [LoggerMessage(12008, LogLevel.Warning, "Failed to get zone {ZoneIndex} mute state: {ErrorMessage}")]
    private partial void LogFailedToGetZoneMuteState(int zoneIndex, string errorMessage);

    [LoggerMessage(12009, LogLevel.Warning, "Failed to toggle zone {ZoneIndex} mute: {ErrorMessage}")]
    private partial void LogFailedToToggleZoneMute(int zoneIndex, string errorMessage);

    [LoggerMessage(12010, LogLevel.Warning, "Failed to play zone {ZoneIndex}: {ErrorMessage}")]
    private partial void LogFailedToPlayZone(int zoneIndex, string errorMessage);

    [LoggerMessage(12011, LogLevel.Warning, "Failed to pause zone {ZoneIndex}: {ErrorMessage}")]
    private partial void LogFailedToPauseZone(int zoneIndex, string errorMessage);

    [LoggerMessage(12012, LogLevel.Warning, "Failed to stop zone {ZoneIndex}: {ErrorMessage}")]
    private partial void LogFailedToStopZone(int zoneIndex, string errorMessage);

    [LoggerMessage(
        12013,
        LogLevel.Warning,
        "Failed to set zone {ZoneIndex} playlist to {PlaylistIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetZonePlaylist(int zoneIndex, int playlistIndex, string errorMessage);
}
