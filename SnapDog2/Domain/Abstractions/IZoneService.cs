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
    /// Starts playback on the specified zone.
    /// </summary>
    [CommandId("PLAY")]
    Task<Result> StartPlaybackAsync(TrackInfo trackInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback on the specified zone.
    /// </summary>
    [CommandId("STOP")]
    Task<Result> StopPlaybackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback on the specified zone.
    /// </summary>
    [CommandId("PAUSE")]
    Task<Result> PausePlaybackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for the specified zone.
    /// </summary>
    [CommandId("VOLUME")]
    Task<Result> SetVolumeAsync(int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the mute state for the specified zone.
    /// </summary>
    [CommandId("MUTE")]
    Task<Result> SetMuteAsync(bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a specific track by index.
    /// </summary>
    [CommandId("TRACK")]
    Task<Result> SetTrackAsync(int trackIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to a specific position in the current track.
    /// </summary>
    [CommandId("TRACK_POSITION")]
    Task<Result> SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a specific playlist by index.
    /// </summary>
    [CommandId("PLAYLIST")]
    Task<Result> SetPlaylistAsync(int playlistIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the zone.
    /// </summary>
    Task<Result<ZoneState>> GetStateAsync();

    /// <summary>
    /// Plays the current track.
    /// </summary>
    Task<Result> PlayAsync();

    /// <summary>
    /// Plays a URL directly.
    /// </summary>
    Task<Result> PlayUrlAsync(string mediaUrl);

    // Additional methods required by handlers
    Task<Result> UpdateSnapcastGroupId(string groupId);
    Task<Result> PauseAsync(CancellationToken cancellationToken = default);
    Task<Result> StopAsync(CancellationToken cancellationToken = default);
    Task<Result> NextTrackAsync(CancellationToken cancellationToken = default);
    Task<Result> PreviousTrackAsync(CancellationToken cancellationToken = default);
    Task<Result> SetPlaylistShuffleAsync(bool shuffle, CancellationToken cancellationToken = default);
    Task<Result> SetPlaylistRepeatAsync(bool repeat, CancellationToken cancellationToken = default);
    Task<Result> SeekToProgressAsync(double progress, CancellationToken cancellationToken = default);
    Task<Result> PlayTrackAsync(int trackIndex, CancellationToken cancellationToken = default);
    Task<Result> SeekToPositionAsync(TimeSpan position, CancellationToken cancellationToken = default);
    Task<Result> TogglePlaylistRepeatAsync(CancellationToken cancellationToken = default);
    Task<Result> TogglePlaylistShuffleAsync(CancellationToken cancellationToken = default);
    Task<Result> ToggleTrackRepeatAsync(CancellationToken cancellationToken = default);
    Task<Result> SetTrackRepeatAsync(bool repeat, CancellationToken cancellationToken = default);
    Task<Result> PreviousPlaylistAsync(CancellationToken cancellationToken = default);
    Task<Result> NextPlaylistAsync(CancellationToken cancellationToken = default);
    Task<Result> ToggleMuteAsync(CancellationToken cancellationToken = default);
    Task<Result> VolumeDownAsync(int step = 5, CancellationToken cancellationToken = default);
    Task<Result> VolumeUpAsync(int step = 5, CancellationToken cancellationToken = default);
}
