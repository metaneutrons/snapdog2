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
using SnapDog2.Core.Attributes;

namespace SnapDog2.Core.Notifications;

/// <summary>
/// Notification for playlist count status changes.
/// </summary>
[StatusId("PLAYLIST_COUNT_STATUS")]
public class PlaylistCountStatusNotification
{
    /// <summary>
    /// Gets or sets the zone index.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Gets or sets the playlist count.
    /// </summary>
    public int PlaylistCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this status was generated.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
