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
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Background service that restores persisted state on application startup.
/// Handles configuration change detection and state migration.
/// </summary>
public partial class StateRestorationService(
    IPersistentStateStore persistentStore,
    IZoneStateStore zoneStateStore,
    IClientStateStore clientStateStore,
    IZoneManager zoneManager,
    SnapDogConfiguration config,
    ILogger<StateRestorationService> logger)
    : BackgroundService
{
    async protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            this.LogStateRestorationStarting();

            // Check if persistent store is available
            if (!await persistentStore.IsHealthyAsync())
            {
                this.LogPersistentStoreUnavailable();
                return;
            }

            // Generate current configuration fingerprint
            var currentFingerprint = this.GenerateConfigurationFingerprint();

            // Check if configuration has changed
            var storedFingerprint = await persistentStore.GetConfigurationFingerprintAsync();

            if (storedFingerprint != null && !ConfigurationChanged(currentFingerprint, storedFingerprint))
            {
                // Configuration unchanged, restore states
                await this.RestoreStatesAsync();
            }
            else
            {
                // Configuration changed or first run, clear old state and save new fingerprint
                if (storedFingerprint != null)
                {
                    this.LogConfigurationChanged(storedFingerprint.Hash, currentFingerprint.Hash);
                    await persistentStore.ClearAllStateAsync();
                }
                else
                {
                    this.LogFirstRun();
                }

                await persistentStore.SaveConfigurationFingerprintAsync(currentFingerprint);
            }

            this.LogStateRestorationCompleted();
        }
        catch (Exception ex)
        {
            this.LogStateRestorationFailed(ex);
        }
    }

    /// <summary>
    /// Restores zone and client states from persistent storage.
    /// Sets up initial states for new zones and resumes playback for playing zones.
    /// </summary>
    private async Task RestoreStatesAsync()
    {
        // Restore zone states
        var zoneStates = await persistentStore.LoadAllZoneStatesAsync();
        var zonesToResumePlayback = new List<(int zoneIndex, ZoneState zoneState)>();

        // Process each configured zone
        for (var i = 0; i < config.Zones.Count; i++)
        {
            var zoneIndex = i + 1; // 1-based indexing
            var zoneConfig = config.Zones[i];

            if (zoneStates.TryGetValue(zoneIndex, out var persistedState))
            {
                // Restore persisted state
                try
                {
                    zoneStateStore.SetZoneState(zoneIndex, persistedState);
                    this.LogZoneStateRestored(zoneIndex, persistedState.Name);

                    // Track zones that were playing for later resumption
                    if (persistedState.PlaybackState == PlaybackState.Playing)
                    {
                        zonesToResumePlayback.Add((zoneIndex, persistedState));
                        this.LogZonePlaybackWillResume(zoneIndex, persistedState.Name);
                    }
                }
                catch (Exception ex)
                {
                    this.LogZoneStateRestoreFailed(ex, zoneIndex);
                }
            }
            else
            {
                // Create initial state for new zone
                await this.SetupInitialZoneStateAsync(zoneIndex, zoneConfig);
            }
        }

        // Restore client states
        var clientStates = await persistentStore.LoadAllClientStatesAsync();
        for (var i = 0; i < config.Clients.Count; i++)
        {
            var clientIndex = i + 1; // 1-based indexing
            var clientConfig = config.Clients[i];

            if (clientStates.TryGetValue(clientIndex, out var persistedClientState))
            {
                try
                {
                    clientStateStore.SetClientState(clientIndex, persistedClientState);
                    this.LogClientStateRestored(clientIndex, persistedClientState.Name);
                }
                catch (Exception ex)
                {
                    this.LogClientStateRestoreFailed(ex, clientIndex);
                }
            }
            else
            {
                // Create initial client state if needed
                this.LogClientStateNotFound(clientIndex, clientConfig.Name);
            }
        }

        this.LogStatesRestored(zoneStates.Count, clientStates.Count);

        // Resume playback for zones that were playing (after a delay to ensure system is ready)
        if (zonesToResumePlayback.Any())
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3)); // Wait for system initialization
                await this.ResumePlaybackAsync(zonesToResumePlayback);
            });
        }
    }

    /// <summary>
    /// Sets up initial state for a new zone with playlist 1, track 1 (not playing).
    /// </summary>
    private async Task SetupInitialZoneStateAsync(int zoneIndex, ZoneConfig zoneConfig)
    {
        try
        {
            var initialState = new ZoneState
            {
                Name = zoneConfig.Name,
                Volume = 50, // Default volume
                Mute = false,
                PlaybackState = PlaybackState.Stopped,
                TrackRepeat = false,
                PlaylistRepeat = false,
                PlaylistShuffle = false,
                SnapcastGroupId = "", // Will be set by Snapcast integration
                SnapcastStreamId = "", // Will be set by Snapcast integration

                Playlist = null, // Will be loaded when playlist is actually loaded
                Track = null, // Will be loaded when track is actually loaded
                Clients = Array.Empty<int>(), // Will be populated by client management
                TimestampUtc = DateTime.UtcNow
            };

            zoneStateStore.SetZoneState(zoneIndex, initialState);
            this.LogInitialZoneStateCreated(zoneIndex, zoneConfig.Name);

            // Preload playlist 1, track 1 (but don't start playing)
            await this.PreloadInitialPlaylistAsync(zoneIndex, zoneConfig.Name);
        }
        catch (Exception ex)
        {
            this.LogInitialZoneStateCreationFailed(ex, zoneIndex, zoneConfig.Name);
        }
    }

    /// <summary>
    /// Preloads playlist 1, track 1 for a new zone without starting playback.
    /// </summary>
    private async Task PreloadInitialPlaylistAsync(int zoneIndex, string zoneName)
    {
        try
        {
            this.LogPreloadingInitialPlaylist(zoneIndex, zoneName);

            // Get the zone service
            var zoneResult = await zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsSuccess)
            {
                var zoneService = zoneResult.Value!;

                // Set playlist 1
                var playlistResult = await zoneService.SetPlaylistAsync(1);
                if (playlistResult.IsSuccess)
                {
                    // Set track 1
                    var trackResult = await zoneService.SetTrackAsync(1);
                    if (trackResult.IsSuccess)
                    {
                        this.LogInitialPlaylistPreloaded(zoneIndex, zoneName);
                    }
                    else
                    {
                        this.LogInitialPlaylistPreloadFailed(new Exception($"Failed to set track 1: {trackResult.ErrorMessage}"), zoneIndex, zoneName);
                    }
                }
                else
                {
                    this.LogInitialPlaylistPreloadFailed(new Exception($"Failed to set playlist 1: {playlistResult.ErrorMessage}"), zoneIndex, zoneName);
                }
            }
            else
            {
                this.LogInitialPlaylistPreloadFailed(new Exception($"Zone {zoneIndex} not found: {zoneResult.ErrorMessage}"), zoneIndex, zoneName);
            }
        }
        catch (Exception ex)
        {
            this.LogInitialPlaylistPreloadFailed(ex, zoneIndex, zoneName);
        }
    }

    /// <summary>
    /// Resumes playback for zones that were playing when the application was stopped.
    /// </summary>
    private async Task ResumePlaybackAsync(List<(int zoneIndex, ZoneState zoneState)> zonesToResume)
    {
        foreach (var (zoneIndex, zoneState) in zonesToResume)
        {
            try
            {
                this.LogResumingPlayback(zoneIndex, zoneState.Name);

                // Get the zone service and resume playback
                var zoneResult = await zoneManager.GetZoneAsync(zoneIndex);
                if (zoneResult.IsSuccess)
                {
                    var zoneService = zoneResult.Value!;

                    // Start playback - the zone should resume from its persisted state
                    var playResult = await zoneService.PlayAsync();

                    if (playResult.IsSuccess)
                    {
                        this.LogPlaybackResumed(zoneIndex, zoneState.Name);
                    }
                    else
                    {
                        this.LogPlaybackResumeFailed(new Exception(playResult.ErrorMessage), zoneIndex, zoneState.Name);
                    }
                }
                else
                {
                    this.LogPlaybackResumeFailed(new Exception($"Zone {zoneIndex} not found: {zoneResult.ErrorMessage}"), zoneIndex, zoneState.Name);
                }
            }
            catch (Exception ex)
            {
                this.LogPlaybackResumeFailed(ex, zoneIndex, zoneState.Name);
            }
        }
    }

    /// <summary>
    /// Generates a fingerprint of the current configuration to detect changes.
    /// </summary>
    private ConfigurationFingerprint GenerateConfigurationFingerprint()
    {
        var configData = new
        {
            ZoneCount = config.Zones.Count,
            ClientCount = config.Clients.Count,
            ZoneNames = config.Zones.Select(z => z.Name).ToArray(),
            ClientNames = config.Clients.Select(c => c.Name).ToArray(),
            ZoneConfigs = config.Zones.Select(z => new { z.Name, z.Sink }).ToArray(),
            ClientConfigs = config.Clients.Select(c => new { c.Name, c.Mac, c.DefaultZone }).ToArray()
        };

        var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = false });
        var hash = ComputeSha256Hash(json);

        return new ConfigurationFingerprint
        {
            Hash = hash,
            CreatedAt = DateTime.UtcNow,
            ZoneCount = config.Zones.Count,
            ClientCount = config.Clients.Count,
            ZoneNames = config.Zones.Select(z => z.Name).ToArray(),
            ClientNames = config.Clients.Select(c => c.Name).ToArray()
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

    [LoggerMessage(EventId = 114400, Level = LogLevel.Information, Message = "🔄 Starting state restoration from persistent storage..."
)]
    private partial void LogStateRestorationStarting();

    [LoggerMessage(EventId = 114401, Level = LogLevel.Warning, Message = "⚠️ Persistent store unavailable - skipping state restoration"
)]
    private partial void LogPersistentStoreUnavailable();

    [LoggerMessage(EventId = 114402, Level = LogLevel.Information, Message = "🔄 Configuration changed: {OldHash} → {NewHash} - clearing old state"
)]
    private partial void LogConfigurationChanged(string oldHash, string newHash);

    [LoggerMessage(EventId = 114403, Level = LogLevel.Information, Message = "🆕 First run detected - initializing persistent state"
)]
    private partial void LogFirstRun();

    [LoggerMessage(EventId = 114404, Level = LogLevel.Debug, Message = "✅ Zone {ZoneIndex} ({ZoneName}) state restored"
)]
    private partial void LogZoneStateRestored(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114405, Level = LogLevel.Error, Message = "❌ Failed to restore zone {ZoneIndex} state"
)]
    private partial void LogZoneStateRestoreFailed(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 114406, Level = LogLevel.Debug, Message = "✅ Client {ClientIndex} ({ClientName}) state restored"
)]
    private partial void LogClientStateRestored(int clientIndex, string clientName);

    [LoggerMessage(EventId = 114407, Level = LogLevel.Error, Message = "❌ Failed to restore client {ClientIndex} state"
)]
    private partial void LogClientStateRestoreFailed(Exception ex, int clientIndex);

    [LoggerMessage(EventId = 114408, Level = LogLevel.Information, Message = "✅ State restoration completed: {ZoneCount} zones, {ClientCount} clients restored"
)]
    private partial void LogStatesRestored(int zoneCount, int clientCount);

    [LoggerMessage(EventId = 114409, Level = LogLevel.Information, Message = "✅ State restoration service completed successfully"
)]
    private partial void LogStateRestorationCompleted();

    [LoggerMessage(EventId = 114410, Level = LogLevel.Error, Message = "❌ State restoration service failed"
)]
    private partial void LogStateRestorationFailed(Exception ex);

    [LoggerMessage(EventId = 114411, Level = LogLevel.Information, Message = "🆕 Created initial state for zone {ZoneIndex} ({ZoneName}) - Playlist 1, Track 1, Stopped"
)]
    private partial void LogInitialZoneStateCreated(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114412, Level = LogLevel.Error, Message = "❌ Failed to create initial state for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogInitialZoneStateCreationFailed(Exception ex, int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114413, Level = LogLevel.Debug, Message = "🎵 Zone {ZoneIndex} ({ZoneName}) was playing - will resume playback"
)]
    private partial void LogZonePlaybackWillResume(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114414, Level = LogLevel.Information, Message = "▶️ Resuming playback for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogResumingPlayback(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114415, Level = LogLevel.Information, Message = "✅ Playback resumed for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogPlaybackResumed(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114416, Level = LogLevel.Error, Message = "❌ Failed to resume playback for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogPlaybackResumeFailed(Exception ex, int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114417, Level = LogLevel.Debug, Message = "ℹ️ No persisted state found for client {ClientIndex} ({ClientName})"
)]
    private partial void LogClientStateNotFound(int clientIndex, string clientName);

    [LoggerMessage(EventId = 114418, Level = LogLevel.Debug, Message = "🎵 Preloading playlist 1, track 1 for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogPreloadingInitialPlaylist(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114419, Level = LogLevel.Information, Message = "✅ Initial playlist preloaded for zone {ZoneIndex} ({ZoneName}) - Playlist 1, Track 1"
)]
    private partial void LogInitialPlaylistPreloaded(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 114420, Level = LogLevel.Warning, Message = "⚠️ Failed to preload initial playlist for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogInitialPlaylistPreloadFailed(Exception ex, int zoneIndex, string zoneName);

    #endregion
}
