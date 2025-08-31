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
namespace SnapDog2.Server.Global.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

/// <summary>
/// Notification published when a system error occurs.
/// </summary>
[StatusId("SYSTEM_ERROR")]
public record SystemErrorNotification : INotification
{
    /// <summary>
    /// Gets the error details.
    /// </summary>
    public required ErrorDetails Error { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the error occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
