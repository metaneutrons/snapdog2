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
namespace SnapDog2.Domain.Abstractions;

using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

/// <summary>
/// Service interface for zone operations with CommandId-attributed methods.
/// Replaces mediator pattern with direct service calls.
/// </summary>
public interface IZoneService
{
    /// <summary>
    /// Gets the zone index.
    /// </summary>
    int ZoneIndex { get; }

    /// <summary>
    /// Plays/resumes playback on the zone.
    /// </summary>
    [CommandId("PLAY")]
    Task<Result> PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback on the zone.
    /// </summary>
    [CommandId("PAUSE")]
    Task<Result> PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback on the zone.
    /// </summary>
    [CommandId("STOP")]
    Task<Result> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for the zone.
    /// </summary>
    [CommandId("VOLUME")]
    Task<Result> SetVolumeAsync(int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increases volume by step.
    /// </summary>
    [CommandId("VOLUME_UP")]
    Task<Result> VolumeUpAsync(int step = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decreases volume by step.
    /// </summary>
    [CommandId("VOLUME_DOWN")]
    Task<Result> VolumeDownAsync(int step = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the mute state for the zone.
    /// </summary>
    [CommandId("MUTE")]
    Task<Result> SetMuteAsync(bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles mute state.
    /// </summary>
    [CommandId("MUTE_TOGGLE")]
    Task<Result> ToggleMuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays next track.
    /// </summary>
    [CommandId("TRACK_NEXT")]
    Task<Result> NextTrackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays previous track.
    /// </summary>
    [CommandId("TRACK_PREVIOUS")]
    Task<Result> PreviousTrackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays specific track by index.
    /// </summary>
    [CommandId("TRACK")]
    Task<Result> PlayTrackAsync(int trackIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to specific position.
    /// </summary>
    [CommandId("TRACK_POSITION")]
    Task<Result> SeekToPositionAsync(TimeSpan position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to progress percentage (0.0-1.0).
    /// </summary>
    [CommandId("TRACK_PROGRESS")]
    Task<Result> SeekToProgressAsync(double progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets specific playlist by index.
    /// </summary>
    [CommandId("PLAYLIST")]
    Task<Result> SetPlaylistAsync(int playlistIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays next playlist.
    /// </summary>
    [CommandId("PLAYLIST_NEXT")]
    Task<Result> NextPlaylistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays previous playlist.
    /// </summary>
    [CommandId("PLAYLIST_PREVIOUS")]
    Task<Result> PreviousPlaylistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets playlist shuffle state.
    /// </summary>
    [CommandId("PLAYLIST_SHUFFLE")]
    Task<Result> SetPlaylistShuffleAsync(bool shuffle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles playlist shuffle.
    /// </summary>
    [CommandId("PLAYLIST_SHUFFLE_TOGGLE")]
    Task<Result> TogglePlaylistShuffleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets playlist repeat state.
    /// </summary>
    [CommandId("PLAYLIST_REPEAT")]
    Task<Result> SetPlaylistRepeatAsync(bool repeat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles playlist repeat.
    /// </summary>
    [CommandId("PLAYLIST_REPEAT_TOGGLE")]
    Task<Result> TogglePlaylistRepeatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets track repeat state.
    /// </summary>
    [CommandId("TRACK_REPEAT")]
    Task<Result> SetTrackRepeatAsync(bool repeat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles track repeat.
    /// </summary>
    [CommandId("TRACK_REPEAT_TOGGLE")]
    Task<Result> ToggleTrackRepeatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the zone.
    /// </summary>
    Task<Result<ZoneState>> GetStateAsync();

    /// <summary>
    /// Plays a URL directly.
    /// </summary>
    Task<Result> PlayUrlAsync(string mediaUrl);

    /// <summary>
    /// Updates Snapcast group ID (internal).
    /// </summary>
    Task<Result> UpdateSnapcastGroupId(string groupId);
}
