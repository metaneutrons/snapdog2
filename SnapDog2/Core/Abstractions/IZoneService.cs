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
/// Service for controlling a specific audio zone.
/// </summary>
public interface IZoneService
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    int ZoneIndex { get; }

    /// <summary>
    /// Gets the current zone state.
    /// </summary>
    /// <returns>The current zone state.</returns>
    Task<Result<ZoneState>> GetStateAsync();

    // Playback Control
    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> PlayAsync();

    /// <summary>
    /// Plays a specific track by index.
    /// </summary>
    /// <param name="trackIndex">The track index (1-based).</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> PlayTrackAsync(int trackIndex);

    /// <summary>
    /// Plays media from a URL.
    /// </summary>
    /// <param name="mediaUrl">The media URL.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> PlayUrlAsync(string mediaUrl);

    /// <summary>
    /// Pauses playback.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> PauseAsync();

    /// <summary>
    /// Stops playback.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> StopAsync();

    // Volume Control
    /// <summary>
    /// Sets the zone volume.
    /// </summary>
    /// <param name="volume">The volume level (0-100).</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetVolumeAsync(int volume);

    /// <summary>
    /// Increases the zone volume.
    /// </summary>
    /// <param name="step">The volume step to increase.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> VolumeUpAsync(int step = 5);

    /// <summary>
    /// Decreases the zone volume.
    /// </summary>
    /// <param name="step">The volume step to decrease.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> VolumeDownAsync(int step = 5);

    /// <summary>
    /// Sets the zone mute state.
    /// </summary>
    /// <param name="enabled">Whether to mute the zone.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetMuteAsync(bool enabled);

    /// <summary>
    /// Toggles the zone mute state.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> ToggleMuteAsync();

    // Track Management
    /// <summary>
    /// Sets a specific track.
    /// </summary>
    /// <param name="trackIndex">The track index (1-based).</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetTrackAsync(int trackIndex);

    /// <summary>
    /// Plays the next track.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> NextTrackAsync();

    /// <summary>
    /// Plays the previous track.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> PreviousTrackAsync();

    /// <summary>
    /// Sets track repeat mode.
    /// </summary>
    /// <param name="enabled">Whether to enable track repeat.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetTrackRepeatAsync(bool enabled);

    /// <summary>
    /// Toggles track repeat mode.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> ToggleTrackRepeatAsync();

    // Playlist Management
    /// <summary>
    /// Sets a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetPlaylistAsync(int playlistIndex);

    /// <summary>
    /// Sets a specific playlist by ID.
    /// </summary>
    /// <param name="playlistIndex">The playlist ID.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetPlaylistAsync(string playlistIndex);

    /// <summary>
    /// Plays the next playlist.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> NextPlaylistAsync();

    /// <summary>
    /// Plays the previous playlist.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> PreviousPlaylistAsync();

    /// <summary>
    /// Sets playlist shuffle mode.
    /// </summary>
    /// <param name="enabled">Whether to enable playlist shuffle.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetPlaylistShuffleAsync(bool enabled);

    /// <summary>
    /// Toggles playlist shuffle mode.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> TogglePlaylistShuffleAsync();

    /// <summary>
    /// Sets playlist repeat mode.
    /// </summary>
    /// <param name="enabled">Whether to enable playlist repeat.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SetPlaylistRepeatAsync(bool enabled);

    /// <summary>
    /// Toggles playlist repeat mode.
    /// </summary>
    /// <returns>Result of the operation.</returns>
    Task<Result> TogglePlaylistRepeatAsync();

    /// <summary>
    /// Seeks to a specific position in the current track.
    /// </summary>
    /// <param name="positionMs">Position in milliseconds</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SeekToPositionAsync(long positionMs);

    /// <summary>
    /// Seeks to a specific progress percentage in the current track.
    /// </summary>
    /// <param name="progress">Progress percentage (0.0-1.0)</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SeekToProgressAsync(float progress);

    // Status Publishing Methods (Blueprint Compliance)
    /// <summary>
    /// Publishes the current playback state status.
    /// </summary>
    /// <param name="playbackState">The playback state to publish.</param>
    /// <returns>Task representing the async operation.</returns>
    Task PublishPlaybackStateStatusAsync(Core.Enums.PlaybackState playbackState);

    /// <summary>
    /// Publishes the current volume status.
    /// </summary>
    /// <param name="volume">The volume level to publish (0-100).</param>
    /// <returns>Task representing the async operation.</returns>
    Task PublishVolumeStatusAsync(int volume);

    /// <summary>
    /// Publishes the current mute status.
    /// </summary>
    /// <param name="isMuted">The mute state to publish.</param>
    /// <returns>Task representing the async operation.</returns>
    Task PublishMuteStatusAsync(bool isMuted);

    /// <summary>
    /// Publishes the current track status.
    /// </summary>
    /// <param name="trackInfo">The track information to publish.</param>
    /// <param name="trackIndex">The track index to publish (1-based).</param>
    /// <returns>Task representing the async operation.</returns>
    Task PublishTrackStatusAsync(Core.Models.TrackInfo trackInfo, int trackIndex);

    /// <summary>
    /// Publishes the current playlist status.
    /// </summary>
    /// <param name="playlistInfo">The playlist information to publish.</param>
    /// <param name="playlistIndex">The playlist index to publish (1-based).</param>
    /// <returns>Task representing the async operation.</returns>
    Task PublishPlaylistStatusAsync(Core.Models.PlaylistInfo playlistInfo, int playlistIndex);

}
