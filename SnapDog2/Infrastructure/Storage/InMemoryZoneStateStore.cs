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
using System.Reflection;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Events;
using SnapDog2.Shared.Models;

/// <summary>
/// In-memory implementation of zone state storage.
/// Thread-safe and persists state across HTTP requests.
/// </summary>
public class InMemoryZoneStateStore : IZoneStateStore
{
    private readonly ConcurrentDictionary<int, ZoneState> _zoneStates = new();
    private readonly ServicesConfig _servicesConfig;

    // Position debouncing (configurable)
    private readonly ConcurrentDictionary<int, Timer> _positionTimers = new();
    private readonly ConcurrentDictionary<int, ZoneState> _pendingPositionStates = new();

    public InMemoryZoneStateStore(IOptions<ServicesConfig> servicesOptions)
    {
        _servicesConfig = servicesOptions.Value;
    }

    /// <summary>
    /// Event raised when zone state changes.
    /// </summary>
    public event EventHandler<ZoneStateChangedEventArgs>? ZoneStateChanged;

    /// <summary>
    /// Event raised when zone playlist changes.
    /// </summary>
    public event EventHandler<ZonePlaylistChangedEventArgs>? ZonePlaylistChanged;

    /// <summary>
    /// Event raised when zone position changes (debounced to 500ms).
    /// </summary>
    public event EventHandler<ZonePositionChangedEventArgs>? ZonePositionChanged;

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

        // Check if this is a position-only update (debounce these)
        if (IsPositionOnlyUpdate(oldState, newState))
        {
            DebouncePositionUpdate(zoneIndex, newState);
            return; // Don't fire other events for position-only changes
        }

        // For non-position changes, fire events immediately
        DetectAndPublishChanges(zoneIndex, oldState, newState);
        ZoneStateChanged?.Invoke(this, new ZoneStateChangedEventArgs(zoneIndex, oldState, newState));
    }

    /// <summary>
    /// Surgical updates - only trigger specific change events
    /// </summary>
    public void UpdateTrack(int zoneIndex, TrackInfo track)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var oldTrack = currentState.Track;
        var newState = currentState with { Track = track };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);

        // Only fire track change event
        if (oldTrack?.Index != track?.Index || oldTrack?.Title != track?.Title)
        {
            ZoneTrackChanged?.Invoke(this, new ZoneTrackChangedEventArgs(zoneIndex, oldTrack, track));
        }
    }

    public void UpdateVolume(int zoneIndex, int volume)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var oldVolume = currentState.Volume;
        var newState = currentState with { Volume = volume };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);

        // Only fire volume change event
        if (oldVolume != volume)
        {
            ZoneVolumeChanged?.Invoke(this, new ZoneVolumeChangedEventArgs(zoneIndex, oldVolume, volume));
        }
    }

    public void UpdatePlaybackState(int zoneIndex, PlaybackState state)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var oldState = currentState.PlaybackState;
        var newZoneState = currentState with { PlaybackState = state };
        _zoneStates.AddOrUpdate(zoneIndex, newZoneState, (_, _) => newZoneState);

        // Only fire playback state change event
        if (oldState != state)
        {
            ZonePlaybackStateChanged?.Invoke(this, new ZonePlaybackStateChangedEventArgs(zoneIndex, oldState, state));
        }
    }

    public void UpdatePlaylist(int zoneIndex, PlaylistInfo playlist)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var oldPlaylist = currentState.Playlist;
        var newState = currentState with { Playlist = playlist };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);

        // Only fire playlist change event
        if (oldPlaylist?.Index != playlist?.Index || oldPlaylist?.Name != playlist?.Name)
        {
            ZonePlaylistChanged?.Invoke(this, new ZonePlaylistChangedEventArgs(zoneIndex, oldPlaylist, playlist));
        }
    }

    public void UpdatePosition(int zoneIndex, int positionMs, double progress)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState?.Track == null)
        {
            return;
        }

        // Only update if position actually changed (avoid spam)
        var currentPositionMs = currentState.Track.PositionMs ?? 0;

        // For radio streams (progress=0), use position change threshold of 500ms
        // For regular tracks, use both position and progress thresholds
        var positionChanged = Math.Abs(currentPositionMs - positionMs) >= 500;
        var progressChanged = progress > 0 && Math.Abs((double)(currentState.Track.Progress ?? 0) - progress) >= 0.01;

        if (!positionChanged && !progressChanged)
        {
            return; // Skip if no significant change
        }

        var updatedTrack = currentState.Track with { PositionMs = positionMs, Progress = (float?)progress };
        var newState = currentState with { Track = updatedTrack };

        // Update state first
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);

        // Use debounced position update for high-frequency changes
        DebouncePositionUpdate(zoneIndex, newState);
    }

    public void UpdateMute(int zoneIndex, bool mute)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var newState = currentState with { Mute = mute };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);

        // Fire mute change event if needed (add to events if required)
    }

    public void UpdateRepeat(int zoneIndex, bool trackRepeat, bool playlistRepeat)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var newState = currentState with { TrackRepeat = trackRepeat, PlaylistRepeat = playlistRepeat };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);
    }

    public void UpdateShuffle(int zoneIndex, bool shuffle)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var newState = currentState with { PlaylistShuffle = shuffle };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);
    }

    public void UpdateClients(int zoneIndex, int[] clientIds)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        var newState = currentState with { Clients = clientIds };
        _zoneStates.AddOrUpdate(zoneIndex, newState, (_, _) => newState);
    }

    public void PublishCurrentState(int zoneIndex)
    {
        var currentState = GetZoneState(zoneIndex);
        if (currentState == null)
        {
            return;
        }

        // Use reflection to fire all zone events automatically
        var eventFields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.Name.StartsWith("Zone") && f.Name.EndsWith("Changed"));

        foreach (var eventField in eventFields)
        {
            var eventDelegate = (MulticastDelegate?)eventField.GetValue(this);
            if (eventDelegate == null)
            {
                continue;
            }

            var eventArgs = CreateEventArgs(eventField.Name, zoneIndex, currentState);
            if (eventArgs != null)
            {
                eventDelegate.DynamicInvoke(this, eventArgs);
            }
        }
    }

    private object? CreateEventArgs(string eventName, int zoneIndex, ZoneState currentState)
    {
        return eventName switch
        {
            "ZoneTrackChanged" when currentState.Track != null &&
                                   !string.IsNullOrEmpty(currentState.Track.Title) &&
                                   currentState.Track.Title != "No Track"
                => new ZoneTrackChangedEventArgs(zoneIndex, null, currentState.Track),

            "ZonePlaylistChanged" when currentState.Playlist != null
                => new ZonePlaylistChangedEventArgs(zoneIndex, null, currentState.Playlist),

            "ZoneVolumeChanged"
                => new ZoneVolumeChangedEventArgs(zoneIndex, 0, currentState.Volume),

            "ZonePlaybackStateChanged"
                => new ZonePlaybackStateChangedEventArgs(zoneIndex, PlaybackState.Stopped, currentState.PlaybackState),

            _ => null // Skip unknown or conditional events
        };
    }

    private static bool IsPositionOnlyUpdate(ZoneState? oldState, ZoneState newState)
    {
        if (oldState == null)
        {
            return false;
        }

        // Check if ONLY position/progress changed, everything else is identical
        return oldState.Volume == newState.Volume &&
               oldState.Mute == newState.Mute &&
               oldState.PlaybackState == newState.PlaybackState &&
               oldState.Playlist?.Index == newState.Playlist?.Index &&
               oldState.Track?.Index == newState.Track?.Index &&
               oldState.Track?.Title == newState.Track?.Title &&
               oldState.Track?.Artist == newState.Track?.Artist &&
               oldState.Track?.Album == newState.Track?.Album &&
               // Only position/progress can be different
               (oldState.Track?.PositionMs != newState.Track?.PositionMs ||
                oldState.Track?.Progress != newState.Track?.Progress);
    }

    private void DebouncePositionUpdate(int zoneIndex, ZoneState newState)
    {
        _pendingPositionStates[zoneIndex] = newState;

        // Only start timer if none exists (throttling, not debouncing)
        if (!_positionTimers.ContainsKey(zoneIndex))
        {
            // Start new configurable timer
            _positionTimers[zoneIndex] = new Timer(
                callback: _ => PublishDebouncedPosition(zoneIndex),
                state: null,
                dueTime: TimeSpan.FromMilliseconds(_servicesConfig.DebouncingMs),
                period: Timeout.InfiniteTimeSpan);
        }
        // If timer exists, just update the pending state (don't reset timer)
    }

    private void PublishDebouncedPosition(int zoneIndex)
    {
        if (_pendingPositionStates.TryRemove(zoneIndex, out var state))
        {
            // Fire debounced position event (max every 500ms)
            ZonePositionChanged?.Invoke(this, new ZonePositionChangedEventArgs(zoneIndex, state.Track));
        }

        if (_positionTimers.TryRemove(zoneIndex, out var timer))
        {
            timer.Dispose();
        }
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

        // Playlist changes - check index, name, and other key properties
        if (oldState?.Playlist?.Index != newState.Playlist?.Index ||
            oldState?.Playlist?.Name != newState.Playlist?.Name ||
            oldState?.Playlist?.TrackCount != newState.Playlist?.TrackCount)
        {
            ZonePlaylistChanged?.Invoke(this, new ZonePlaylistChangedEventArgs(
                zoneIndex, oldState?.Playlist, newState.Playlist));
        }
        else
        {
        }

        // Volume changes
        if (oldState?.Volume != newState.Volume)
        {
            ZoneVolumeChanged?.Invoke(this, new ZoneVolumeChangedEventArgs(
                zoneIndex, oldState?.Volume ?? 0, newState.Volume));
        }

        // Track changes
        if (oldState?.Track?.Index != newState.Track?.Index)
        {
            ZoneTrackChanged?.Invoke(this, new ZoneTrackChangedEventArgs(
                zoneIndex, oldState?.Track, newState.Track));
        }

        // Playback state changes
        if (oldState?.PlaybackState != newState.PlaybackState)
        {
            ZonePlaybackStateChanged?.Invoke(this, new ZonePlaybackStateChangedEventArgs(
                zoneIndex, oldState?.PlaybackState ?? Shared.Enums.PlaybackState.Stopped, newState.PlaybackState));
        }
    }
}
