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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;

/// <summary>
/// Background service that restores persisted state on application startup.
/// Handles configuration change detection and state migration.
/// </summary>
public partial class StateRestorationService : BackgroundService
{
    private readonly IPersistentStateStore _persistentStore;
    private readonly IZoneStateStore _zoneStateStore;
    private readonly IClientStateStore _clientStateStore;
    private readonly SnapDogConfiguration _config;
    private readonly ILogger<StateRestorationService> _logger;

    public StateRestorationService(
        IPersistentStateStore persistentStore,
        IZoneStateStore zoneStateStore,
        IClientStateStore clientStateStore,
        SnapDogConfiguration config,
        ILogger<StateRestorationService> logger)
    {
        _persistentStore = persistentStore;
        _zoneStateStore = zoneStateStore;
        _clientStateStore = clientStateStore;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            LogStateRestorationStarting();

            // Check if persistent store is available
            if (!await _persistentStore.IsHealthyAsync())
            {
                LogPersistentStoreUnavailable();
                return;
            }

            // Generate current configuration fingerprint
            var currentFingerprint = GenerateConfigurationFingerprint();

            // Check if configuration has changed
            var storedFingerprint = await _persistentStore.GetConfigurationFingerprintAsync();

            if (storedFingerprint != null && !ConfigurationChanged(currentFingerprint, storedFingerprint))
            {
                // Configuration unchanged, restore states
                await RestoreStatesAsync();
            }
            else
            {
                // Configuration changed or first run, clear old state and save new fingerprint
                if (storedFingerprint != null)
                {
                    LogConfigurationChanged(storedFingerprint.Hash, currentFingerprint.Hash);
                    await _persistentStore.ClearAllStateAsync();
                }
                else
                {
                    LogFirstRun();
                }

                await _persistentStore.SaveConfigurationFingerprintAsync(currentFingerprint);
            }

            LogStateRestorationCompleted();
        }
        catch (Exception ex)
        {
            LogStateRestorationFailed(ex);
        }
    }

    /// <summary>
    /// Restores zone and client states from persistent storage.
    /// </summary>
    private async Task RestoreStatesAsync()
    {
        // Restore zone states
        var zoneStates = await _persistentStore.LoadAllZoneStatesAsync();
        foreach (var (zoneIndex, zoneState) in zoneStates)
        {
            try
            {
                _zoneStateStore.SetZoneState(zoneIndex, zoneState);
                LogZoneStateRestored(zoneIndex, zoneState.Name);
            }
            catch (Exception ex)
            {
                LogZoneStateRestoreFailed(ex, zoneIndex);
            }
        }

        // Restore client states
        var clientStates = await _persistentStore.LoadAllClientStatesAsync();
        foreach (var (clientIndex, clientState) in clientStates)
        {
            try
            {
                _clientStateStore.SetClientState(clientIndex, clientState);
                LogClientStateRestored(clientIndex, clientState.Name);
            }
            catch (Exception ex)
            {
                LogClientStateRestoreFailed(ex, clientIndex);
            }
        }

        LogStatesRestored(zoneStates.Count, clientStates.Count);
    }

    /// <summary>
    /// Generates a fingerprint of the current configuration to detect changes.
    /// </summary>
    private ConfigurationFingerprint GenerateConfigurationFingerprint()
    {
        var configData = new
        {
            ZoneCount = _config.Zones.Count,
            ClientCount = _config.Clients.Count,
            ZoneNames = _config.Zones.Select(z => z.Name).ToArray(),
            ClientNames = _config.Clients.Select(c => c.Name).ToArray(),
            ZoneConfigs = _config.Zones.Select(z => new { z.Name, z.Sink }).ToArray(),
            ClientConfigs = _config.Clients.Select(c => new { c.Name, c.Mac, c.DefaultZone }).ToArray()
        };

        var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = false });
        var hash = ComputeSha256Hash(json);

        return new ConfigurationFingerprint
        {
            Hash = hash,
            CreatedAt = DateTime.UtcNow,
            ZoneCount = _config.Zones.Count,
            ClientCount = _config.Clients.Count,
            ZoneNames = _config.Zones.Select(z => z.Name).ToArray(),
            ClientNames = _config.Clients.Select(c => c.Name).ToArray()
        };
    }

    /// <summary>
    /// Checks if the configuration has changed by comparing fingerprints.
    /// </summary>
    private static bool ConfigurationChanged(ConfigurationFingerprint current, ConfigurationFingerprint stored)
    {
        return current.Hash != stored.Hash;
    }

    /// <summary>
    /// Computes SHA256 hash of the input string.
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    #region LoggerMessage Methods

    [LoggerMessage(
        EventId = 7100,
        Level = LogLevel.Information,
        Message = "🔄 Starting state restoration from persistent storage..."
    )]
    private partial void LogStateRestorationStarting();

    [LoggerMessage(
        EventId = 7101,
        Level = LogLevel.Warning,
        Message = "⚠️ Persistent store unavailable - skipping state restoration"
    )]
    private partial void LogPersistentStoreUnavailable();

    [LoggerMessage(
        EventId = 7102,
        Level = LogLevel.Information,
        Message = "🔄 Configuration changed: {OldHash} → {NewHash} - clearing old state"
    )]
    private partial void LogConfigurationChanged(string OldHash, string NewHash);

    [LoggerMessage(
        EventId = 7103,
        Level = LogLevel.Information,
        Message = "🆕 First run detected - initializing persistent state"
    )]
    private partial void LogFirstRun();

    [LoggerMessage(
        EventId = 7104,
        Level = LogLevel.Debug,
        Message = "✅ Zone {ZoneIndex} ({ZoneName}) state restored"
    )]
    private partial void LogZoneStateRestored(int ZoneIndex, string ZoneName);

    [LoggerMessage(
        EventId = 7105,
        Level = LogLevel.Error,
        Message = "❌ Failed to restore zone {ZoneIndex} state"
    )]
    private partial void LogZoneStateRestoreFailed(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 7106,
        Level = LogLevel.Debug,
        Message = "✅ Client {ClientIndex} ({ClientName}) state restored"
    )]
    private partial void LogClientStateRestored(int ClientIndex, string ClientName);

    [LoggerMessage(
        EventId = 7107,
        Level = LogLevel.Error,
        Message = "❌ Failed to restore client {ClientIndex} state"
    )]
    private partial void LogClientStateRestoreFailed(Exception ex, int ClientIndex);

    [LoggerMessage(
        EventId = 7108,
        Level = LogLevel.Information,
        Message = "✅ State restoration completed: {ZoneCount} zones, {ClientCount} clients restored"
    )]
    private partial void LogStatesRestored(int ZoneCount, int ClientCount);

    [LoggerMessage(
        EventId = 7109,
        Level = LogLevel.Information,
        Message = "✅ State restoration service completed successfully"
    )]
    private partial void LogStateRestorationCompleted();

    [LoggerMessage(
        EventId = 7110,
        Level = LogLevel.Error,
        Message = "❌ State restoration service failed"
    )]
    private partial void LogStateRestorationFailed(Exception ex);

    #endregion
}
