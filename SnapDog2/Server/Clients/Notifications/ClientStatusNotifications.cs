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
namespace SnapDog2.Server.Clients.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

/// <summary>
/// Notification published when a client's volume status changes.
/// </summary>
[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeStatusNotification(int ClientIndex, int Volume) : INotification;

/// <summary>
/// Notification published when a client's mute status changes.
/// </summary>
[StatusId("CLIENT_MUTE_STATUS")]
public record ClientMuteStatusNotification(int ClientIndex, bool Muted) : INotification;

/// <summary>
/// Notification published when a client's latency status changes.
/// </summary>
[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyStatusNotification(int ClientIndex, int LatencyMs) : INotification;

/// <summary>
/// Notification published when a client's zone assignment changes.
/// </summary>
[StatusId("CLIENT_ZONE_STATUS")]
public record ClientZoneStatusNotification(int ClientIndex, int? ZoneIndex) : INotification;

/// <summary>
/// Notification published when a client's connection status changes.
/// </summary>
[StatusId("CLIENT_CONNECTED")]
public record ClientConnectionStatusNotification(int ClientIndex, bool IsConnected) : INotification;

/// <summary>
/// Notification published when a client's complete state changes.
/// </summary>
[StatusId("CLIENT_STATE")]
public record ClientStateNotification(int ClientIndex, ClientState State) : INotification;
