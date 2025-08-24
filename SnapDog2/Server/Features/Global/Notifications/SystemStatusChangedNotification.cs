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
namespace SnapDog2.Server.Features.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when the system status changes.
/// </summary>
[StatusId("SYSTEM_STATUS")]
public record SystemStatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the new system status.
    /// </summary>
    public required SystemStatus Status { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
