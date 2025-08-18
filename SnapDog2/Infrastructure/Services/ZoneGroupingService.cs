using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Enterprise-grade service for managing Snapcast client grouping based on zone assignments.
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

    public async Task<Result> SynchronizeZoneGroupingAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.SynchronizeZone");
        activity?.SetTag("zone.id", zoneId);

        try
        {
            _logger.LogInformation("🔄 Starting zone grouping synchronization for zone {ZoneId}", zoneId);

            // Get zone information
            var zone = await _zoneManager.GetZoneAsync(zoneId, cancellationToken);
            if (!zone.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to get zone {ZoneId}: {Error}",
                    zoneId,
                    zone.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(zone.ErrorMessage ?? "Failed to get zone information");
            }

            activity?.SetTag("zone.name", zone.Value!.Name);

            // Get clients assigned to this zone
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneId, cancellationToken);
            if (!zoneClients.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to get clients for zone {ZoneId}: {Error}",
                    zoneId,
                    zoneClients.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(zoneClients.ErrorMessage ?? "Failed to get zone clients");
            }

            if (zoneClients.Value?.Any() != true)
            {
                _logger.LogInformation("ℹ️ No clients assigned to zone {ZoneId}, skipping grouping", zoneId);
                return Result.Success();
            }

            // Get current Snapcast server status
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to get Snapcast server status: {Error}",
                    serverStatus.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(serverStatus.ErrorMessage ?? "Failed to get server status");
            }

            // Find or create target group for this zone
            var targetGroup = await FindOrCreateZoneGroupAsync(
                zoneId,
                zone.Value!.Name,
                serverStatus.Value!,
                cancellationToken
            );
            if (!targetGroup.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to find/create group for zone {ZoneId}: {Error}",
                    zoneId,
                    targetGroup.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(targetGroup.ErrorMessage ?? "Failed to find/create group");
            }

            // Move all zone clients to the target group
            var clientIds = zoneClients
                .Value!.Select(c => c.SnapcastId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();
            if (clientIds.Any())
            {
                var moveResult = await MoveClientsToGroupAsync(clientIds, targetGroup.Value!.Id, cancellationToken);
                if (!moveResult.IsSuccess)
                {
                    _logger.LogError(
                        "❌ Failed to move clients to group for zone {ZoneId}: {Error}",
                        zoneId,
                        moveResult.ErrorMessage
                    );
                    return moveResult;
                }

                _logger.LogInformation(
                    "✅ Successfully synchronized {ClientCount} clients to zone {ZoneId} group {GroupId}",
                    clientIds.Count,
                    zoneId,
                    targetGroup.Value.Id
                );
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during zone grouping synchronization for zone {ZoneId}", zoneId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure($"Zone grouping synchronization failed: {ex.Message}");
        }
    }

    public async Task<Result> EnsureClientInZoneGroupAsync(
        int clientId,
        int zoneId,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.EnsureClientInZoneGroup");
        activity?.SetTag("client.id", clientId);
        activity?.SetTag("zone.id", zoneId);

        try
        {
            _logger.LogInformation("📍 Ensuring client {ClientId} is in zone {ZoneId} group", clientId, zoneId);

            // Get client information
            var client = await _clientManager.GetClientAsync(clientId, cancellationToken);
            if (!client.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to get client {ClientId}: {Error}",
                    clientId,
                    client.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(client.ErrorMessage ?? "Failed to get client information");
            }

            if (string.IsNullOrEmpty(client.Value!.SnapcastId))
            {
                _logger.LogWarning("⚠️ Client {ClientId} has no Snapcast client ID, skipping grouping", clientId);
                return Result.Success();
            }

            activity?.SetTag("client.name", client.Value.Name);
            activity?.SetTag("client.snapcast_id", client.Value.SnapcastId);

            // Get zone information
            var zone = await _zoneManager.GetZoneAsync(zoneId, cancellationToken);
            if (!zone.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to get zone {ZoneId}: {Error}",
                    zoneId,
                    zone.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(zone.ErrorMessage ?? "Failed to get zone information");
            }

            // Get current Snapcast server status
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to get Snapcast server status: {Error}",
                    serverStatus.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(serverStatus.ErrorMessage ?? "Failed to get server status");
            }

            // Find current group of the client
            var currentGroup = FindClientCurrentGroup(client.Value.SnapcastId, serverStatus.Value!);

            // Find or create target group for the zone
            var targetGroup = await FindOrCreateZoneGroupAsync(
                zoneId,
                zone.Value!.Name,
                serverStatus.Value!,
                cancellationToken
            );
            if (!targetGroup.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to find/create group for zone {ZoneId}: {Error}",
                    zoneId,
                    targetGroup.ErrorMessage ?? "Unknown error"
                );
                return Result.Failure(targetGroup.ErrorMessage ?? "Failed to find/create group");
            }

            // Check if client is already in the correct group
            if (currentGroup?.Id == targetGroup.Value!.Id)
            {
                _logger.LogInformation(
                    "✅ Client {ClientId} is already in correct group {GroupId} for zone {ZoneId}",
                    clientId,
                    targetGroup.Value.Id,
                    zoneId
                );
                return Result.Success();
            }

            // Move client to target group
            var moveResult = await MoveClientsToGroupAsync(
                new[] { client.Value.SnapcastId },
                targetGroup.Value.Id,
                cancellationToken
            );
            if (!moveResult.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to move client {ClientId} to zone {ZoneId} group: {Error}",
                    clientId,
                    zoneId,
                    moveResult.ErrorMessage
                );
                return moveResult;
            }

            _logger.LogInformation(
                "✅ Successfully moved client {ClientId} ({ClientName}) to zone {ZoneId} group {GroupId}",
                clientId,
                client.Value.Name,
                zoneId,
                targetGroup.Value.Id
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "💥 Unexpected error ensuring client {ClientId} in zone {ZoneId} group",
                clientId,
                zoneId
            );
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure($"Client zone grouping failed: {ex.Message}");
        }
    }

    public async Task<Result> ValidateGroupingConsistencyAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.ValidateConsistency");

        try
        {
            _logger.LogInformation("🔍 Validating zone grouping consistency");

            var status = await GetZoneGroupingStatusAsync(cancellationToken);
            if (!status.IsSuccess)
            {
                return Result.Failure(status.ErrorMessage ?? "Failed to get zone grouping status");
            }

            var inconsistencies = status.Value!.ZoneDetails.Where(z => z.Health != ZoneGroupingHealth.Healthy).ToList();

            if (inconsistencies.Any())
            {
                var issues = inconsistencies.SelectMany(z => z.Issues).ToList();
                _logger.LogWarning(
                    "⚠️ Found {Count} zone grouping inconsistencies: {Issues}",
                    inconsistencies.Count,
                    string.Join(", ", issues)
                );

                return Result.Failure($"Grouping inconsistencies found: {string.Join(", ", issues)}");
            }

            _logger.LogInformation("✅ Zone grouping consistency validation passed");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during grouping consistency validation");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure($"Consistency validation failed: {ex.Message}");
        }
    }

    public async Task<Result<ZoneGroupingStatus>> GetZoneGroupingStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.GetStatus");

        try
        {
            _logger.LogDebug("📊 Collecting zone grouping status");

            // Get all zones and clients
            var zones = await _zoneManager.GetAllZonesAsync(cancellationToken);
            if (!zones.IsSuccess)
            {
                return Result<ZoneGroupingStatus>.Failure($"Failed to get zones: {zones.ErrorMessage}");
            }

            var allClients = await _clientManager.GetAllClientsAsync(cancellationToken);
            if (!allClients.IsSuccess)
            {
                return Result<ZoneGroupingStatus>.Failure($"Failed to get clients: {allClients.ErrorMessage}");
            }

            // Get current Snapcast server status
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                return Result<ZoneGroupingStatus>.Failure(
                    $"Failed to get Snapcast status: {serverStatus.ErrorMessage}"
                );
            }

            // Analyze each zone
            var zoneDetails = new List<ZoneGroupingDetail>();
            var totalClients = 0;
            var correctlyGroupedClients = 0;

            foreach (var zone in zones.Value ?? Enumerable.Empty<ZoneState>())
            {
                var zoneClients =
                    allClients.Value?.Where(c => c.ZoneIndex == zone.Id).ToList() ?? new List<ClientState>();
                totalClients += zoneClients.Count;

                var detail = AnalyzeZoneGrouping(zone, zoneClients, serverStatus.Value!);
                zoneDetails.Add(detail);

                correctlyGroupedClients += detail.ExpectedClients.Count(c => c.IsCorrectlyGrouped);
            }

            // Calculate overall health
            var healthyZones = zoneDetails.Count(z => z.Health == ZoneGroupingHealth.Healthy);
            var unhealthyZones = zoneDetails.Count(z => z.Health == ZoneGroupingHealth.Unhealthy);

            var overallHealth =
                unhealthyZones == 0 ? ZoneGroupingHealth.Healthy
                : healthyZones > 0 ? ZoneGroupingHealth.Degraded
                : ZoneGroupingHealth.Unhealthy;

            var status = new ZoneGroupingStatus
            {
                OverallHealth = overallHealth,
                TotalZones = zones.Value?.Count() ?? 0,
                HealthyZones = healthyZones,
                UnhealthyZones = unhealthyZones,
                TotalClients = totalClients,
                CorrectlyGroupedClients = correctlyGroupedClients,
                ZoneDetails = zoneDetails,
                Issues = zoneDetails.SelectMany(z => z.Issues).ToList(),
            };

            _logger.LogDebug(
                "📊 Zone grouping status: {Health} ({HealthyZones}/{TotalZones} zones, {CorrectClients}/{TotalClients} clients)",
                overallHealth,
                healthyZones,
                zones.Value?.Count() ?? 0,
                correctlyGroupedClients,
                totalClients
            );

            return Result<ZoneGroupingStatus>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error collecting zone grouping status");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result<ZoneGroupingStatus>.Failure($"Status collection failed: {ex.Message}");
        }
    }

    public async Task<Result<ZoneGroupingReconciliationResult>> ReconcileAllZoneGroupingsAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.ReconcileAll");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("🔧 Starting full zone grouping reconciliation");

            var actions = new List<string>();
            var errors = new List<string>();
            var zonesReconciled = 0;
            var clientsMoved = 0;

            // Get all zones
            var zones = await _zoneManager.GetAllZonesAsync(cancellationToken);
            if (!zones.IsSuccess)
            {
                return Result<ZoneGroupingReconciliationResult>.Failure($"Failed to get zones: {zones.ErrorMessage}");
            }

            // Reconcile each zone
            foreach (var zone in zones.Value ?? Enumerable.Empty<ZoneState>())
            {
                try
                {
                    var result = await SynchronizeZoneGroupingAsync(zone.Id, cancellationToken);
                    if (result.IsSuccess)
                    {
                        zonesReconciled++;
                        actions.Add($"Reconciled zone {zone.Id} ({zone.Name})");
                    }
                    else
                    {
                        errors.Add($"Failed to reconcile zone {zone.Id}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Exception reconciling zone {zone.Id}: {ex.Message}");
                }
            }

            stopwatch.Stop();

            var reconciliationResult = new ZoneGroupingReconciliationResult
            {
                ZonesReconciled = zonesReconciled,
                ClientsMoved = clientsMoved, // TODO: Track actual client moves
                GroupsCreated = 0, // TODO: Track group creation
                GroupsRemoved = 0, // TODO: Track group removal
                Actions = actions,
                Errors = errors,
                Duration = stopwatch.Elapsed,
            };

            _logger.LogInformation(
                "✅ Zone grouping reconciliation completed: {ZonesReconciled} zones reconciled in {Duration}ms",
                zonesReconciled,
                stopwatch.ElapsedMilliseconds
            );

            return Result<ZoneGroupingReconciliationResult>.Success(reconciliationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during zone grouping reconciliation");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result<ZoneGroupingReconciliationResult>.Failure($"Reconciliation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes client names between SnapDog configuration and Snapcast server.
    /// Sets friendly names from SnapDog config to replace MAC address-based names in Snapcast.
    /// </summary>
    public async Task<Result<ClientNameSyncResult>> SynchronizeClientNamesAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var activity = ActivitySource.StartActivity("SynchronizeClientNames");
        _logger.LogInformation("🏷️ Starting client name synchronization");

        var startTime = DateTime.UtcNow;
        var syncResult = new ClientNameSyncResult { StartTime = startTime };

        try
        {
            // Get all clients from SnapDog configuration
            var allClients = await _clientManager.GetAllClientsAsync(cancellationToken);
            if (!allClients.IsSuccess)
            {
                return Result<ClientNameSyncResult>.Failure(
                    $"Failed to get clients: {allClients.ErrorMessage ?? "Unknown error"}"
                );
            }

            // Get current Snapcast server status
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                return Result<ClientNameSyncResult>.Failure(
                    $"Failed to get server status: {serverStatus.ErrorMessage ?? "Unknown error"}"
                );
            }

            var clientsToSync =
                allClients.Value?.Where(c => !string.IsNullOrEmpty(c.SnapcastId)).ToList() ?? new List<ClientState>();
            syncResult.TotalClients = clientsToSync.Count;

            foreach (var client in clientsToSync)
            {
                try
                {
                    // Find the corresponding Snapcast client
                    var snapcastClient = serverStatus
                        .Value!.Groups?.SelectMany(g => g.Clients ?? Enumerable.Empty<SnapcastClientInfo>())
                        .FirstOrDefault(sc => sc.Id == client.SnapcastId);

                    if (snapcastClient == null)
                    {
                        _logger.LogWarning(
                            "⚠️ Snapcast client {SnapcastId} not found for SnapDog client {ClientName}",
                            client.SnapcastId,
                            client.Name
                        );
                        syncResult.SkippedClients++;
                        continue;
                    }

                    // Check if name needs updating
                    var currentName = snapcastClient.Name ?? "";
                    var desiredName = client.Name;

                    if (currentName == desiredName)
                    {
                        _logger.LogDebug(
                            "✅ Client {SnapcastId} already has correct name: {Name}",
                            client.SnapcastId,
                            desiredName
                        );
                        syncResult.AlreadyCorrect++;
                        continue;
                    }

                    // Update the client name
                    _logger.LogInformation(
                        "🏷️ Setting client {SnapcastId} name from '{CurrentName}' to '{DesiredName}'",
                        client.SnapcastId,
                        currentName,
                        desiredName
                    );

                    var setNameResult = await _snapcastService.SetClientNameAsync(
                        client.SnapcastId,
                        desiredName,
                        cancellationToken
                    );

                    if (setNameResult.IsSuccess)
                    {
                        syncResult.UpdatedClients++;
                        syncResult.UpdatedClientNames.Add(
                            new ClientNameUpdate
                            {
                                SnapcastId = client.SnapcastId,
                                OldName = currentName,
                                NewName = desiredName,
                            }
                        );
                        _logger.LogInformation(
                            "✅ Successfully updated client {SnapcastId} name to '{Name}'",
                            client.SnapcastId,
                            desiredName
                        );
                    }
                    else
                    {
                        syncResult.FailedClients++;
                        _logger.LogError(
                            "❌ Failed to update client {SnapcastId} name: {Error}",
                            client.SnapcastId,
                            setNameResult.ErrorMessage
                        );
                    }
                }
                catch (Exception ex)
                {
                    syncResult.FailedClients++;
                    _logger.LogError(ex, "❌ Error synchronizing name for client {SnapcastId}", client.SnapcastId);
                }
            }

            var endTime = DateTime.UtcNow;
            var finalResult = syncResult with { EndTime = endTime, Duration = endTime - startTime };

            _logger.LogInformation(
                "🏷️ Client name synchronization completed: {Updated} updated, {AlreadyCorrect} already correct, {Skipped} skipped, {Failed} failed in {Duration}ms",
                finalResult.UpdatedClients,
                finalResult.AlreadyCorrect,
                finalResult.SkippedClients,
                finalResult.FailedClients,
                finalResult.Duration.TotalMilliseconds
            );

            return Result<ClientNameSyncResult>.Success(finalResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Client name synchronization failed");
            return Result<ClientNameSyncResult>.Failure($"Client name synchronization failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<Result<SnapcastGroupInfo>> FindOrCreateZoneGroupAsync(
        int zoneId,
        string zoneName,
        SnapcastServerStatus serverStatus,
        CancellationToken cancellationToken
    )
    {
        // Determine the correct stream ID for this zone
        var expectedStreamId = $"Zone{zoneId}";

        _logger.LogDebug("🎯 Looking for group with stream {StreamId} for zone {ZoneId}", expectedStreamId, zoneId);

        // Look for existing group with the correct stream ID
        var existingGroupWithCorrectStream = serverStatus.Groups?.FirstOrDefault(g => g.StreamId == expectedStreamId);

        if (existingGroupWithCorrectStream != null)
        {
            _logger.LogDebug(
                "✅ Found existing group {GroupId} with correct stream {StreamId} for zone {ZoneId}",
                existingGroupWithCorrectStream.Id,
                expectedStreamId,
                zoneId
            );
            return Result<SnapcastGroupInfo>.Success(existingGroupWithCorrectStream);
        }

        // Look for existing group with zone clients but wrong stream
        var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneId, cancellationToken);
        if (!zoneClients.IsSuccess)
        {
            return Result<SnapcastGroupInfo>.Failure($"Failed to get zone clients: {zoneClients.ErrorMessage}");
        }

        var zoneClientIds =
            zoneClients.Value?.Select(c => c.SnapcastId).Where(id => !string.IsNullOrEmpty(id)).ToHashSet()
            ?? new HashSet<string>();

        // Find group that contains any of the zone's clients
        var existingGroupWithClients = serverStatus.Groups?.FirstOrDefault(g =>
            g.Clients?.Any(c => zoneClientIds.Contains(c.Id)) == true
        );

        if (existingGroupWithClients != null)
        {
            _logger.LogDebug(
                "🔧 Found group {GroupId} with zone clients but wrong stream {CurrentStream}, fixing to {ExpectedStream}",
                existingGroupWithClients.Id,
                existingGroupWithClients.StreamId,
                expectedStreamId
            );

            // Fix the stream ID for this group
            var setStreamResult = await _snapcastService.SetGroupStreamAsync(
                existingGroupWithClients.Id,
                expectedStreamId,
                cancellationToken
            );

            if (!setStreamResult.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to set stream {StreamId} for group {GroupId}: {Error}",
                    expectedStreamId,
                    existingGroupWithClients.Id,
                    setStreamResult.ErrorMessage
                );
                return Result<SnapcastGroupInfo>.Failure(
                    $"Failed to set correct stream: {setStreamResult.ErrorMessage}"
                );
            }

            _logger.LogInformation(
                "✅ Fixed group {GroupId} stream to {StreamId} for zone {ZoneId}",
                existingGroupWithClients.Id,
                expectedStreamId,
                zoneId
            );

            return Result<SnapcastGroupInfo>.Success(existingGroupWithClients);
        }

        // Use any available group and set correct stream
        var availableGroup = serverStatus.Groups?.FirstOrDefault();
        if (availableGroup != null)
        {
            _logger.LogDebug(
                "🔧 Using available group {GroupId} and setting stream to {StreamId} for zone {ZoneId}",
                availableGroup.Id,
                expectedStreamId,
                zoneId
            );

            var setStreamResult = await _snapcastService.SetGroupStreamAsync(
                availableGroup.Id,
                expectedStreamId,
                cancellationToken
            );

            if (!setStreamResult.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to set stream {StreamId} for group {GroupId}: {Error}",
                    expectedStreamId,
                    availableGroup.Id,
                    setStreamResult.ErrorMessage
                );
                return Result<SnapcastGroupInfo>.Failure($"Failed to set stream: {setStreamResult.ErrorMessage}");
            }

            _logger.LogInformation(
                "✅ Set group {GroupId} stream to {StreamId} for zone {ZoneId}",
                availableGroup.Id,
                expectedStreamId,
                zoneId
            );

            return Result<SnapcastGroupInfo>.Success(availableGroup);
        }

        return Result<SnapcastGroupInfo>.Failure("No available groups found");
    }

    private SnapcastGroupInfo? FindClientCurrentGroup(string snapcastClientId, SnapcastServerStatus serverStatus)
    {
        return serverStatus.Groups?.FirstOrDefault(g => g.Clients?.Any(c => c.Id == snapcastClientId) == true);
    }

    private async Task<Result> MoveClientsToGroupAsync(
        IEnumerable<string> clientIds,
        string targetGroupId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var clientIdList = clientIds.ToList();
            _logger.LogDebug("🔄 Moving {ClientCount} clients to group {GroupId}", clientIdList.Count, targetGroupId);

            // Use Snapcast service to move clients
            var result = await _snapcastService.SetGroupClientsAsync(targetGroupId, clientIdList, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "❌ Failed to move clients to group {GroupId}: {Error}",
                    targetGroupId,
                    result.ErrorMessage
                );
                return result;
            }

            _logger.LogDebug(
                "✅ Successfully moved {ClientCount} clients to group {GroupId}",
                clientIdList.Count,
                targetGroupId
            );
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error moving clients to group {GroupId}", targetGroupId);
            return Result.Failure($"Failed to move clients to group: {ex.Message}");
        }
    }

    private ZoneGroupingDetail AnalyzeZoneGrouping(
        ZoneState zone,
        IList<ClientState> zoneClients,
        SnapcastServerStatus serverStatus
    )
    {
        var issues = new List<string>();
        var clientDetails = new List<ZoneClientDetail>();

        // Analyze each client in the zone
        foreach (var client in zoneClients)
        {
            if (string.IsNullOrEmpty(client.SnapcastId))
            {
                issues.Add($"Client {client.Name} has no Snapcast client ID");
                continue;
            }

            var currentGroup = FindClientCurrentGroup(client.SnapcastId, serverStatus);
            var isConnected = currentGroup?.Clients?.Any(c => c.Id == client.SnapcastId && c.Connected) == true;

            var clientDetail = new ZoneClientDetail
            {
                ClientId = client.Id,
                ClientName = client.Name,
                SnapcastClientId = client.SnapcastId,
                CurrentGroupId = currentGroup?.Id,
                IsConnected = isConnected,
                IsCorrectlyGrouped = false, // Will be determined below
            };

            clientDetails.Add(clientDetail);
        }

        // Determine if clients are correctly grouped together
        var connectedClients = clientDetails.Where(c => c.IsConnected).ToList();
        if (connectedClients.Count > 1)
        {
            var groupIds = connectedClients.Select(c => c.CurrentGroupId).Distinct().ToList();
            if (groupIds.Count > 1)
            {
                issues.Add($"Zone {zone.Name} clients are split across {groupIds.Count} groups");
            }
            else if (groupIds.Count == 1)
            {
                // All clients are in the same group - mark as correctly grouped
                for (int i = 0; i < clientDetails.Count; i++)
                {
                    if (clientDetails[i].IsConnected)
                    {
                        clientDetails[i] = clientDetails[i] with { IsCorrectlyGrouped = true };
                    }
                }
            }
        }
        else if (connectedClients.Count == 1)
        {
            // Single client is always correctly grouped
            var connectedIndex = clientDetails.FindIndex(c => c.IsConnected);
            if (connectedIndex >= 0)
            {
                clientDetails[connectedIndex] = clientDetails[connectedIndex] with { IsCorrectlyGrouped = true };
            }
        }

        // Determine overall zone health
        var health = issues.Any() ? ZoneGroupingHealth.Unhealthy : ZoneGroupingHealth.Healthy;

        return new ZoneGroupingDetail
        {
            ZoneId = zone.Id,
            ZoneName = zone.Name,
            ExpectedGroupId = connectedClients.FirstOrDefault()?.CurrentGroupId,
            ActualGroupIds = clientDetails.Select(c => c.CurrentGroupId).Where(id => id != null).Distinct().ToList()!,
            ExpectedClients = clientDetails,
            Health = health,
            Issues = issues,
        };
    }

    #endregion
}
