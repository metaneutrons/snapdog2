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
                    if (persistedState.PlaybackState == true)
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
                PlaybackState = false,
                TrackRepeat = false,
                PlaylistRepeat = false,
                PlaylistShuffle = false,
                SnapcastGroupId = "", // Will be set by Snapcast integration
                SnapcastStreamId = "", // Will be set by Snapcast integration

                Playlist = null, // Will be loaded when playlist is actually loaded
                Track = null, // Will be loaded when track is actually loaded
                Clients = Array.Empty<int>() // Will be populated by client management
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
    /// If track 1 is not available, sets playlist 1 with "No Title" track.
    /// </summary>
    private async Task PreloadInitialPlaylistAsync(int zoneIndex, string zoneName)
    {
        try
        {
            this.LogPreloadingInitialPlaylist(zoneIndex, zoneName);

            // Get all zones and find the target zone
            var allZonesResult = await zoneManager.GetAllZonesAsync();
            if (!allZonesResult.IsSuccess || allZonesResult.Value == null)
            {
                this.LogInitialPlaylistPreloadFailed(new Exception("Failed to retrieve zones"), zoneIndex, zoneName);
                return;
            }

            var zoneService = allZonesResult.Value.FirstOrDefault(z => z.ZoneIndex == zoneIndex);
            if (zoneService != null)
            {
                // Set playlist 1
                var playlistResult = await zoneService.SetPlaylistAsync(1);
                if (playlistResult.IsSuccess)
                {
                    // Try to set track 1
                    var trackResult = await zoneService.PlayTrackAsync(1);
                    if (trackResult.IsSuccess)
                    {
                        this.LogInitialPlaylistPreloaded(zoneIndex, zoneName);
                    }
                    else
                    {
                        // Track 1 not available, set "No Title" track
                        await this.SetNoTitleTrackAsync(zoneIndex, zoneService);
                        this.LogInitialPlaylistPreloadedWithNoTitle(zoneIndex, zoneName);
                    }
                }
                else
                {
                    this.LogInitialPlaylistPreloadFailed(new Exception($"Failed to set playlist 1: {playlistResult.ErrorMessage}"), zoneIndex, zoneName);
                }
            }
            else
            {
                this.LogInitialPlaylistPreloadFailed(new Exception($"Zone {zoneIndex} not found"), zoneIndex, zoneName);
            }
        }
        catch (Exception ex)
        {
            this.LogInitialPlaylistPreloadFailed(ex, zoneIndex, zoneName);
        }
    }

    /// <summary>
    /// Sets a "No Title" track for zones where track 1 is not available.
    /// </summary>
    private async Task SetNoTitleTrackAsync(int zoneIndex, IZoneService zoneService)
    {
        try
        {
            // Create a minimal "No Title" track
            var noTitleTrack = new TrackInfo
            {
                Title = "No Title",
                Artist = "",
                Album = "",
                Source = "System",
                Url = "",
                DurationMs = 0,
                PositionMs = 0,
                Progress = 0,
                CoverArtUrl = "data:image/svg+xml;charset=US-ASCII,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%2272%22%20height%3D%2272%22%20fill%3D%22none%22%20viewBox%3D%220%200%2072%2072%22%3E%3Crect%20width%3D%2272%22%20height%3D%2271.998%22%20fill%3D%22%23198AFF%22%20rx%3D%222.8%22%2F%3E%3Cpath%20fill%3D%22%23fff%22%20d%3D%22M59.0409%2031.8969H15.8403V41.1881H59.0409V31.8969ZM15.8403%2027.2031H54.967V21.216H15.8403V27.2031ZM15.8403%2016.9585H44.2502V12.9609H15.8403V16.9585ZM52.2316%2045.8849H15.8374V51.3643H52.2316V45.8849ZM44.248%2059.0426H15.8374V55.6189H44.248V59.0426Z%22%2F%3E%3C%2Fsvg%3E",
                Genre = "",
                TrackNumber = 1,
                Year = null,
                Rating = null
            };

            // Update zone state with the No Title track
            var currentState = await zoneService.GetStateAsync();
            if (currentState.IsSuccess && currentState.Value != null)
            {
                var updatedState = currentState.Value with
                {
                    Track = noTitleTrack
                };

                zoneStateStore.SetZoneState(zoneIndex, updatedState);
            }
        }
        catch (Exception ex)
        {
            this.LogErrorSettingNoTitleTrack(ex, zoneIndex);
        }
    }

    /// <summary>
    /// Resumes playback for zones that were playing when the application was stopped.
    /// </summary>
    private async Task ResumePlaybackAsync(List<(int zoneIndex, ZoneState zoneState)> zonesToResume)
    {
        // Get all zones once to avoid repeated lookups
        var allZonesResult = await zoneManager.GetAllZonesAsync();
        if (!allZonesResult.IsSuccess || allZonesResult.Value == null)
        {
            LogZoneRetrievalFailed();
            return;
        }

        var allZones = allZonesResult.Value.ToList();

        foreach (var (zoneIndex, zoneState) in zonesToResume)
        {
            try
            {
                this.LogResumingPlayback(zoneIndex, zoneState.Name);

                // Find zone by index instead of calling GetZoneAsync
                var zoneService = allZones.FirstOrDefault(z => z.ZoneIndex == zoneIndex);
                if (zoneService != null)
                {
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
                    this.LogPlaybackResumeFailed(new Exception($"Zone {zoneIndex} not found"), zoneIndex, zoneState.Name);
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
            ZoneConfigs = config.Zones.Select(z => new { z.Name }).ToArray(),
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

    [LoggerMessage(EventId = 14154, Level = LogLevel.Information, Message = "Starting state restoration from persistent storage..."
)]
    private partial void LogStateRestorationStarting();

    [LoggerMessage(EventId = 14155, Level = LogLevel.Warning, Message = "[WARNING] Persistent store unavailable - skipping state restoration"
)]
    private partial void LogPersistentStoreUnavailable();

    [LoggerMessage(EventId = 14156, Level = LogLevel.Information, Message = "Configuration changed: {OldHash} → {NewHash} - clearing old state"
)]
    private partial void LogConfigurationChanged(string oldHash, string newHash);

    [LoggerMessage(EventId = 14157, Level = LogLevel.Information, Message = "First run detected - initializing persistent state"
)]
    private partial void LogFirstRun();

    [LoggerMessage(EventId = 14158, Level = LogLevel.Debug, Message = "[OK] Zone {ZoneIndex} ({ZoneName}) state restored"
)]
    private partial void LogZoneStateRestored(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14159, Level = LogLevel.Error, Message = "[ERROR] Failed → restore zone {ZoneIndex} state"
)]
    private partial void LogZoneStateRestoreFailed(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 14160, Level = LogLevel.Debug, Message = "[OK] Client {ClientIndex} ({ClientName}) state restored"
)]
    private partial void LogClientStateRestored(int clientIndex, string clientName);

    [LoggerMessage(EventId = 14161, Level = LogLevel.Error, Message = "[ERROR] Failed → restore client {ClientIndex} state"
)]
    private partial void LogClientStateRestoreFailed(Exception ex, int clientIndex);

    [LoggerMessage(EventId = 14162, Level = LogLevel.Information, Message = "[OK] State restoration completed: {ZoneCount} zones, {ClientCount} clients restored"
)]
    private partial void LogStatesRestored(int zoneCount, int clientCount);

    [LoggerMessage(EventId = 14163, Level = LogLevel.Information, Message = "[OK] State restoration service completed successfully"
)]
    private partial void LogStateRestorationCompleted();

    [LoggerMessage(EventId = 14164, Level = LogLevel.Error, Message = "[ERROR] State restoration service failed"
)]
    private partial void LogStateRestorationFailed(Exception ex);

    [LoggerMessage(EventId = 14165, Level = LogLevel.Information, Message = "Created initial state for zone {ZoneIndex} ({ZoneName}) - Playlist 1, Track 1, Stopped"
)]
    private partial void LogInitialZoneStateCreated(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14166, Level = LogLevel.Error, Message = "[ERROR] Failed → create initial state for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogInitialZoneStateCreationFailed(Exception ex, int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14167, Level = LogLevel.Debug, Message = "Zone {ZoneIndex} ({ZoneName}) was playing - will resume playback"
)]
    private partial void LogZonePlaybackWillResume(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14168, Level = LogLevel.Information, Message = "Resuming playback for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogResumingPlayback(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14169, Level = LogLevel.Information, Message = "[OK] Playback resumed for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogPlaybackResumed(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14170, Level = LogLevel.Error, Message = "[ERROR] Failed → resume playback for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogPlaybackResumeFailed(Exception ex, int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14171, Level = LogLevel.Debug, Message = "No persisted state found for client {ClientIndex} ({ClientName})"
)]
    private partial void LogClientStateNotFound(int clientIndex, string clientName);

    [LoggerMessage(EventId = 14172, Level = LogLevel.Debug, Message = "Preloading playlist 1, track 1 for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogPreloadingInitialPlaylist(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14173, Level = LogLevel.Information, Message = "[OK] Initial playlist preloaded for zone {ZoneIndex} ({ZoneName}) - Playlist 1, Track 1"
)]
    private partial void LogInitialPlaylistPreloaded(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14174, Level = LogLevel.Warning, Message = "[WARNING] Failed → preload initial playlist for zone {ZoneIndex} ({ZoneName})"
)]
    private partial void LogInitialPlaylistPreloadFailed(Exception ex, int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14175, Level = LogLevel.Error, Message = "[ERROR] Failed to retrieve zones for state restoration")]
    private partial void LogZoneRetrievalFailed();

    [LoggerMessage(EventId = 14176, Level = LogLevel.Information, Message = "[OK] Initial playlist preloaded for zone {ZoneIndex} ({ZoneName}) - Playlist 1, No Title (track 1 unavailable)")]
    private partial void LogInitialPlaylistPreloadedWithNoTitle(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 14177, Level = LogLevel.Warning, Message = "[WARNING] Failed to set No Title track for zone {ZoneIndex}")]
    private partial void LogErrorSettingNoTitleTrack(Exception ex, int zoneIndex);

    #endregion
}
