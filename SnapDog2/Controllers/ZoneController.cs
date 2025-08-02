namespace SnapDog2.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Controller for zone management operations.
/// </summary>
[ApiController]
[Route("api/zones")]
[Produces("application/json")]
public class ZoneController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZoneController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneController"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    public ZoneController(IServiceProvider serviceProvider, ILogger<ZoneController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current state of a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The zone state.</returns>
    [HttpGet("{zoneId:int}/state")]
    [ProducesResponseType(typeof(ZoneState), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ZoneState>> GetZoneState([Range(1, int.MaxValue)] int zoneId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting zone state for zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetZoneStateQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetZoneStateQueryHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetZoneStateQuery { ZoneId = zoneId }, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to get zone state for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return NotFound(new { error = result.ErrorMessage ?? "Zone not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting zone state for zone {ZoneId}", zoneId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the states of all zones.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of zone states.</returns>
    [HttpGet("states")]
    [ProducesResponseType(typeof(IEnumerable<ZoneState>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<ZoneState>>> GetAllZoneStates(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting all zone states");

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.GetAllZoneStatesQueryHandler>();
            if (handler == null)
            {
                _logger.LogError("GetAllZoneStatesQueryHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var result = await handler.Handle(new GetAllZoneStatesQuery(), cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to get all zone states: {Error}", result.ErrorMessage);
            return StatusCode(500, new { error = result.ErrorMessage ?? "Failed to get zone states" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all zone states");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Starts or resumes playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/play")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Play([Range(1, int.MaxValue)] int zoneId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting playback for zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.PlayCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("PlayCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new PlayCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Playback started successfully" });
            }

            _logger.LogWarning("Failed to play zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to start playback" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting playback for zone {ZoneId}", zoneId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Pauses playback in a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/pause")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Pause([Range(1, int.MaxValue)] int zoneId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Pausing playback for zone {ZoneId}", zoneId);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.PauseCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("PauseCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new PauseCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Playback paused successfully" });
            }

            _logger.LogWarning("Failed to pause zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to pause playback" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing playback for zone {ZoneId}", zoneId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Sets the volume for a zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <param name="request">The volume request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    [HttpPost("{zoneId:int}/volume")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SetVolume([Range(1, int.MaxValue)] int zoneId, [FromBody] VolumeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting volume for zone {ZoneId} to {Volume}", zoneId, request.Volume);

            var handler = _serviceProvider.GetService<SnapDog2.Server.Features.Zones.Handlers.SetZoneVolumeCommandHandler>();
            if (handler == null)
            {
                _logger.LogError("SetZoneVolumeCommandHandler not found in DI container");
                return StatusCode(500, new { error = "Handler not available" });
            }

            var command = new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = request.Volume,
                Source = CommandSource.Api
            };

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Volume set successfully" });
            }

            _logger.LogWarning("Failed to set volume for zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to set volume" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting volume for zone {ZoneId}", zoneId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

// Request DTOs
public record VolumeRequest
{
    [Range(0, 100)]
    public required int Volume { get; init; }
}
