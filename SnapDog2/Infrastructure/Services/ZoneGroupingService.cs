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

namespace SnapDog2.Infrastructure.Services;

using System.Diagnostics;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Metrics;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Simple zone grouping service using periodic checks only.
/// Ensures clients assigned to the same zone are grouped together for synchronized audio playback.
/// </summary>
public partial class ZoneGroupingService(
    ISnapcastService snapcastService,
    IClientManager clientManager,
    IClientStateStore clientStateStore,
    IZoneManager zoneManager,
    ZoneGroupingMetrics metrics,
    IOptions<SnapcastConfig> config,
    ILogger<ZoneGroupingService> logger)
    : IZoneGroupingService, INotificationHandler<ClientZoneChangedNotification>
{
    private readonly ISnapcastService _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
    private readonly IClientManager _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
    private readonly IClientStateStore _clientStateStore = clientStateStore ?? throw new ArgumentNullException(nameof(clientStateStore));
    private readonly IZoneManager _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
    private readonly ZoneGroupingMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly SnapcastConfig _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<ZoneGroupingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly ActivitySource ActivitySource = new("SnapDog2.ZoneGrouping");

    private readonly TimeSpan _reconciliationInterval = TimeSpan.FromMilliseconds(config.Value.ZoneGroupingIntervalMs);
    private readonly SemaphoreSlim _regroupingSemaphore = new(1, 1);

    /// <summary>
    /// Triggers immediate zone regrouping without waiting for the periodic timer.
    /// Useful for immediate response to manual zone changes.
    /// </summary>
    public async Task TriggerImmediateRegroupingAsync()
    {
        if (this._regroupingSemaphore.Wait(0)) // Non-blocking check
        {
            try
            {
                await this.EnsureZoneGroupingAsync(CancellationToken.None);
            }
            finally
            {
                this._regroupingSemaphore.Release();
            }
        }
        // If already running, skip - the running operation will handle the latest state
    }

    /// <summary>
    /// Simple periodic check: ensure all zones are properly configured.
    /// This is the main method called by the background service.
    /// </summary>
    public async Task<Result> EnsureZoneGroupingAsync(CancellationToken cancellationToken = default)
    {
        await this._regroupingSemaphore.WaitAsync(cancellationToken);
        try
        {
            return await this.EnsureZoneGroupingInternalAsync(cancellationToken);
        }
        finally
        {
            this._regroupingSemaphore.Release();
        }
    }

    private async Task<Result> EnsureZoneGroupingInternalAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.EnsureAll");

        try
        {
            this.LogStartingZoneGroupingCheck();

            // Get all available zone services to extract their indices
            var zonesResult = await this._zoneManager.GetAllZonesAsync(cancellationToken);
            if (!zonesResult.IsSuccess)
            {
                return Result.Failure($"Failed to get available zones: {zonesResult.ErrorMessage}");
            }

            // We need to get the zone indices. Since we can't access the internal _zones dictionary,
            // let's try to get zone states and infer indices from successful zone retrievals
            var zones = new List<int>();

            // Try zones 1-10 (reasonable upper limit) and see which ones exist
            for (var i = 1; i <= 10; i++)
            {
                if (await this._zoneManager.ZoneExistsAsync(i))
                {
                    zones.Add(i);
                }
            }
            if (zones.Count == 0)
            {
                this.LogNoZonesConfigured();
                return Result.Success();
            }

            this.LogCheckingZones(zones.Count, string.Join(",", zones));

            // Synchronize each zone
            foreach (var zoneIndex in zones)
            {
                this.LogCheckingZone(zoneIndex);
                var result = await this.SynchronizeZoneGroupingAsync(zoneIndex, cancellationToken);
                if (!result.IsSuccess)
                {
                    this.LogFailedSynchronizeZone(zoneIndex, result.ErrorMessage);
                }
                else
                {
                    this.LogZoneCheckCompleted(zoneIndex);
                }
            }

            this.LogAllZoneGroupingChecksCompleted();
            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringPeriodicCheck(ex);
            return Result.Failure($"Error during zone grouping check: {ex.Message}");
        }
    }

    public async Task<Result> SynchronizeZoneGroupingAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.SynchronizeZone");
        activity?.SetTag("zone.id", zoneIndex);

        try
        {
            this.LogSynchronizingZone(zoneIndex);

            // Get zone clients from state store (for zone assignments) and merge with live Snapcast data (for SnapcastId)
            var allClientStates = this._clientStateStore.GetAllClientStates();
            Console.WriteLine($"DEBUG: State store for zone {zoneIndex}:");
            foreach (var kvp in allClientStates)
            {
                Console.WriteLine($"DEBUG:   Client {kvp.Key} -> Zone {kvp.Value.ZoneIndex}");
            }

            var zoneClientStates = allClientStates.Values
                .Where(c => c.ZoneIndex == zoneIndex)
                .ToList();
            Console.WriteLine($"DEBUG: Found {zoneClientStates.Count} clients assigned to zone {zoneIndex}");

            // Get live SnapcastId for each client in this zone
            var clientIndexs = new List<string>();
            foreach (var clientState in zoneClientStates)
            {
                Console.WriteLine($"DEBUG: Processing client {clientState.Id} for zone {zoneIndex}");
                // Get fresh client data to get current SnapcastId
                var clientResult = await this._clientManager.GetAllClientsAsync();
                if (clientResult.IsSuccess)
                {
                    var liveClient = clientResult.Value?.FirstOrDefault(c => c.Id == clientState.Id);
                    if (liveClient != null && !string.IsNullOrEmpty(liveClient.SnapcastId))
                    {
                        Console.WriteLine($"DEBUG: Adding {liveClient.SnapcastId} to zone {zoneIndex}");
                        clientIndexs.Add(liveClient.SnapcastId);
                    }
                }
            }

            Console.WriteLine($"DEBUG: Zone {zoneIndex} has {clientIndexs.Count} clients: [{string.Join(", ", clientIndexs)}]");

            if (clientIndexs.Count == 0)
            {
                this.LogNoClientsAssigned(zoneIndex);
                return Result.Success();
            }

            // Get current server status
            var serverStatus = await this._snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                return Result.Failure($"Failed to get server status: {serverStatus.ErrorMessage}");
            }

            var expectedStreamId = $"Zone{zoneIndex}";

            // Check if zone is already properly configured
            if (this.IsZoneProperlyConfigured(serverStatus.Value, clientIndexs, expectedStreamId))
            {
                // Find the group that contains our clients and update the zone manager
                var existingGroup = serverStatus.Value?.Groups
                    .FirstOrDefault(g => g.Clients.Any(c => clientIndexs.Contains(c.Id)));

                if (existingGroup != null)
                {
                    var zone = await this._zoneManager.GetZoneAsync(zoneIndex);
                    if (zone.IsSuccess)
                    {
                        zone.Value!.UpdateSnapcastGroupId(existingGroup.Id);
                    }
                }

                this.LogZoneAlreadyConfigured(zoneIndex, string.Join(",", clientIndexs), expectedStreamId);
                return Result.Success();
            }

            // Zone needs configuration - provision it (KEEP THIS AT INFO - actual work!)
            this.LogProvisioningZone(zoneIndex, clientIndexs.Count, string.Join(",", clientIndexs), expectedStreamId);

            // Find a group that has any of our zone's clients, or use any available group
            var targetGroup =
                serverStatus.Value?.Groups.FirstOrDefault(g => g.Clients.Any(c => clientIndexs.Contains(c.Id)))
                ?? serverStatus.Value?.Groups.FirstOrDefault();

            if (targetGroup == null)
            {
                return Result.Failure("No groups available on Snapcast server");
            }

            // Set the correct stream for this zone
            if (targetGroup.StreamId != expectedStreamId)
            {
                var setStreamResult = await this._snapcastService.SetGroupStreamAsync(
                    targetGroup.Id,
                    expectedStreamId,
                    cancellationToken
                );
                if (!setStreamResult.IsSuccess)
                {
                    this.LogFailedSetGroupStream(targetGroup.Id, setStreamResult.ErrorMessage);
                }
                else
                {
                    this.LogSetGroupStream(targetGroup.Id, expectedStreamId);
                }
            }

            // Set the correct group name for this zone
            var expectedGroupName = GetExpectedZoneName(zoneIndex);
            if (targetGroup.Name != expectedGroupName)
            {
                var setGroupNameResult = await this._snapcastService.SetGroupNameAsync(
                    targetGroup.Id,
                    expectedGroupName,
                    cancellationToken
                );
                if (!setGroupNameResult.IsSuccess)
                {
                    this.LogFailedSetGroupName(targetGroup.Id, setGroupNameResult.ErrorMessage);
                }
                else
                {
                    this.LogSetGroupName(targetGroup.Id, expectedGroupName);
                }
            }

            // Put ALL zone clients in this group
            var setClientsResult = await this._snapcastService.SetGroupClientsAsync(
                targetGroup.Id,
                clientIndexs,
                cancellationToken
            );
            if (!setClientsResult.IsSuccess)
            {
                return Result.Failure($"Failed to set clients for group: {setClientsResult.ErrorMessage}");
            }

            // Synchronize client names to match configuration
            await this.SynchronizeClientNamesAsync(zoneIndex, cancellationToken);

            this.LogZoneSynchronized(zoneIndex, clientIndexs.Count, targetGroup.Id, expectedStreamId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogErrorSynchronizingZone(ex, zoneIndex);
            return Result.Failure($"Zone synchronization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a zone is already properly configured.
    /// </summary>
    private bool IsZoneProperlyConfigured(
        SnapcastServerStatus? serverStatus,
        List<string> clientIndexs,
        string expectedStreamId
    )
    {
        if (serverStatus?.Groups == null)
        {
            this.LogNoServerStatusOrGroups();
            return false;
        }

        // Find groups that contain any of our zone clients
        var groupsWithOurClients = serverStatus
            .Groups.Where(g => g.Clients.Any(c => clientIndexs.Contains(c.Id)))
            .ToList();

        this.LogZoneCheckDetails(expectedStreamId, groupsWithOurClients.Count, string.Join(",", clientIndexs));

        // All our clients should be in exactly one group with the correct stream
        if (groupsWithOurClients.Count != 1)
        {
            this.LogZoneMisconfiguredMultipleGroups(groupsWithOurClients.Count);
            return false;
        }

        var targetGroup = groupsWithOurClients.First();

        // Check if all our clients are in this group and no foreign clients
        var groupClientIndexs = targetGroup.Clients.Select(c => c.Id).ToList();
        var allOurClientsPresent = clientIndexs.All(id => groupClientIndexs.Contains(id));
        var noForeignClients = groupClientIndexs.All(id => clientIndexs.Contains(id));
        var correctStream = targetGroup.StreamId == expectedStreamId;

        // Check group name - extract zone ID from stream and get expected zone name
        var zoneIndex = int.Parse(expectedStreamId.Replace("Zone", ""));
        var expectedGroupName = GetExpectedZoneName(zoneIndex);
        var correctGroupName = targetGroup.Name == expectedGroupName;

        // Check client names - all clients should have their configured names (not null)
        var correctClientNames = targetGroup.Clients.All(client => !string.IsNullOrEmpty(client.Name));

        this.LogZoneCheckDetailsVerbose(
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
            this.LogZoneMisconfiguredDetails(
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
    private static string GetExpectedZoneName(int zoneIndex)
    {
        // This should match the zone names from configuration
        // Zone 1 = "Ground Floor", Zone 2 = "1st Floor"
        return zoneIndex switch
        {
            1 => "Ground Floor",
            2 => "1st Floor",
            _ => $"Zone {zoneIndex}",
        };
    }

    // Stub implementations for interface compatibility - these are not used in periodic mode
    public async Task<Result> EnsureClientInZoneGroupAsync(
        int clientIndex,
        int zoneIndex,
        CancellationToken cancellationToken = default
    )
    {
        return await this.SynchronizeZoneGroupingAsync(zoneIndex, cancellationToken);
    }

    public async Task<Result> ValidateGroupingConsistencyAsync(CancellationToken cancellationToken = default)
    {
        return await this.EnsureZoneGroupingAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronizes client names for a specific zone to match configuration.
    /// </summary>
    private async Task SynchronizeClientNamesAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            this.LogStartingClientNameSync(zoneIndex);

            // Get zone clients with their configured names
            var zoneClients = await this._clientManager.GetClientsByZoneAsync(zoneIndex, cancellationToken);
            if (!zoneClients.IsSuccess || zoneClients.Value == null)
            {
                this.LogFailedGetZoneClients(zoneClients.ErrorMessage);
                return;
            }

            this.LogFoundZoneClients(
                zoneClients.Value.Count,
                zoneIndex,
                string.Join(", ", zoneClients.Value.Select(c => $"{c.SnapcastId}='{c.Name}'"))
            );

            // Get current server status to check actual client names
            var serverStatus = await this._snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess || serverStatus.Value?.Groups == null)
            {
                this.LogFailedGetServerStatusForNameSync(serverStatus.ErrorMessage);
                return;
            }

            // Create a map of snapcast client Index to current name
            var currentClientNames = new Dictionary<string, string?>();
            foreach (var group in serverStatus.Value.Groups)
            {
                foreach (var client in group.Clients)
                {
                    currentClientNames[client.Id] = client.Name;
                }
            }

            this.LogCurrentSnapcastClientNames(
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

                this.LogCheckingClientName(client.SnapcastId, expectedName, currentName);

                if (currentName != expectedName)
                {
                    // KEEP THIS AT INFO - actual work being done!
                    this.LogSettingClientName(client.SnapcastId, currentName, expectedName);

                    var setNameResult = await this._snapcastService.SetClientNameAsync(
                        client.SnapcastId,
                        expectedName,
                        cancellationToken
                    );

                    if (setNameResult.IsSuccess)
                    {
                        // KEEP THIS AT INFO - successful work completed!
                        this.LogClientNameSet(client.SnapcastId, expectedName);
                    }
                    else
                    {
                        this.LogFailedSetClientName(client.SnapcastId, setNameResult.ErrorMessage);
                    }
                }
                else
                {
                    this.LogClientNameAlreadyCorrect(client.SnapcastId, expectedName);
                }
            }

            this.LogClientNameSyncCompleted(zoneIndex);
        }
        catch (Exception ex)
        {
            this.LogErrorSynchronizingClientNames(ex, zoneIndex);
        }
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 114500, Level = LogLevel.Debug, Message = "🔍 Starting zone grouping check for all zones")]
    private partial void LogStartingZoneGroupingCheck();

    [LoggerMessage(EventId = 114501, Level = LogLevel.Debug, Message = "ℹ️ No zones configured, skipping zone grouping")]
    private partial void LogNoZonesConfigured();

    [LoggerMessage(EventId = 114502, Level = LogLevel.Debug, Message = "🔍 Checking {ZoneCount} zones: {ZoneIds}")]
    private partial void LogCheckingZones(int ZoneCount, string ZoneIds);

    [LoggerMessage(EventId = 114503, Level = LogLevel.Debug, Message = "🔧 Checking zone {ZoneId}...")]
    private partial void LogCheckingZone(int ZoneId);

    [LoggerMessage(EventId = 114504, Level = LogLevel.Warning, Message = "⚠️ Failed to synchronize zone {ZoneId}: {Error}")]
    private partial void LogFailedSynchronizeZone(int ZoneId, string? Error);

    [LoggerMessage(EventId = 114505, Level = LogLevel.Debug, Message = "✅ Zone {ZoneId} check completed")]
    private partial void LogZoneCheckCompleted(int ZoneId);

    [LoggerMessage(EventId = 114506, Level = LogLevel.Debug, Message = "✅ All zone grouping checks completed")]
    private partial void LogAllZoneGroupingChecksCompleted();

    [LoggerMessage(EventId = 114507, Level = LogLevel.Error, Message = "❌ Error during periodic zone grouping check")]
    private partial void LogErrorDuringPeriodicCheck(Exception ex);

    [LoggerMessage(EventId = 114508, Level = LogLevel.Debug, Message = "🔄 Synchronizing zone {ZoneId}")]
    private partial void LogSynchronizingZone(int ZoneId);

    [LoggerMessage(EventId = 114509, Level = LogLevel.Debug, Message = "ℹ️ No clients assigned to zone {ZoneId}, skipping")]
    private partial void LogNoClientsAssigned(int ZoneId);

    [LoggerMessage(EventId = 114510, Level = LogLevel.Debug, Message = "✅ Zone {ZoneId} is already properly configured (clients: {ClientIndexs}, stream: {StreamId})"
)]
    private partial void LogZoneAlreadyConfigured(int ZoneId, string ClientIndexs, string StreamId);

    [LoggerMessage(EventId = 114511, Level = LogLevel.Information, Message = "🔧 Provisioning zone {ZoneId}: {ClientCount} clients ({ClientIndexs}) with stream {StreamId}"
)]
    private partial void LogProvisioningZone(int ZoneId, int ClientCount, string ClientIndexs, string StreamId);

    [LoggerMessage(EventId = 114512, Level = LogLevel.Warning, Message = "⚠️ Failed to set stream for group {GroupId}: {Error}"
)]
    private partial void LogFailedSetGroupStream(string GroupId, string? Error);

    [LoggerMessage(EventId = 114513, Level = LogLevel.Debug, Message = "✅ Set group {GroupId} stream to {StreamId}")]
    private partial void LogSetGroupStream(string GroupId, string StreamId);

    [LoggerMessage(EventId = 114514, Level = LogLevel.Warning, Message = "⚠️ Failed to set name for group {GroupId}: {Error}"
)]
    private partial void LogFailedSetGroupName(string GroupId, string? Error);

    [LoggerMessage(EventId = 114515, Level = LogLevel.Information, Message = "✅ Set group {GroupId} name to '{GroupName}'")]
    private partial void LogSetGroupName(string GroupId, string GroupName);

    [LoggerMessage(EventId = 114516, Level = LogLevel.Information, Message = "✅ Zone {ZoneId} synchronized: {ClientCount} clients in group {GroupId} with stream {StreamId}"
)]
    private partial void LogZoneSynchronized(int ZoneId, int ClientCount, string GroupId, string StreamId);

    [LoggerMessage(EventId = 114517, Level = LogLevel.Error, Message = "💥 Error synchronizing zone {ZoneId}")]
    private partial void LogErrorSynchronizingZone(Exception ex, int ZoneId);

    [LoggerMessage(EventId = 114518, Level = LogLevel.Debug, Message = "❌ No server status or groups available")]
    private partial void LogNoServerStatusOrGroups();

    [LoggerMessage(EventId = 114519, Level = LogLevel.Debug, Message = "🔍 Zone check: Expected stream {ExpectedStream}, Found {GroupCount} groups with our clients {ClientIndexs}"
)]
    private partial void LogZoneCheckDetails(string ExpectedStream, int GroupCount, string ClientIndexs);

    [LoggerMessage(EventId = 114520, Level = LogLevel.Information, Message = "❌ Zone misconfigured: {GroupCount} groups contain our clients (should be 1)"
)]
    private partial void LogZoneMisconfiguredMultipleGroups(int GroupCount);

    [LoggerMessage(EventId = 114521, Level = LogLevel.Debug, Message = "🔍 Zone check details: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}, CorrectGroupName={CorrectGroupName} (expected '{ExpectedName}', actual '{ActualName}'), CorrectClientNames={CorrectClientNames}"
)]
    private partial void LogZoneCheckDetailsVerbose(
        bool AllPresent,
        bool NoForeign,
        bool CorrectStream,
        bool CorrectGroupName,
        string ExpectedName,
        string ActualName,
        bool CorrectClientNames
    );

    [LoggerMessage(EventId = 114522, Level = LogLevel.Information, Message = "❌ Zone misconfigured: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}, CorrectGroupName={CorrectGroupName}, CorrectClientNames={CorrectClientNames}"
)]
    private partial void LogZoneMisconfiguredDetails(
        bool AllPresent,
        bool NoForeign,
        bool CorrectStream,
        bool CorrectGroupName,
        bool CorrectClientNames
    );

    [LoggerMessage(EventId = 114523, Level = LogLevel.Debug, Message = "🏷️ Starting client name synchronization for zone {ZoneId}"
)]
    private partial void LogStartingClientNameSync(int ZoneId);

    [LoggerMessage(EventId = 114524, Level = LogLevel.Warning, Message = "⚠️ Failed to get zone clients for name synchronization: {Error}"
)]
    private partial void LogFailedGetZoneClients(string? Error);

    [LoggerMessage(EventId = 114525, Level = LogLevel.Debug, Message = "🔍 Found {ClientCount} clients for zone {ZoneId}: {ClientNames}"
)]
    private partial void LogFoundZoneClients(int ClientCount, int ZoneId, string ClientNames);

    [LoggerMessage(EventId = 114526, Level = LogLevel.Warning, Message = "⚠️ Failed to get server status for client name synchronization: {Error}"
)]
    private partial void LogFailedGetServerStatusForNameSync(string? Error);

    [LoggerMessage(EventId = 114527, Level = LogLevel.Debug, Message = "🔍 Current Snapcast client names: {CurrentNames}")]
    private partial void LogCurrentSnapcastClientNames(string CurrentNames);

    [LoggerMessage(EventId = 114528, Level = LogLevel.Debug, Message = "🔍 Checking client {ClientIndex}: expected='{ExpectedName}', current='{CurrentName}'"
)]
    private partial void LogCheckingClientName(string ClientIndex, string ExpectedName, string? CurrentName);

    [LoggerMessage(EventId = 114529, Level = LogLevel.Information, Message = "🏷️ Setting client {ClientIndex} name from '{CurrentName}' to '{ExpectedName}'"
)]
    private partial void LogSettingClientName(string ClientIndex, string? CurrentName, string ExpectedName);

    [LoggerMessage(EventId = 114530, Level = LogLevel.Information, Message = "✅ Set client {ClientIndex} name to '{ClientName}'"
)]
    private partial void LogClientNameSet(string ClientIndex, string ClientName);

    [LoggerMessage(EventId = 114531, Level = LogLevel.Warning, Message = "⚠️ Failed to set name for client {ClientIndex}: {Error}"
)]
    private partial void LogFailedSetClientName(string ClientIndex, string? Error);

    [LoggerMessage(EventId = 114532, Level = LogLevel.Debug, Message = "✅ Client {ClientIndex} name is already correct: '{ClientName}'"
)]
    private partial void LogClientNameAlreadyCorrect(string ClientIndex, string ClientName);

    [LoggerMessage(EventId = 114533, Level = LogLevel.Debug, Message = "✅ Client name synchronization completed for zone {ZoneId}"
)]
    private partial void LogClientNameSyncCompleted(int ZoneId);

    [LoggerMessage(EventId = 114534, Level = LogLevel.Error, Message = "💥 Error synchronizing client names for zone {ZoneId}"
)]
    private partial void LogErrorSynchronizingClientNames(Exception ex, int ZoneId);

    [LoggerMessage(EventId = 114535, Level = LogLevel.Error, Message = "Error during background zone regrouping for client {ClientIndex}")]
    private partial void LogBackgroundRegroupingError(Exception ex, int clientIndex);

    /// <summary>
    /// Handles client zone change notifications by triggering immediate regrouping.
    /// </summary>
    public Task Handle(ClientZoneChangedNotification notification, CancellationToken cancellationToken = default)
    {
        // Fire-and-forget: Execute regrouping in background without blocking HTTP response
        _ = Task.Run(async () =>
        {
            try
            {
                await this.TriggerImmediateRegroupingAsync();
            }
            catch (Exception ex)
            {
                LogBackgroundRegroupingError(ex, notification.ClientIndex);
            }
        });

        return Task.CompletedTask;
    }
}
