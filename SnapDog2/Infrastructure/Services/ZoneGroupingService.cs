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
using System.Linq;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

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

            // Get all available zone services to extract their indices
            var zonesResult = await _zoneManager.GetAllZonesAsync(cancellationToken);
            if (!zonesResult.IsSuccess)
            {
                return Result.Failure($"Failed to get available zones: {zonesResult.ErrorMessage}");
            }

            // We need to get the zone indices. Since we can't access the internal _zones dictionary,
            // let's try to get zone states and infer indices from successful zone retrievals
            var zones = new List<int>();

            // Try zones 1-10 (reasonable upper limit) and see which ones exist
            for (int i = 1; i <= 10; i++)
            {
                if (await _zoneManager.ZoneExistsAsync(i))
                {
                    zones.Add(i);
                }
            }
            if (zones.Count == 0)
            {
                LogNoZonesConfigured();
                return Result.Success();
            }

            LogCheckingZones(zones.Count, string.Join(",", zones));

            // Synchronize each zone
            foreach (var zoneIndex in zones)
            {
                LogCheckingZone(zoneIndex);
                var result = await SynchronizeZoneGroupingAsync(zoneIndex, cancellationToken);
                if (!result.IsSuccess)
                {
                    LogFailedSynchronizeZone(zoneIndex, result.ErrorMessage);
                }
                else
                {
                    LogZoneCheckCompleted(zoneIndex);
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

    public async Task<Result> SynchronizeZoneGroupingAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ZoneGrouping.SynchronizeZone");
        activity?.SetTag("zone.id", zoneIndex);

        try
        {
            LogSynchronizingZone(zoneIndex);

            // Get zone clients - who should be in this zone?
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneIndex, cancellationToken);
            if (!zoneClients.IsSuccess)
            {
                return Result.Failure($"Failed to get zone clients: {zoneClients.ErrorMessage}");
            }

            var clientIndexs =
                zoneClients.Value?.Select(c => c.SnapcastId).Where(id => !string.IsNullOrEmpty(id)).ToList()
                ?? new List<string>();
            if (clientIndexs.Count == 0)
            {
                LogNoClientsAssigned(zoneIndex);
                return Result.Success();
            }

            // Get current server status
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess)
            {
                return Result.Failure($"Failed to get server status: {serverStatus.ErrorMessage}");
            }

            var expectedStreamId = $"Zone{zoneIndex}";

            // Check if zone is already properly configured
            if (IsZoneProperlyConfigured(serverStatus.Value, clientIndexs, expectedStreamId))
            {
                LogZoneAlreadyConfigured(zoneIndex, string.Join(",", clientIndexs), expectedStreamId);
                return Result.Success();
            }

            // Zone needs configuration - provision it (KEEP THIS AT INFO - actual work!)
            LogProvisioningZone(zoneIndex, clientIndexs.Count, string.Join(",", clientIndexs), expectedStreamId);

            // Find a group that has any of our zone's clients, or use any available group
            var targetGroup =
                serverStatus.Value?.Groups?.FirstOrDefault(g => g.Clients?.Any(c => clientIndexs.Contains(c.Id)) == true)
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
            var expectedGroupName = GetExpectedZoneName(zoneIndex);
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
                clientIndexs,
                cancellationToken
            );
            if (!setClientsResult.IsSuccess)
            {
                return Result.Failure($"Failed to set clients for group: {setClientsResult.ErrorMessage}");
            }

            // Synchronize client names to match configuration
            await SynchronizeClientNamesAsync(zoneIndex, cancellationToken);

            LogZoneSynchronized(zoneIndex, clientIndexs.Count, targetGroup.Id, expectedStreamId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogErrorSynchronizingZone(ex, zoneIndex);
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
            LogNoServerStatusOrGroups();
            return false;
        }

        // Find groups that contain any of our zone clients
        var groupsWithOurClients = serverStatus
            .Groups.Where(g => g.Clients?.Any(c => clientIndexs.Contains(c.Id)) == true)
            .ToList();

        LogZoneCheckDetails(expectedStreamId, groupsWithOurClients.Count, string.Join(",", clientIndexs));

        // All our clients should be in exactly one group with the correct stream
        if (groupsWithOurClients.Count != 1)
        {
            LogZoneMisconfiguredMultipleGroups(groupsWithOurClients.Count);
            return false;
        }

        var targetGroup = groupsWithOurClients.First();

        // Check if all our clients are in this group and no foreign clients
        var groupClientIndexs = targetGroup.Clients?.Select(c => c.Id).ToList() ?? new List<string>();
        var allOurClientsPresent = clientIndexs.All(id => groupClientIndexs.Contains(id));
        var noForeignClients = groupClientIndexs.All(id => clientIndexs.Contains(id));
        var correctStream = targetGroup.StreamId == expectedStreamId;

        // Check group name - extract zone ID from stream and get expected zone name
        var zoneIndex = int.Parse(expectedStreamId.Replace("Zone", ""));
        var expectedGroupName = GetExpectedZoneName(zoneIndex);
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
        return await SynchronizeZoneGroupingAsync(zoneIndex, cancellationToken);
    }

    public async Task<Result> ValidateGroupingConsistencyAsync(CancellationToken cancellationToken = default)
    {
        return await EnsureZoneGroupingAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronizes client names for a specific zone to match configuration.
    /// </summary>
    private async Task SynchronizeClientNamesAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            LogStartingClientNameSync(zoneIndex);

            // Get zone clients with their configured names
            var zoneClients = await _clientManager.GetClientsByZoneAsync(zoneIndex, cancellationToken);
            if (!zoneClients.IsSuccess || zoneClients.Value == null)
            {
                LogFailedGetZoneClients(zoneClients.ErrorMessage);
                return;
            }

            LogFoundZoneClients(
                zoneClients.Value.Count,
                zoneIndex,
                string.Join(", ", zoneClients.Value.Select(c => $"{c.SnapcastId}='{c.Name}'"))
            );

            // Get current server status to check actual client names
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            if (!serverStatus.IsSuccess || serverStatus.Value?.Groups == null)
            {
                LogFailedGetServerStatusForNameSync(serverStatus.ErrorMessage);
                return;
            }

            // Create a map of snapcast client Index to current name
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

            LogClientNameSyncCompleted(zoneIndex);
        }
        catch (Exception ex)
        {
            LogErrorSynchronizingClientNames(ex, zoneIndex);
        }
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 7200, Level = LogLevel.Debug, Message = "🔍 Starting zone grouping check for all zones")]
    private partial void LogStartingZoneGroupingCheck();

    [LoggerMessage(EventId = 7201, Level = LogLevel.Debug, Message = "ℹ️ No zones configured, skipping zone grouping")]
    private partial void LogNoZonesConfigured();

    [LoggerMessage(EventId = 7202, Level = LogLevel.Debug, Message = "🔍 Checking {ZoneCount} zones: {ZoneIds}")]
    private partial void LogCheckingZones(int ZoneCount, string ZoneIds);

    [LoggerMessage(EventId = 7203, Level = LogLevel.Debug, Message = "🔧 Checking zone {ZoneId}...")]
    private partial void LogCheckingZone(int ZoneId);

    [LoggerMessage(EventId = 7204, Level = LogLevel.Warning, Message = "⚠️ Failed to synchronize zone {ZoneId}: {Error}")]
    private partial void LogFailedSynchronizeZone(int ZoneId, string? Error);

    [LoggerMessage(EventId = 7205, Level = LogLevel.Debug, Message = "✅ Zone {ZoneId} check completed")]
    private partial void LogZoneCheckCompleted(int ZoneId);

    [LoggerMessage(EventId = 7206, Level = LogLevel.Debug, Message = "✅ All zone grouping checks completed")]
    private partial void LogAllZoneGroupingChecksCompleted();

    [LoggerMessage(EventId = 7207, Level = LogLevel.Error, Message = "❌ Error during periodic zone grouping check")]
    private partial void LogErrorDuringPeriodicCheck(Exception ex);

    [LoggerMessage(EventId = 7208, Level = LogLevel.Debug, Message = "🔄 Synchronizing zone {ZoneId}")]
    private partial void LogSynchronizingZone(int ZoneId);

    [LoggerMessage(EventId = 7209, Level = LogLevel.Debug, Message = "ℹ️ No clients assigned to zone {ZoneId}, skipping")]
    private partial void LogNoClientsAssigned(int ZoneId);

    [LoggerMessage(
        EventId = 7210,
        Level = LogLevel.Debug,
        Message = "✅ Zone {ZoneId} is already properly configured (clients: {ClientIndexs}, stream: {StreamId})"
    )]
    private partial void LogZoneAlreadyConfigured(int ZoneId, string ClientIndexs, string StreamId);

    [LoggerMessage(
        EventId = 7211,
        Level = LogLevel.Information,
        Message = "🔧 Provisioning zone {ZoneId}: {ClientCount} clients ({ClientIndexs}) with stream {StreamId}"
    )]
    private partial void LogProvisioningZone(int ZoneId, int ClientCount, string ClientIndexs, string StreamId);

    [LoggerMessage(
        EventId = 7212,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to set stream for group {GroupId}: {Error}"
    )]
    private partial void LogFailedSetGroupStream(string GroupId, string? Error);

    [LoggerMessage(EventId = 7213, Level = LogLevel.Debug, Message = "✅ Set group {GroupId} stream to {StreamId}")]
    private partial void LogSetGroupStream(string GroupId, string StreamId);

    [LoggerMessage(
        EventId = 7214,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to set name for group {GroupId}: {Error}"
    )]
    private partial void LogFailedSetGroupName(string GroupId, string? Error);

    [LoggerMessage(EventId = 7215, Level = LogLevel.Information, Message = "✅ Set group {GroupId} name to '{GroupName}'")]
    private partial void LogSetGroupName(string GroupId, string GroupName);

    [LoggerMessage(
        EventId = 7216,
        Level = LogLevel.Information,
        Message = "✅ Zone {ZoneId} synchronized: {ClientCount} clients in group {GroupId} with stream {StreamId}"
    )]
    private partial void LogZoneSynchronized(int ZoneId, int ClientCount, string GroupId, string StreamId);

    [LoggerMessage(EventId = 7217, Level = LogLevel.Error, Message = "💥 Error synchronizing zone {ZoneId}")]
    private partial void LogErrorSynchronizingZone(Exception ex, int ZoneId);

    [LoggerMessage(EventId = 7218, Level = LogLevel.Debug, Message = "❌ No server status or groups available")]
    private partial void LogNoServerStatusOrGroups();

    [LoggerMessage(
        EventId = 7219,
        Level = LogLevel.Debug,
        Message = "🔍 Zone check: Expected stream {ExpectedStream}, Found {GroupCount} groups with our clients {ClientIndexs}"
    )]
    private partial void LogZoneCheckDetails(string ExpectedStream, int GroupCount, string ClientIndexs);

    [LoggerMessage(
        EventId = 7220,
        Level = LogLevel.Information,
        Message = "❌ Zone misconfigured: {GroupCount} groups contain our clients (should be 1)"
    )]
    private partial void LogZoneMisconfiguredMultipleGroups(int GroupCount);

    [LoggerMessage(
        EventId = 7221,
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
        EventId = 7222,
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
        EventId = 7223,
        Level = LogLevel.Debug,
        Message = "🏷️ Starting client name synchronization for zone {ZoneId}"
    )]
    private partial void LogStartingClientNameSync(int ZoneId);

    [LoggerMessage(
        EventId = 7224,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to get zone clients for name synchronization: {Error}"
    )]
    private partial void LogFailedGetZoneClients(string? Error);

    [LoggerMessage(
        EventId = 7225,
        Level = LogLevel.Debug,
        Message = "🔍 Found {ClientCount} clients for zone {ZoneId}: {ClientNames}"
    )]
    private partial void LogFoundZoneClients(int ClientCount, int ZoneId, string ClientNames);

    [LoggerMessage(
        EventId = 7226,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to get server status for client name synchronization: {Error}"
    )]
    private partial void LogFailedGetServerStatusForNameSync(string? Error);

    [LoggerMessage(EventId = 7227, Level = LogLevel.Debug, Message = "🔍 Current Snapcast client names: {CurrentNames}")]
    private partial void LogCurrentSnapcastClientNames(string CurrentNames);

    [LoggerMessage(
        EventId = 7228,
        Level = LogLevel.Debug,
        Message = "🔍 Checking client {ClientIndex}: expected='{ExpectedName}', current='{CurrentName}'"
    )]
    private partial void LogCheckingClientName(string ClientIndex, string ExpectedName, string? CurrentName);

    [LoggerMessage(
        EventId = 7229,
        Level = LogLevel.Information,
        Message = "🏷️ Setting client {ClientIndex} name from '{CurrentName}' to '{ExpectedName}'"
    )]
    private partial void LogSettingClientName(string ClientIndex, string? CurrentName, string ExpectedName);

    [LoggerMessage(
        EventId = 7230,
        Level = LogLevel.Information,
        Message = "✅ Set client {ClientIndex} name to '{ClientName}'"
    )]
    private partial void LogClientNameSet(string ClientIndex, string ClientName);

    [LoggerMessage(
        EventId = 7231,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to set name for client {ClientIndex}: {Error}"
    )]
    private partial void LogFailedSetClientName(string ClientIndex, string? Error);

    [LoggerMessage(
        EventId = 7232,
        Level = LogLevel.Debug,
        Message = "✅ Client {ClientIndex} name is already correct: '{ClientName}'"
    )]
    private partial void LogClientNameAlreadyCorrect(string ClientIndex, string ClientName);

    [LoggerMessage(
        EventId = 7233,
        Level = LogLevel.Debug,
        Message = "✅ Client name synchronization completed for zone {ZoneId}"
    )]
    private partial void LogClientNameSyncCompleted(int ZoneId);

    [LoggerMessage(
        EventId = 7234,
        Level = LogLevel.Error,
        Message = "💥 Error synchronizing client names for zone {ZoneId}"
    )]
    private partial void LogErrorSynchronizingClientNames(Exception ex, int ZoneId);
}
