using SnapDog2.Core.Models;

namespace SnapDog2.Core.Abstractions;

/// <summary>
/// Service responsible for managing Snapcast client grouping based on zone assignments.
/// Ensures clients assigned to the same zone are grouped together in Snapcast for synchronized audio playback.
/// </summary>
public interface IZoneGroupingService
{
    /// <summary>
    /// Ensures all clients assigned to a specific zone are grouped together in Snapcast.
    /// </summary>
    /// <param name="zoneId">The zone ID to synchronize grouping for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the grouping operation</returns>
    Task<Result> SynchronizeZoneGroupingAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a specific client is placed in the correct Snapcast group for its assigned zone.
    /// </summary>
    /// <param name="clientId">The client ID to group</param>
    /// <param name="zoneId">The zone ID the client should be grouped with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the client grouping</returns>
    Task<Result> EnsureClientInZoneGroupAsync(int clientId, int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the current Snapcast grouping matches the logical zone assignments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating whether grouping is consistent</returns>
    Task<Result> ValidateGroupingConsistencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of zone-based grouping across all zones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive status of zone grouping</returns>
    Task<Result<ZoneGroupingStatus>> GetZoneGroupingStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full reconciliation of all zone groupings, correcting any inconsistencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with details of reconciliation actions taken</returns>
    Task<Result<ZoneGroupingReconciliationResult>> ReconcileAllZoneGroupingsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Synchronizes client names between SnapDog configuration and Snapcast server.
    /// Sets friendly names from SnapDog config to replace MAC address-based names in Snapcast.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with details of client name synchronization actions taken</returns>
    Task<Result<ClientNameSyncResult>> SynchronizeClientNamesAsync(CancellationToken cancellationToken = default);
}
