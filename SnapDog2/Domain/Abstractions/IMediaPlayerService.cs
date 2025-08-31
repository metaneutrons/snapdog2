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

using SnapDog2.Shared.Models;

/// <summary>
/// Interface for media player services that handle audio streaming to zones.
/// </summary>
public interface IMediaPlayerService
{
    /// <summary>
    /// Starts playing audio for the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to play audio in</param>
    /// <param name="trackInfo">Information about the track to play</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> PlayAsync(int zoneIndex, TrackInfo trackInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback for the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to stop playback in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> StopAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback for the specified zone.
    /// Note: For streaming audio, this may be equivalent to stop.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to pause playback in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> PauseAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current playback status for a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to get status for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the playback status</returns>
    Task<Result<PlaybackStatus>> GetStatusAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current playback status for all zones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing playback status for all zones</returns>
    Task<Result<IEnumerable<PlaybackStatus>>> GetAllStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the MediaPlayer instance for a specific zone (for event subscription).
    /// </summary>
    /// <param name="zoneIndex">Zone index</param>
    /// <returns>MediaPlayer instance or null if not found</returns>
    Infrastructure.Audio.MediaPlayer? GetMediaPlayer(int zoneIndex);

    /// <summary>
    /// Stops all active playback across all zones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> StopAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system-wide playback statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing playback statistics</returns>
    Task<Result<PlaybackStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to a specific position in the current track for the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to seek in</param>
    /// <param name="positionMs">Position in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SeekToPositionAsync(int zoneIndex, long positionMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to a specific progress percentage in the current track for the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to seek in</param>
    /// <param name="progress">Progress percentage (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SeekToProgressAsync(int zoneIndex, float progress, CancellationToken cancellationToken = default);
}
