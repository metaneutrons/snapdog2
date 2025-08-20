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
            _logger.LogDebug("🔍 Starting zone grouping check for all zones");

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
                _logger.LogDebug("🔧 Checking zone {ZoneId}...", zoneId);
                var result = await SynchronizeZoneGroupingAsync(zoneId, cancellationToken);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("⚠️ Failed to synchronize zone {ZoneId}: {Error}", zoneId, result.ErrorMessage);
                }
                else
                {
                    _logger.LogDebug("✅ Zone {ZoneId} check completed", zoneId);
                }
            }

            _logger.LogDebug("✅ All zone grouping checks completed");
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
                _logger.LogDebug(
                    "✅ Zone {ZoneId} is already properly configured (clients: {ClientIds}, stream: {StreamId})",
                    zoneId,
                    string.Join(",", clientIds),
                    expectedStreamId
                );
                return Result.Success();
            }

            // Zone needs configuration - provision it (KEEP THIS AT INFO - actual work!)
            _logger.LogInformation(
                "🔧 Provisioning zone {ZoneId}: {ClientCount} clients ({ClientIds}) with stream {StreamId}",
                zoneId,
                clientIds.Count,
                string.Join(",", clientIds),
                expectedStreamId
            );

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

            // Set the correct group name for this zone
            var expectedGroupName = GetExpectedZoneName(zoneId);
            if (targetGroup.Name != expectedGroupName)
            {
                var setGroupNameResult = await _snapcastService.SetGroupNameAsync(
                    targetGroup.Id,
                    expectedGroupName,
                    cancellationToken
                );
                if (!setGroupNameResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "⚠️ Failed to set name for group {GroupId}: {Error}",
                        targetGroup.Id,
                        setGroupNameResult.ErrorMessage
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "✅ Set group {GroupId} name to '{GroupName}'",
                        targetGroup.Id,
                        expectedGroupName
                    );
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

            // Synchronize client names to match configuration
            await SynchronizeClientNamesAsync(zoneId, cancellationToken);

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

        // Check group name - extract zone ID from stream and get expected zone name
        var zoneId = int.Parse(expectedStreamId.Replace("Zone", ""));
        var expectedGroupName = GetExpectedZoneName(zoneId);
        var correctGroupName = targetGroup.Name == expectedGroupName;

        // Check client names - all clients should have their configured names (not null)
        var correctClientNames = true;
        if (targetGroup.Clients != null)
        {
            foreach (var client in targetGroup.Clients)
            {
                if (string.IsNullOrEmpty(client.Name))
                {
                    correctClientNames = false;
                    break;
                }
            }
        }

        _logger.LogDebug(
            "🔍 Zone check details: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}, CorrectGroupName={CorrectGroupName} (expected '{ExpectedName}', actual '{ActualName}'), CorrectClientNames={CorrectClientNames}",
            allOurClientsPresent,
            noForeignClients,
            correctStream,
            correctGroupName,
            expectedGroupName,
            targetGroup.Name,
            correctClientNames
        );

        var isProperlyConfigured =
            allOurClientsPresent && noForeignClients && correctStream && correctGroupName && correctClientNames;

        if (!isProperlyConfigured)
        {
            _logger.LogInformation(
                "❌ Zone misconfigured: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}, CorrectGroupName={CorrectGroupName}, CorrectClientNames={CorrectClientNames}",
                allOurClientsPresent,
                noForeignClients,
                correctStream,
                correctGroupName,
                correctClientNames
            );
        }

        return isProperlyConfigured;
    }

    /// <summary>
    /// Gets the expected zone name for a given zone ID.
    /// </summary>
    private string GetExpectedZoneName(int zoneId)
    {
        // This should match the zone names from configuration
        // Zone 1 = "Ground Floor", Zone 2 = "1st Floor"
        return zoneId switch
        {
            1 => "Ground Floor",
            2 => "1st Floor",
            _ => $"Zone {zoneId}",
        };
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

    /// <summary>
    /// Synchronizes client names for a specific zone to match configuration.
    /// </summary>
    private async Task SynchronizeClientNamesAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🏷️ Starting client name synchronization for zone {ZoneId}", zoneId);

            // Get zone clients with their configured names
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneId, cancellationToken);
            if (!zoneClients.IsSuccess || zoneClients.Value == null)
            {
                _logger.LogWarning(
                    "⚠️ Failed to get zone clients for name synchronization: {Error}",
                    zoneClients.ErrorMessage
                );
                return;
            }

            _logger.LogDebug(
                "🔍 Found {ClientCount} clients for zone {ZoneId}: {ClientNames}",
                zoneClients.Value.Count,
                zoneId,
                string.Join(", ", zoneClients.Value.Select(c => $"{c.SnapcastId}='{c.Name}'"))
            );

            // Get current server status to check actual client names
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess || serverStatus.Value?.Groups == null)
            {
                _logger.LogWarning(
                    "⚠️ Failed to get server status for client name synchronization: {Error}",
                    serverStatus.ErrorMessage
                );
                return;
            }

            // Create a map of snapcast client ID to current name
            var currentClientNames = new Dictionary<string, string?>();
            foreach (var group in serverStatus.Value.Groups)
            {
                if (group.Clients != null)
                {
                    foreach (var client in group.Clients)
                    {
                        currentClientNames[client.Id] = client.Name;
                    }
                }
            }

            _logger.LogDebug(
                "🔍 Current Snapcast client names: {CurrentNames}",
                string.Join(", ", currentClientNames.Select(kvp => $"{kvp.Key}='{kvp.Value}'"))
            );

            // Check and update client names
            foreach (var client in zoneClients.Value)
            {
                if (string.IsNullOrEmpty(client.SnapcastId))
                {
                    continue;
                }

                var expectedName = client.Name;
                var currentName = currentClientNames.GetValueOrDefault(client.SnapcastId);

                _logger.LogDebug(
                    "🔍 Checking client {ClientId}: expected='{ExpectedName}', current='{CurrentName}'",
                    client.SnapcastId,
                    expectedName,
                    currentName
                );

                if (currentName != expectedName)
                {
                    // KEEP THIS AT INFO - actual work being done!
                    _logger.LogInformation(
                        "🏷️ Setting client {ClientId} name from '{CurrentName}' to '{ExpectedName}'",
                        client.SnapcastId,
                        currentName,
                        expectedName
                    );

                    var setNameResult = await _snapcastService.SetClientNameAsync(
                        client.SnapcastId,
                        expectedName,
                        cancellationToken
                    );

                    if (setNameResult.IsSuccess)
                    {
                        // KEEP THIS AT INFO - successful work completed!
                        _logger.LogInformation(
                            "✅ Set client {ClientId} name to '{ClientName}'",
                            client.SnapcastId,
                            expectedName
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Failed to set name for client {ClientId}: {Error}",
                            client.SnapcastId,
                            setNameResult.ErrorMessage
                        );
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "✅ Client {ClientId} name is already correct: '{ClientName}'",
                        client.SnapcastId,
                        expectedName
                    );
                }
            }

            _logger.LogDebug("✅ Client name synchronization completed for zone {ZoneId}", zoneId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error synchronizing client names for zone {ZoneId}", zoneId);
        }
    }
}
