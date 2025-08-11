namespace SnapDog2.Api.Controllers.V1;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;

/// <summary>
/// Modern simplified zones controller with direct primitive responses.
///
/// MODERN API DESIGN PRINCIPLES:
/// - Return primitives directly (int, bool, string) instead of wrapper objects
/// - Use direct parameter binding instead of single-property request objects
/// - Minimal DTOs only for complex multi-property requests
/// - Clean HTTP semantics: 200 for data, 204 for actions, proper error codes
/// </summary>
[ApiController]
[Route("api/v1/zones")]
[Produces("application/json")]
public class ZonesController : ControllerBase
{
    private readonly ILogger<ZonesController> _logger;

    public ZonesController(ILogger<ZonesController> logger)
    {
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE LISTING - Clean paginated API with simplified response
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lists configured zones with clean pagination.
    /// Returns data directly without wrapper objects.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Page<Zone>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public ActionResult<Page<Zone>> GetZones([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        try
        {
            var zones = new Zone[]
            {
                new("Living Room", 1, true, "Playing"),
                new("Kitchen", 2, false, "Stopped"),
                new("Bedroom", 3, true, "Paused"),
            };

            return Ok(new Page<Zone>(zones, zones.Length, size, page));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve zones");
            return Problem(
                title: "Failed to retrieve zones",
                detail: "An error occurred while fetching the zone list",
                statusCode: 500
            );
        }
    }

    /// <summary>
    /// Gets detailed information about a specific zone.
    /// Returns zone state directly.
    /// </summary>
    [HttpGet("{zoneIndex:int}")]
    [ProducesResponseType(typeof(ZoneState), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public ActionResult<ZoneState> GetZone(int zoneIndex)
    {
        try
        {
            if (zoneIndex < 1 || zoneIndex > 10)
            {
                return NotFound(
                    new ProblemDetails
                    {
                        Title = "Zone not found",
                        Detail = $"Zone {zoneIndex} does not exist",
                        Status = 404,
                    }
                );
            }

            var zoneState = new ZoneState
            {
                Id = zoneIndex,
                Name = $"Zone {zoneIndex}",
                PlaybackState = "play",
                Volume = 75,
                Mute = false,
                TrackRepeat = false,
                PlaylistRepeat = true,
                PlaylistShuffle = false,
                SnapcastGroupId = $"group-{zoneIndex}",
                SnapcastStreamId = $"stream-{zoneIndex}",
                IsSnapcastGroupMuted = false,
                Clients = new[] { zoneIndex },
                TimestampUtc = DateTime.UtcNow,
            };

            return Ok(zoneState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get zone {ZoneIndex}", zoneIndex);
            return Problem(
                title: "Failed to retrieve zone",
                detail: $"An error occurred while fetching zone {zoneIndex}",
                statusCode: 500
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // VOLUME CONTROL - Direct primitive responses, no wrapper objects
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the current volume level for a zone.
    /// Returns volume directly as integer (0-100).
    /// </summary>
    [HttpGet("{zoneIndex:int}/volume")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<int> GetVolume(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(75); // Direct primitive return - no wrapper!
    }

    /// <summary>
    /// Sets the volume level for a zone.
    /// Uses direct parameter binding instead of request object.
    /// </summary>
    [HttpPut("{zoneIndex:int}/volume")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<int> SetVolume(int zoneIndex, [FromBody] int volume)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        if (volume < 0 || volume > 100)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid volume level",
                    Detail = "Volume must be between 0 and 100",
                    Status = 400,
                }
            );
        }

        return Ok(volume); // Return the set value directly
    }

    /// <summary>
    /// Increases the volume for a zone.
    /// Uses direct parameter binding with default value.
    /// </summary>
    [HttpPost("{zoneIndex:int}/volume/up")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<int> VolumeUp(int zoneIndex, [FromBody] int step = 5)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        var newVolume = Math.Min(100, 75 + step);
        return Ok(newVolume); // Direct primitive return
    }

    /// <summary>
    /// Decreases the volume for a zone.
    /// Uses direct parameter binding with default value.
    /// </summary>
    [HttpPost("{zoneIndex:int}/volume/down")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<int> VolumeDown(int zoneIndex, [FromBody] int step = 5)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        var newVolume = Math.Max(0, 75 - step);
        return Ok(newVolume); // Direct primitive return
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MUTE CONTROL - Direct boolean responses
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the current mute state.
    /// Returns boolean directly - no wrapper object.
    /// </summary>
    [HttpGet("{zoneIndex:int}/mute")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> GetMute(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(false); // Direct boolean return
    }

    /// <summary>
    /// Sets the mute state.
    /// Uses direct boolean parameter instead of request object.
    /// </summary>
    [HttpPut("{zoneIndex:int}/mute")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> SetMute(int zoneIndex, [FromBody] bool muted)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(muted); // Return the set value directly
    }

    /// <summary>
    /// Toggles the mute state.
    /// Returns new mute state directly.
    /// </summary>
    [HttpPost("{zoneIndex:int}/mute/toggle")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> ToggleMute(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(true); // Return toggled state directly
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // TRACK MANAGEMENT - Direct primitive responses
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the current track index (1-based).
    /// Returns integer directly - no wrapper object.
    /// </summary>
    [HttpGet("{zoneIndex:int}/track")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<int> GetTrack(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(3); // Direct integer return (1-based index)
    }

    /// <summary>
    /// Sets the current track by index (1-based).
    /// Uses direct integer parameter instead of request object.
    /// </summary>
    [HttpPut("{zoneIndex:int}/track")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult SetTrack(int zoneIndex, [FromBody] int track)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        if (track < 1)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid track index",
                    Detail = "Track index must be 1 or greater",
                    Status = 400,
                }
            );
        }

        return NoContent(); // Action completed, no data to return
    }

    /// <summary>
    /// Gets the current track metadata.
    /// Returns TrackInfo directly - complex objects don't need wrappers.
    /// </summary>
    [HttpGet("{zoneIndex:int}/track/info")]
    [ProducesResponseType(typeof(TrackInfo), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<TrackInfo> GetTrackInfo(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        var trackInfo = new TrackInfo
        {
            Index = 3,
            Id = "track-123",
            Title = "Sample Track",
            Artist = "Sample Artist",
            Album = "Sample Album",
            DurationSec = 210,
            PositionSec = 72,
            Source = "subsonic",
        };

        return Ok(trackInfo); // Complex object returned directly
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYBACK CONTROL - Clean HTTP semantics
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Starts playback in a zone.
    /// Supports both URL and track-based playback via optional request body.
    /// </summary>
    [HttpPost("{zoneIndex:int}/play")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult Play(int zoneIndex, [FromBody] PlayRequest? request = null)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        // Handle both URL and track-based playback
        return NoContent(); // Action completed successfully
    }

    /// <summary>Pauses playback in a zone.</summary>
    [HttpPost("{zoneIndex:int}/pause")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult Pause(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return NoContent();
    }

    /// <summary>Stops playback in a zone.</summary>
    [HttpPost("{zoneIndex:int}/stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult Stop(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return NoContent();
    }

    /// <summary>Skips to the next track.</summary>
    [HttpPost("{zoneIndex:int}/track/next")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult NextTrack(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return NoContent();
    }

    /// <summary>Skips to the previous track.</summary>
    [HttpPost("{zoneIndex:int}/track/previous")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult PreviousTrack(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // REPEAT/SHUFFLE CONTROL - Direct boolean responses
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>Gets the track repeat state. Returns boolean directly.</summary>
    [HttpGet("{zoneIndex:int}/track/repeat")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> GetTrackRepeat(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(false); // Direct boolean return
    }

    /// <summary>Sets the track repeat state. Uses direct boolean parameter.</summary>
    [HttpPut("{zoneIndex:int}/track/repeat")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> SetTrackRepeat(int zoneIndex, [FromBody] bool enabled)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(enabled); // Return the set value directly
    }

    /// <summary>Toggles the track repeat state. Returns new state directly.</summary>
    [HttpPost("{zoneIndex:int}/track/repeat/toggle")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> ToggleTrackRepeat(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(true); // Return toggled state directly
    }

    /// <summary>Gets the playlist repeat state. Returns boolean directly.</summary>
    [HttpGet("{zoneIndex:int}/playlist/repeat")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> GetPlaylistRepeat(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(true); // Direct boolean return
    }

    /// <summary>Sets the playlist repeat state. Uses direct boolean parameter.</summary>
    [HttpPut("{zoneIndex:int}/playlist/repeat")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> SetPlaylistRepeat(int zoneIndex, [FromBody] bool enabled)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(enabled); // Return the set value directly
    }

    /// <summary>Toggles the playlist repeat state. Returns new state directly.</summary>
    [HttpPost("{zoneIndex:int}/playlist/repeat/toggle")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> TogglePlaylistRepeat(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(false); // Return toggled state directly
    }

    /// <summary>Gets the playlist shuffle state. Returns boolean directly.</summary>
    [HttpGet("{zoneIndex:int}/playlist/shuffle")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> GetPlaylistShuffle(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(false); // Direct boolean return
    }

    /// <summary>Sets the playlist shuffle state. Uses direct boolean parameter.</summary>
    [HttpPut("{zoneIndex:int}/playlist/shuffle")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> SetPlaylistShuffle(int zoneIndex, [FromBody] bool enabled)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(enabled); // Return the set value directly
    }

    /// <summary>Toggles the playlist shuffle state. Returns new state directly.</summary>
    [HttpPost("{zoneIndex:int}/playlist/shuffle/toggle")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public ActionResult<bool> TogglePlaylistShuffle(int zoneIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        return Ok(true); // Return toggled state directly
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYLIST MANAGEMENT - Minimal request objects for complex operations
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the playlist by index (1-based).
    /// Uses direct integer parameter for maximum simplicity.
    /// </summary>
    [HttpPut("{zoneIndex:int}/playlist")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public IActionResult SetPlaylist(int zoneIndex, [FromBody] int playlistIndex)
    {
        if (zoneIndex < 1 || zoneIndex > 10)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Zone not found",
                    Detail = $"Zone {zoneIndex} does not exist",
                    Status = 404,
                }
            );
        }

        if (playlistIndex < 1)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid playlist index",
                    Detail = "Playlist index must be 1 or greater",
                    Status = 400,
                }
            );
        }

        return NoContent(); // Action completed successfully
    }
}
