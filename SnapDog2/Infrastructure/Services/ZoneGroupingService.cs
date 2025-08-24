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
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Simple zone grouping service using periodic checks only.
/// Ensures clients assigned to the same zone are grouped together for synchronized audio playback.
/// </summary>
public partial class ZoneGroupingService : IZoneGroupingService
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
            LogStartingZoneGroupingCheck();

            // Get all available zones
            var zonesResult = await _zoneManager.GetAllZonesAsync(cancellationToken);
            if (!zonesResult.IsSuccess)
            {
                return Result.Failure($"Failed to get available zones: {zonesResult.ErrorMessage}");
            }

            var zones = zonesResult.Value?.Select(z => z.Id).ToList() ?? new List<int>();
            if (zones.Count == 0)
            {
                LogNoZonesConfigured();
                return Result.Success();
            }

            LogCheckingZones(zones.Count, string.Join(",", zones));

            // Synchronize each zone
            foreach (var zoneId in zones)
            {
                LogCheckingZone(zoneId);
                var result = await SynchronizeZoneGroupingAsync(zoneId, cancellationToken);
                if (!result.IsSuccess)
                {
                    LogFailedSynchronizeZone(zoneId, result.ErrorMessage);
                }
                else
                {
                    LogZoneCheckCompleted(zoneId);
                }
            }

            LogAllZoneGroupingChecksCompleted();
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogErrorDuringPeriodicCheck(ex);
            return Result.Failure($"Error during zone grouping check: {ex.Message}");
        }
    }

    public async Task<Result> SynchronizeZoneGroupingAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.SynchronizeZone");
        activity?.SetTag("zone.id", zoneId);

        try
        {
            LogSynchronizingZone(zoneId);

            // Get zone clients - who should be in this zone?
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneId, cancellationToken);
            if (!zoneClients.IsSuccess)
            {
                return Result.Failure($"Failed to get zone clients: {zoneClients.ErrorMessage}");
            }

            var clientIds =
                zoneClients.Value?.Select(c => c.SnapcastId).Where(id => !string.IsNullOrEmpty(id)).ToList()
                ?? new List<string>();
            if (clientIds.Count == 0)
            {
                LogNoClientsAssigned(zoneId);
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
                LogZoneAlreadyConfigured(zoneId, string.Join(",", clientIds), expectedStreamId);
                return Result.Success();
            }

            // Zone needs configuration - provision it (KEEP THIS AT INFO - actual work!)
            LogProvisioningZone(zoneId, clientIds.Count, string.Join(",", clientIds), expectedStreamId);

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
                    LogFailedSetGroupStream(targetGroup.Id, setStreamResult.ErrorMessage);
                }
                else
                {
                    LogSetGroupStream(targetGroup.Id, expectedStreamId);
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
                    LogFailedSetGroupName(targetGroup.Id, setGroupNameResult.ErrorMessage);
                }
                else
                {
                    LogSetGroupName(targetGroup.Id, expectedGroupName);
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

            LogZoneSynchronized(zoneId, clientIds.Count, targetGroup.Id, expectedStreamId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogErrorSynchronizingZone(ex, zoneId);
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
            LogNoServerStatusOrGroups();
            return false;
        }

        // Find groups that contain any of our zone clients
        var groupsWithOurClients = serverStatus
            .Groups.Where(g => g.Clients?.Any(c => clientIds.Contains(c.Id)) == true)
            .ToList();

        LogZoneCheckDetails(expectedStreamId, groupsWithOurClients.Count, string.Join(",", clientIds));

        // All our clients should be in exactly one group with the correct stream
        if (groupsWithOurClients.Count != 1)
        {
            LogZoneMisconfiguredMultipleGroups(groupsWithOurClients.Count);
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

        LogZoneCheckDetailsVerbose(
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
            LogZoneMisconfiguredDetails(
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
    private static string GetExpectedZoneName(int zoneId)
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
            LogStartingClientNameSync(zoneId);

            // Get zone clients with their configured names
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneId, cancellationToken);
            if (!zoneClients.IsSuccess || zoneClients.Value == null)
            {
                LogFailedGetZoneClients(zoneClients.ErrorMessage);
                return;
            }

            LogFoundZoneClients(
                zoneClients.Value.Count,
                zoneId,
                string.Join(", ", zoneClients.Value.Select(c => $"{c.SnapcastId}='{c.Name}'"))
            );

            // Get current server status to check actual client names
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess || serverStatus.Value?.Groups == null)
            {
                LogFailedGetServerStatusForNameSync(serverStatus.ErrorMessage);
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

            LogCurrentSnapcastClientNames(
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

                LogCheckingClientName(client.SnapcastId, expectedName, currentName);

                if (currentName != expectedName)
                {
                    // KEEP THIS AT INFO - actual work being done!
                    LogSettingClientName(client.SnapcastId, currentName, expectedName);

                    var setNameResult = await _snapcastService.SetClientNameAsync(
                        client.SnapcastId,
                        expectedName,
                        cancellationToken
                    );

                    if (setNameResult.IsSuccess)
                    {
                        // KEEP THIS AT INFO - successful work completed!
                        LogClientNameSet(client.SnapcastId, expectedName);
                    }
                    else
                    {
                        LogFailedSetClientName(client.SnapcastId, setNameResult.ErrorMessage);
                    }
                }
                else
                {
                    LogClientNameAlreadyCorrect(client.SnapcastId, expectedName);
                }
            }

            LogClientNameSyncCompleted(zoneId);
        }
        catch (Exception ex)
        {
            LogErrorSynchronizingClientNames(ex, zoneId);
        }
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 7000, Level = LogLevel.Debug, Message = "🔍 Starting zone grouping check for all zones")]
    private partial void LogStartingZoneGroupingCheck();

    [LoggerMessage(EventId = 7001, Level = LogLevel.Debug, Message = "ℹ️ No zones configured, skipping zone grouping")]
    private partial void LogNoZonesConfigured();

    [LoggerMessage(EventId = 7002, Level = LogLevel.Debug, Message = "🔍 Checking {ZoneCount} zones: {ZoneIds}")]
    private partial void LogCheckingZones(int ZoneCount, string ZoneIds);

    [LoggerMessage(EventId = 7003, Level = LogLevel.Debug, Message = "🔧 Checking zone {ZoneId}...")]
    private partial void LogCheckingZone(int ZoneId);

    [LoggerMessage(EventId = 7004, Level = LogLevel.Warning, Message = "⚠️ Failed to synchronize zone {ZoneId}: {Error}")]
    private partial void LogFailedSynchronizeZone(int ZoneId, string? Error);

    [LoggerMessage(EventId = 7005, Level = LogLevel.Debug, Message = "✅ Zone {ZoneId} check completed")]
    private partial void LogZoneCheckCompleted(int ZoneId);

    [LoggerMessage(EventId = 7006, Level = LogLevel.Debug, Message = "✅ All zone grouping checks completed")]
    private partial void LogAllZoneGroupingChecksCompleted();

    [LoggerMessage(EventId = 7007, Level = LogLevel.Error, Message = "❌ Error during periodic zone grouping check")]
    private partial void LogErrorDuringPeriodicCheck(Exception ex);

    [LoggerMessage(EventId = 7008, Level = LogLevel.Debug, Message = "🔄 Synchronizing zone {ZoneId}")]
    private partial void LogSynchronizingZone(int ZoneId);

    [LoggerMessage(EventId = 7009, Level = LogLevel.Debug, Message = "ℹ️ No clients assigned to zone {ZoneId}, skipping")]
    private partial void LogNoClientsAssigned(int ZoneId);

    [LoggerMessage(
        EventId = 7010,
        Level = LogLevel.Debug,
        Message = "✅ Zone {ZoneId} is already properly configured (clients: {ClientIds}, stream: {StreamId})"
    )]
    private partial void LogZoneAlreadyConfigured(int ZoneId, string ClientIds, string StreamId);

    [LoggerMessage(
        EventId = 7011,
        Level = LogLevel.Information,
        Message = "🔧 Provisioning zone {ZoneId}: {ClientCount} clients ({ClientIds}) with stream {StreamId}"
    )]
    private partial void LogProvisioningZone(int ZoneId, int ClientCount, string ClientIds, string StreamId);

    [LoggerMessage(
        EventId = 7012,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to set stream for group {GroupId}: {Error}"
    )]
    private partial void LogFailedSetGroupStream(string GroupId, string? Error);

    [LoggerMessage(EventId = 7013, Level = LogLevel.Debug, Message = "✅ Set group {GroupId} stream to {StreamId}")]
    private partial void LogSetGroupStream(string GroupId, string StreamId);

    [LoggerMessage(
        EventId = 7014,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to set name for group {GroupId}: {Error}"
    )]
    private partial void LogFailedSetGroupName(string GroupId, string? Error);

    [LoggerMessage(EventId = 7015, Level = LogLevel.Information, Message = "✅ Set group {GroupId} name to '{GroupName}'")]
    private partial void LogSetGroupName(string GroupId, string GroupName);

    [LoggerMessage(
        EventId = 7016,
        Level = LogLevel.Information,
        Message = "✅ Zone {ZoneId} synchronized: {ClientCount} clients in group {GroupId} with stream {StreamId}"
    )]
    private partial void LogZoneSynchronized(int ZoneId, int ClientCount, string GroupId, string StreamId);

    [LoggerMessage(EventId = 7017, Level = LogLevel.Error, Message = "💥 Error synchronizing zone {ZoneId}")]
    private partial void LogErrorSynchronizingZone(Exception ex, int ZoneId);

    [LoggerMessage(EventId = 7018, Level = LogLevel.Debug, Message = "❌ No server status or groups available")]
    private partial void LogNoServerStatusOrGroups();

    [LoggerMessage(
        EventId = 7019,
        Level = LogLevel.Debug,
        Message = "🔍 Zone check: Expected stream {ExpectedStream}, Found {GroupCount} groups with our clients {ClientIds}"
    )]
    private partial void LogZoneCheckDetails(string ExpectedStream, int GroupCount, string ClientIds);

    [LoggerMessage(
        EventId = 7020,
        Level = LogLevel.Information,
        Message = "❌ Zone misconfigured: {GroupCount} groups contain our clients (should be 1)"
    )]
    private partial void LogZoneMisconfiguredMultipleGroups(int GroupCount);

    [LoggerMessage(
        EventId = 7021,
        Level = LogLevel.Debug,
        Message = "🔍 Zone check details: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}, CorrectGroupName={CorrectGroupName} (expected '{ExpectedName}', actual '{ActualName}'), CorrectClientNames={CorrectClientNames}"
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

    [LoggerMessage(
        EventId = 7022,
        Level = LogLevel.Information,
        Message = "❌ Zone misconfigured: AllClientsPresent={AllPresent}, NoForeignClients={NoForeign}, CorrectStream={CorrectStream}, CorrectGroupName={CorrectGroupName}, CorrectClientNames={CorrectClientNames}"
    )]
    private partial void LogZoneMisconfiguredDetails(
        bool AllPresent,
        bool NoForeign,
        bool CorrectStream,
        bool CorrectGroupName,
        bool CorrectClientNames
    );

    [LoggerMessage(
        EventId = 7023,
        Level = LogLevel.Debug,
        Message = "🏷️ Starting client name synchronization for zone {ZoneId}"
    )]
    private partial void LogStartingClientNameSync(int ZoneId);

    [LoggerMessage(
        EventId = 7024,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to get zone clients for name synchronization: {Error}"
    )]
    private partial void LogFailedGetZoneClients(string? Error);

    [LoggerMessage(
        EventId = 7025,
        Level = LogLevel.Debug,
        Message = "🔍 Found {ClientCount} clients for zone {ZoneId}: {ClientNames}"
    )]
    private partial void LogFoundZoneClients(int ClientCount, int ZoneId, string ClientNames);

    [LoggerMessage(
        EventId = 7026,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to get server status for client name synchronization: {Error}"
    )]
    private partial void LogFailedGetServerStatusForNameSync(string? Error);

    [LoggerMessage(EventId = 7027, Level = LogLevel.Debug, Message = "🔍 Current Snapcast client names: {CurrentNames}")]
    private partial void LogCurrentSnapcastClientNames(string CurrentNames);

    [LoggerMessage(
        EventId = 7028,
        Level = LogLevel.Debug,
        Message = "🔍 Checking client {ClientId}: expected='{ExpectedName}', current='{CurrentName}'"
    )]
    private partial void LogCheckingClientName(string ClientId, string ExpectedName, string? CurrentName);

    [LoggerMessage(
        EventId = 7029,
        Level = LogLevel.Information,
        Message = "🏷️ Setting client {ClientId} name from '{CurrentName}' to '{ExpectedName}'"
    )]
    private partial void LogSettingClientName(string ClientId, string? CurrentName, string ExpectedName);

    [LoggerMessage(
        EventId = 7030,
        Level = LogLevel.Information,
        Message = "✅ Set client {ClientId} name to '{ClientName}'"
    )]
    private partial void LogClientNameSet(string ClientId, string ClientName);

    [LoggerMessage(
        EventId = 7031,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to set name for client {ClientId}: {Error}"
    )]
    private partial void LogFailedSetClientName(string ClientId, string? Error);

    [LoggerMessage(
        EventId = 7032,
        Level = LogLevel.Debug,
        Message = "✅ Client {ClientId} name is already correct: '{ClientName}'"
    )]
    private partial void LogClientNameAlreadyCorrect(string ClientId, string ClientName);

    [LoggerMessage(
        EventId = 7033,
        Level = LogLevel.Debug,
        Message = "✅ Client name synchronization completed for zone {ZoneId}"
    )]
    private partial void LogClientNameSyncCompleted(int ZoneId);

    [LoggerMessage(
        EventId = 7034,
        Level = LogLevel.Error,
        Message = "💥 Error synchronizing client names for zone {ZoneId}"
    )]
    private partial void LogErrorSynchronizingClientNames(Exception ex, int ZoneId);
}
