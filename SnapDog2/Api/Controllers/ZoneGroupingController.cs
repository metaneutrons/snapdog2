using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Core.Abstractions;

namespace SnapDog2.Api.Controllers;

/// <summary>
/// Controller for managing zone-based client grouping operations.
/// Provides endpoints for testing, monitoring, and managing Snapcast client grouping.
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

    /// <summary>
    /// Synchronizes grouping for a specific zone.
    /// </summary>
    /// <param name="zoneId">Zone ID to synchronize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Synchronization result</returns>
    [HttpPost("zones/{zoneId:int}/synchronize")]
    public async Task<IActionResult> SynchronizeZoneGrouping(int zoneId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 Synchronizing zone {ZoneId} grouping", zoneId);

        var result = await _zoneGroupingService.SynchronizeZoneGroupingAsync(zoneId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { message = $"Zone {zoneId} grouping synchronized successfully" });
        }

        _logger.LogError("❌ Failed to synchronize zone {ZoneId} grouping: {Error}", zoneId, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Ensures a specific client is in the correct group for its assigned zone.
    /// </summary>
    /// <param name="clientId">Client ID to group</param>
    /// <param name="zoneId">Zone ID the client should be grouped with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Client grouping result</returns>
    [HttpPost("clients/{clientId:int}/assign-to-zone/{zoneId:int}")]
    public async Task<IActionResult> EnsureClientInZoneGroup(
        int clientId,
        int zoneId,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("📍 Ensuring client {ClientId} is in zone {ZoneId} group", clientId, zoneId);

        var result = await _zoneGroupingService.EnsureClientInZoneGroupAsync(clientId, zoneId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { message = $"Client {clientId} successfully assigned to zone {zoneId} group" });
        }

        _logger.LogError(
            "❌ Failed to assign client {ClientId} to zone {ZoneId} group: {Error}",
            clientId,
            zoneId,
            result.ErrorMessage
        );
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Performs a full reconciliation of all zone groupings.
    /// This corrects any inconsistencies between logical zone assignments and physical Snapcast grouping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation result with detailed actions taken</returns>
    [HttpPost("reconcile")]
    public async Task<IActionResult> ReconcileAllZoneGroupings(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔧 Starting full zone grouping reconciliation");

        var result = await _zoneGroupingService.ReconcileAllZoneGroupingsAsync(cancellationToken);

        if (result.IsSuccess)
        {
            var reconciliation = result.Value!;
            _logger.LogInformation(
                "✅ Zone grouping reconciliation completed: {ZonesReconciled} zones, {ClientsMoved} clients moved",
                reconciliation.ZonesReconciled,
                reconciliation.ClientsMoved
            );

            return Ok(reconciliation);
        }

        _logger.LogError("❌ Zone grouping reconciliation failed: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }
}
