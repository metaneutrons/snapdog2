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
namespace SnapDog2.Shared.Models;

/// <summary>
/// Playback status information for a zone.
/// </summary>
public class PlaybackStatus
{
    /// <summary>
    /// Zone ID for this playback status.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Whether audio is currently playing in this zone.
    /// </summary>
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Number of currently active audio streams across all zones.
    /// </summary>
    public int ActiveStreams { get; set; }

    /// <summary>
    /// Maximum number of concurrent streams allowed.
    /// </summary>
    public int MaxStreams { get; set; }

    /// <summary>
    /// Current track information if playing.
    /// </summary>
    public TrackInfo? CurrentTrack { get; set; }

    /// <summary>
    /// Audio format being used for playback.
    /// </summary>
    public AudioFormat? AudioFormat { get; set; }

    /// <summary>
    /// Timestamp when playback started (UTC).
    /// </summary>
    public DateTime? PlaybackStartedAt { get; set; }
}
