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
    /// Gets the total count of clients.
    /// </summary>
    Task<Result<int>> GetClientsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all clients.
    /// </summary>
    Task<Result<List<ClientState>>> GetAllClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific client by index.
    /// </summary>
    Task<Result<ClientState>> GetClientAsync(int clientIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a client to a specific zone.
    /// </summary>
    [CommandId("CLIENT_ZONE")]
    Task<Result> AssignToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for a specific client.
    /// </summary>
    [CommandId("CLIENT_VOLUME")]
    Task<Result> SetVolumeAsync(int clientIndex, int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increases client volume by specified step.
    /// </summary>
    [CommandId("CLIENT_VOLUME_UP")]
    Task<Result> VolumeUpAsync(int clientIndex, int step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decreases client volume by specified step.
    /// </summary>
    [CommandId("CLIENT_VOLUME_DOWN")]
    Task<Result> VolumeDownAsync(int clientIndex, int step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the mute state for a specific client.
    /// </summary>
    [CommandId("CLIENT_MUTE")]
    Task<Result> SetMuteAsync(int clientIndex, bool muted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the mute state for a specific client.
    /// </summary>
    [CommandId("CLIENT_MUTE_TOGGLE")]
    Task<Result> ToggleMuteAsync(int clientIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the latency compensation for a specific client.
    /// </summary>
    [CommandId("CLIENT_LATENCY")]
    Task<Result> SetLatencyAsync(int clientIndex, int latencyMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the name for a specific client.
    /// </summary>
    [CommandId("CLIENT_NAME")]
    Task<Result> SetNameAsync(int clientIndex, string name, CancellationToken cancellationToken = default);
}
