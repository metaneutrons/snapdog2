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
namespace SnapDog2.Server.Snapcast.Notifications;

using Cortex.Mediator.Notifications;
using SnapcastClient.Models;

/// <summary>
/// Base class for all Snapcast-related notifications.
/// </summary>
public abstract record SnapcastNotification : INotification
{
    /// <summary>
    /// Timestamp when the notification was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification sent when a Snapcast client connects.
/// </summary>
public record SnapcastClientConnectedNotification(SnapClient Client) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast client disconnects.
/// </summary>
public record SnapcastClientDisconnectedNotification(SnapClient Client) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast client's volume changes.
/// </summary>
public record SnapcastClientVolumeChangedNotification(string ClientIndex, ClientVolume Volume)
    : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast client's latency changes.
/// </summary>
public record SnapcastClientLatencyChangedNotification(string ClientIndex, int LatencyMs) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast client's name changes.
/// </summary>
public record SnapcastClientNameChangedNotification(string ClientIndex, string Name) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast group's mute state changes.
/// </summary>
public record SnapcastGroupMuteChangedNotification(string GroupId, bool Muted) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast group's stream changes.
/// </summary>
public record SnapcastGroupStreamChangedNotification(string GroupId, string StreamId) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast group's name changes.
/// </summary>
public record SnapcastGroupNameChangedNotification(string GroupId, string Name) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast stream's properties change.
/// </summary>
public record SnapcastStreamPropertiesChangedNotification(string StreamId, Dictionary<string, object> Properties)
    : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast stream is updated.
/// </summary>
public record SnapcastStreamUpdatedNotification(Stream Stream) : SnapcastNotification;

/// <summary>
/// Notification sent when the Snapcast server status is updated.
/// </summary>
public record SnapcastServerUpdatedNotification(Server Server) : SnapcastNotification;

/// <summary>
/// Notification sent when the Snapcast connection is established.
/// </summary>
public record SnapcastConnectionEstablishedNotification : SnapcastNotification;

/// <summary>
/// Notification sent when the Snapcast connection is lost.
/// </summary>
public record SnapcastConnectionLostNotification(string Reason) : SnapcastNotification;

/// <summary>
/// Notification sent when a Snapcast operation fails.
/// </summary>
public record SnapcastOperationFailedNotification(string Operation, string Error) : SnapcastNotification;
