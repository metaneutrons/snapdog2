using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Core.Abstractions;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Controller for monitoring zone-based client grouping operations.
/// Provides read-only endpoints for status and validation - grouping is fully automatic.
/// </summary>
[ApiController]
[Route("api/zone-grouping")]
[Authorize]
public class ZoneGroupingController : ControllerBase
{
    private readonly IZoneGroupingService _zoneGroupingService;
    private readonly ILogger<ZoneGroupingController> _logger;

    public ZoneGroupingController(IZoneGroupingService zoneGroupingService, ILogger<ZoneGroupingController> logger)
    {
        _zoneGroupingService = zoneGroupingService ?? throw new ArgumentNullException(nameof(zoneGroupingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current status of zone-based client grouping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive zone grouping status</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetZoneGroupingStatus(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📊 Getting zone grouping status");

        var result = await _zoneGroupingService.GetZoneGroupingStatusAsync(cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        _logger.LogError("❌ Failed to get zone grouping status: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Validates that current Snapcast grouping matches logical zone assignments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateGroupingConsistency(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Validating zone grouping consistency");

        var result = await _zoneGroupingService.ValidateGroupingConsistencyAsync(cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { status = "valid", message = "Zone grouping is consistent" });
        }

        _logger.LogWarning("⚠️ Zone grouping validation failed: {Error}", result.ErrorMessage);
        return BadRequest(new { status = "invalid", message = result.ErrorMessage });
    }
}
