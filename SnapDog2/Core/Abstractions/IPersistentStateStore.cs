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
namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Interface for persistent state storage across application restarts.
/// Handles both zone and client state persistence with configuration fingerprinting.
/// </summary>
public interface IPersistentStateStore
{
    #region Zone State Persistence

    /// <summary>
    /// Saves a single zone state immediately (event-based).
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="zoneState">Zone state to save</param>
    Task SaveZoneStateAsync(int zoneIndex, ZoneState zoneState);

    /// <summary>
    /// Loads all persisted zone states.
    /// </summary>
    /// <returns>Dictionary of zone states by zone index</returns>
    Task<Dictionary<int, ZoneState>> LoadAllZoneStatesAsync();

    #endregion

    #region Client State Persistence

    /// <summary>
    /// Saves a single client state immediately (event-based).
    /// </summary>
    /// <param name="clientIndex">Client index (1-based)</param>
    /// <param name="clientState">Client state to save</param>
    Task SaveClientStateAsync(int clientIndex, ClientState clientState);

    /// <summary>
    /// Loads all persisted client states.
    /// </summary>
    /// <returns>Dictionary of client states by client index</returns>
    Task<Dictionary<int, ClientState>> LoadAllClientStatesAsync();

    #endregion

    #region Configuration Management

    /// <summary>
    /// Gets the stored configuration fingerprint to detect config changes.
    /// </summary>
    Task<ConfigurationFingerprint?> GetConfigurationFingerprintAsync();

    /// <summary>
    /// Saves the current configuration fingerprint.
    /// </summary>
    /// <param name="fingerprint">Configuration fingerprint</param>
    Task SaveConfigurationFingerprintAsync(ConfigurationFingerprint fingerprint);

    /// <summary>
    /// Clears all persisted state (used when configuration changes).
    /// </summary>
    Task ClearAllStateAsync();

    #endregion

    #region Health and Diagnostics

    /// <summary>
    /// Checks if the persistent store is healthy and accessible.
    /// </summary>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// Gets storage statistics for monitoring.
    /// </summary>
    Task<PersistentStoreStats> GetStatsAsync();

    #endregion
}

/// <summary>
/// Configuration fingerprint for detecting configuration changes.
/// </summary>
public record ConfigurationFingerprint
{
    public required string Hash { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int ZoneCount { get; init; }
    public required int ClientCount { get; init; }
    public required string[] ZoneNames { get; init; }
    public required string[] ClientNames { get; init; }
}

/// <summary>
/// Statistics about the persistent state store.
/// </summary>
public record PersistentStoreStats
{
    public required int ZoneStatesCount { get; init; }
    public required int ClientStatesCount { get; init; }
    public required DateTime LastSaveTime { get; init; }
    public required long StorageSizeBytes { get; init; }
    public required bool IsHealthy { get; init; }
}
