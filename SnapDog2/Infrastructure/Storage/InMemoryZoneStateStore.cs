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
namespace SnapDog2.Infrastructure.Storage;

using System.Collections.Concurrent;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Events;
using SnapDog2.Shared.Models;

/// <summary>
/// In-memory implementation of zone state storage.
/// Thread-safe and persists state across HTTP requests.
/// </summary>
public class InMemoryZoneStateStore : IZoneStateStore
{
    private readonly ConcurrentDictionary<int, ZoneState> _zoneStates = new();

    /// <summary>
    /// Event raised when zone state changes.
    /// </summary>
    public event EventHandler<ZoneStateChangedEventArgs>? ZoneStateChanged;

    /// <summary>
    /// Event raised when zone playlist changes.
    /// </summary>
    public event EventHandler<ZonePlaylistChangedEventArgs>? ZonePlaylistChanged;

    /// <summary>
    /// Event raised when zone volume changes.
    /// </summary>
    public event EventHandler<ZoneVolumeChangedEventArgs>? ZoneVolumeChanged;

    /// <summary>
    /// Event raised when zone track changes.
    /// </summary>
    public event EventHandler<ZoneTrackChangedEventArgs>? ZoneTrackChanged;

    /// <summary>
    /// Event raised when zone playback state changes.
    /// </summary>
    public event EventHandler<ZonePlaybackStateChangedEventArgs>? ZonePlaybackStateChanged;

    public ZoneState? GetZoneState(int zoneIndex)
    {
        return this._zoneStates.TryGetValue(zoneIndex, out var state) ? state : null;
    }

    public void SetZoneState(int zoneIndex, ZoneState newState)
    {
        var oldState = GetZoneState(zoneIndex);
        this._zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);

        // Debug logging
        Console.WriteLine($"DEBUG: SetZoneState called for zone {zoneIndex}");
        Console.WriteLine($"DEBUG: Old playlist index: {oldState?.Playlist?.Index}, New playlist index: {newState.Playlist?.Index}");

        // Detect and publish specific changes
        DetectAndPublishChanges(zoneIndex, oldState, newState);

        // Always fire general state change
        ZoneStateChanged?.Invoke(this, new ZoneStateChangedEventArgs(zoneIndex, oldState, newState));
    }

    public Dictionary<int, ZoneState> GetAllZoneStates()
    {
        return this._zoneStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void InitializeZoneState(int zoneIndex, ZoneState defaultState)
    {
        this._zoneStates.TryAdd(zoneIndex, defaultState);
    }

    private void DetectAndPublishChanges(int zoneIndex, ZoneState? oldState, ZoneState newState)
    {
        Console.WriteLine($"DEBUG: DetectAndPublishChanges called for zone {zoneIndex}");

        // Playlist changes
        if (oldState?.Playlist?.Index != newState.Playlist?.Index)
        {
            Console.WriteLine($"DEBUG: Playlist change detected! Old: {oldState?.Playlist?.Index}, New: {newState.Playlist?.Index}");
            ZonePlaylistChanged?.Invoke(this, new ZonePlaylistChangedEventArgs(
                zoneIndex, oldState?.Playlist, newState.Playlist));
        }
        else
        {
            Console.WriteLine($"DEBUG: No playlist change detected. Old: {oldState?.Playlist?.Index}, New: {newState.Playlist?.Index}");
        }

        // Volume changes
        if (oldState?.Volume != newState.Volume)
        {
            Console.WriteLine($"DEBUG: Volume change detected! Old: {oldState?.Volume}, New: {newState.Volume}");
            ZoneVolumeChanged?.Invoke(this, new ZoneVolumeChangedEventArgs(
                zoneIndex, oldState?.Volume ?? 0, newState.Volume));
        }

        // Track changes
        if (oldState?.Track?.Index != newState.Track?.Index)
        {
            Console.WriteLine($"DEBUG: Track change detected! Old: {oldState?.Track?.Index}, New: {newState.Track?.Index}");
            ZoneTrackChanged?.Invoke(this, new ZoneTrackChangedEventArgs(
                zoneIndex, oldState?.Track, newState.Track));
        }

        // Playback state changes
        if (oldState?.PlaybackState != newState.PlaybackState)
        {
            Console.WriteLine($"DEBUG: Playback state change detected! Old: {oldState?.PlaybackState}, New: {newState.PlaybackState}");
            ZonePlaybackStateChanged?.Invoke(this, new ZonePlaybackStateChangedEventArgs(
                zoneIndex, oldState?.PlaybackState ?? Shared.Enums.PlaybackState.Stopped, newState.PlaybackState));
        }
    }
}
