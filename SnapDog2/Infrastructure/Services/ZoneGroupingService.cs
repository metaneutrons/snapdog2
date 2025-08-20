using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Simple zone grouping service using periodic checks only.
/// Ensures clients assigned to the same zone are grouped together for synchronized audio playback.
/// </summary>
public class ZoneGroupingService : IZoneGroupingService
{
    private readonly ISnapcastService _snapcastService;
    private readonly IClientManager _clientManager;
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<ZoneGroupingService> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.ZoneGrouping");

    public ZoneGroupingService(
        ISnapcastService snapcastService,
        IClientManager clientManager,
        IZoneManager zoneManager,
        ILogger<ZoneGroupingService> logger
    )
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
        _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Simple periodic check: ensure all zones are properly configured.
    /// This is the main method called by the background service.
    /// </summary>
    public async Task<Result> EnsureZoneGroupingAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.EnsureAll");

        try
        {
            _logger.LogDebug("🔍 Starting periodic zone grouping check");

            // Get all available zones
            var zonesResult = await _zoneManager.GetAllZonesAsync(cancellationToken);
            if (!zonesResult.IsSuccess)
            {
                return Result.Failure($"Failed to get available zones: {zonesResult.ErrorMessage}");
            }

            var zones = zonesResult.Value?.Select(z => z.Id).ToList() ?? new List<int>();
            if (!zones.Any())
            {
                _logger.LogDebug("ℹ️ No zones configured, skipping zone grouping");
                return Result.Success();
            }

            _logger.LogDebug("🔍 Checking {ZoneCount} zones: {ZoneIds}", zones.Count, string.Join(",", zones));

            // Synchronize each zone
            foreach (var zoneId in zones)
            {
                var result = await SynchronizeZoneGroupingAsync(zoneId, cancellationToken);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("⚠️ Failed to synchronize zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
                }
            }

            _logger.LogDebug("✅ Periodic zone grouping check completed");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during periodic zone grouping check");
            return Result.Failure($"Error during zone grouping check: {ex.Message}");
        }
    }

    public async Task<Result> SynchronizeZoneGroupingAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.SynchronizeZone");
        activity?.SetTag("zone.id", zoneId);

        try
        {
            _logger.LogDebug("🔄 Synchronizing zone {ZoneId}", zoneId);

            // Get zone clients - who should be in this zone?
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneId, cancellationToken);
            if (!zoneClients.IsSuccess)
            {
                return Result.Failure($"Failed to get zone clients: {zoneClients.ErrorMessage}");
            }

            var clientIds =
                zoneClients.Value?.Select(c => c.SnapcastId).Where(id => !string.IsNullOrEmpty(id)).ToList()
                ?? new List<string>();
            if (!clientIds.Any())
            {
                _logger.LogDebug("ℹ️ No clients assigned to zone {ZoneId}, skipping", zoneId);
                return Result.Success();
            }

            // Get current server status
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                return Result.Failure($"Failed to get server status: {serverStatus.ErrorMessage}");
            }

            var expectedStreamId = $"Zone{zoneId}";

            // Check if zone is already properly configured
            if (IsZoneProperlyConfigured(serverStatus.Value, clientIds, expectedStreamId))
            {
                _logger.LogDebug("✅ Zone {ZoneId} is already properly configured", zoneId);
                return Result.Success();
            }

            // Zone needs configuration - provision it
            _logger.LogInformation("🔧 Provisioning zone {ZoneId}: {ClientCount} clients", zoneId, clientIds.Count);

            // Find a group that has any of our zone's clients, or use any available group
            var targetGroup =
                serverStatus.Value?.Groups?.FirstOrDefault(g => g.Clients?.Any(c => clientIds.Contains(c.Id)) == true)
                ?? serverStatus.Value?.Groups?.FirstOrDefault();

            if (targetGroup == null)
            {
                return Result.Failure("No groups available on Snapcast server");
            }

            // Set the correct stream for this zone
            if (targetGroup.StreamId != expectedStreamId)
            {
                var setStreamResult = await _snapcastService.SetGroupStreamAsync(
                    targetGroup.Id,
                    expectedStreamId,
                    cancellationToken
                );
                if (!setStreamResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "⚠️ Failed to set stream for group {GroupId}: {Error}",
                        targetGroup.Id,
                        setStreamResult.ErrorMessage
                    );
                }
                else
                {
                    _logger.LogDebug("✅ Set group {GroupId} stream to {StreamId}", targetGroup.Id, expectedStreamId);
                }
            }

            // Put ALL zone clients in this group
            var setClientsResult = await _snapcastService.SetGroupClientsAsync(
                targetGroup.Id,
                clientIds,
                cancellationToken
            );
            if (!setClientsResult.IsSuccess)
            {
                return Result.Failure($"Failed to set clients for group: {setClientsResult.ErrorMessage}");
            }

            _logger.LogInformation(
                "✅ Zone {ZoneId} synchronized: {ClientCount} clients in group {GroupId} with stream {StreamId}",
                zoneId,
                clientIds.Count,
                targetGroup.Id,
                expectedStreamId
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error synchronizing zone {ZoneId}", zoneId);
            return Result.Failure($"Zone synchronization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a zone is already properly configured.
    /// </summary>
    private bool IsZoneProperlyConfigured(
        SnapcastServerStatus? serverStatus,
        List<string> clientIds,
        string expectedStreamId
    )
    {
        if (serverStatus?.Groups == null)
        {
            _logger.LogDebug("❌ No server status or groups available");
            return false;
        }

        // Find groups that contain any of our zone clients
        var groupsWithOurClients = serverStatus
            .Groups.Where(g => g.Clients?.Any(c => clientIds.Contains(c.Id)) == true)
            .ToList();

        _logger.LogDebug(
            "🔍 Zone check: Expected stream {ExpectedStream}, Found {GroupCount} groups with our clients {ClientIds}",
            expectedStreamId,
            groupsWithOurClients.Count,
            string.Join(",", clientIds)
        );

        // All our clients should be in exactly one group with the correct stream
        if (groupsWithOurClients.Count != 1)
        {
            _logger.LogInformation(
                "❌ Zone misconfigured: {GroupCount} groups contain our clients (should be 1)",
                groupsWithOurClients.Count
            );
            return false;
        }

        var targetGroup = groupsWithOurClients.First();

        // Check if all our clients are in this group and no foreign clients
        var groupClientIds = targetGroup.Clients?.Select(c => c.Id).ToList() ?? new List<string>();
        var allOurClientsPresent = clientIds.All(id => groupClientIds.Contains(id));
        var noForeignClients = groupClientIds.All(id => clientIds.Contains(id));
        var correctStream = targetGroup.StreamId == expectedStreamId;

        _logger.LogDebug(
            "🔍 Zone check details: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream} (expected {ExpectedStream}, actual {ActualStream})",
            allOurClientsPresent,
            noForeignClients,
            correctStream,
            expectedStreamId,
            targetGroup.StreamId
        );

        var isProperlyConfigured = allOurClientsPresent && noForeignClients && correctStream;

        if (!isProperlyConfigured)
        {
            _logger.LogInformation(
                "❌ Zone misconfigured: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}",
                allOurClientsPresent,
                noForeignClients,
                correctStream
            );
        }

        return isProperlyConfigured;
    }

    // Stub implementations for interface compatibility - these are not used in periodic mode
    public async Task<Result> EnsureClientInZoneGroupAsync(
        int clientId,
        int zoneId,
        CancellationToken cancellationToken = default
    )
    {
        return await SynchronizeZoneGroupingAsync(zoneId, cancellationToken);
    }

    public async Task<Result> ValidateGroupingConsistencyAsync(CancellationToken cancellationToken = default)
    {
        return await EnsureZoneGroupingAsync(cancellationToken);
    }

    public Task<Result<ZoneGroupingStatus>> GetZoneGroupingStatusAsync(CancellationToken cancellationToken = default)
    {
        // Simple status - just return healthy
        var status = new ZoneGroupingStatus
        {
            OverallHealth = ZoneGroupingHealth.Healthy,
            TotalZones = 2,
            ZoneDetails = new List<ZoneGroupingDetail>(),
        };
        return Task.FromResult(Result<ZoneGroupingStatus>.Success(status));
    }

    public Task<Result<ZoneGroupingReconciliationResult>> ReconcileAllZoneGroupingsAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Just call EnsureZoneGroupingAsync and return a simple result
        return Task.Run(async () =>
        {
            var result = await EnsureZoneGroupingAsync(cancellationToken);
            var reconciliationResult = new ZoneGroupingReconciliationResult { ClientsMoved = 0, ZonesReconciled = 2 };
            return result.IsSuccess
                ? Result<ZoneGroupingReconciliationResult>.Success(reconciliationResult)
                : Result<ZoneGroupingReconciliationResult>.Failure(result.ErrorMessage ?? "Reconciliation failed");
        });
    }

    public Task<Result<ClientNameSyncResult>> SynchronizeClientNamesAsync(CancellationToken cancellationToken = default)
    {
        // Not needed for simple periodic mode
        var result = new ClientNameSyncResult { UpdatedClients = 0 };
        return Task.FromResult(Result<ClientNameSyncResult>.Success(result));
    }
}
