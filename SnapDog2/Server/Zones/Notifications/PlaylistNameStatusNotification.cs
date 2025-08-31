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
namespace SnapDog2.Server.Zone.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

/// <summary>
/// Notification published when a zone's playlist name status changes.
/// </summary>
[StatusId("PLAYLIST_NAME_STATUS")]
public record PlaylistNameStatusNotification(int ZoneIndex, string? PlaylistName) : INotification;
