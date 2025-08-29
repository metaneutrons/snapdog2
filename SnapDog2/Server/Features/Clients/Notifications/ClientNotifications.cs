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
namespace SnapDog2.Server.Features.Clients.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a client's volume changes.
/// </summary>
[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the volume changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's mute state changes.
/// </summary>
[StatusId("CLIENT_MUTE_STATUS")]
public record ClientMuteChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets whether the client is muted.
    /// </summary>
    public required bool IsMuted { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the mute state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's latency changes.
/// </summary>
[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the latency changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client is assigned to a different zone.
/// </summary>
[StatusId("CLIENT_ZONE_STATUS")]
public record ClientZoneAssignmentChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new zone ID (null if unassigned).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the previous zone ID (null if was unassigned).
    /// </summary>
    public int? PreviousZoneIndex { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the assignment changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's connection status changes.
/// </summary>
[StatusId("CLIENT_CONNECTED")]
public record ClientConnectionChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the connection status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's complete state changes.
/// </summary>
[StatusId("CLIENT_STATE")]
public record ClientStateChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the complete client state.
    /// </summary>
    public required ClientState ClientState { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the state changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a client's name changes.
/// </summary>
[StatusId("CLIENT_NAME_STATUS")]
public record ClientNameChangedNotification : INotification
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the new client name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the previous client name.
    /// </summary>
    public string? PreviousName { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the name changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
