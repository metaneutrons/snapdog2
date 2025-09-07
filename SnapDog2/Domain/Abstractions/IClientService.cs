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
namespace SnapDog2.Domain.Abstractions;

using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

/// <summary>
/// Service interface for client operations with CommandId-attributed methods.
/// Replaces mediator pattern with direct service calls.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Assigns a client to a specific zone.
    /// </summary>
    [CommandId("CLIENT_ZONE")]
    Task<Result> AssignToZoneAsync(string clientId, int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    [CommandId("CLIENT_VOLUME")]
    Task<Result> SetVolumeAsync(string clientId, int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the mute state for a specific client.
    /// </summary>
    [CommandId("CLIENT_MUTE")]
    Task<Result> SetMuteAsync(string clientId, bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the latency compensation for a specific client.
    /// </summary>
    [CommandId("CLIENT_LATENCY")]
    Task<Result> SetLatencyAsync(string clientId, int latencyMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the name for a specific client.
    /// </summary>
    [CommandId("CLIENT_NAME")]
    Task<Result> SetNameAsync(string clientId, string name, CancellationToken cancellationToken = default);
}
