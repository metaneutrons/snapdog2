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
namespace SnapDog2.Server.Features.Zones.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;

/// <summary>
/// Notification published when a zone's current playlist name changes.
/// Contains the display name of the currently active playlist.
/// </summary>
[StatusId("PLAYLIST_NAME_STATUS")]
public record PlaylistNameStatusNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the current playlist name.
    /// </summary>
    public required string PlaylistName { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the playlist name status was published.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
