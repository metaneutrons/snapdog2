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
namespace SnapDog2.Server.Shared.Notifications;

using System;
using Cortex.Mediator.Notifications;

/// <summary>
/// Generic notification published when any tracked status changes within the system.
/// This is used by infrastructure adapters (MQTT, KNX) for protocol-agnostic status updates.
/// </summary>
public record StatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the type of status that changed (matches Command Framework Status IDs).
    /// </summary>
    public required string StatusType { get; init; }

    /// <summary>
    /// Gets the index of the entity whose status changed (zone index or client index).
    /// </summary>
    public required int TargetIndex { get; init; }

    /// <summary>
    /// Gets the new value of the status.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
