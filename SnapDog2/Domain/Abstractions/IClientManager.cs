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

using SnapDog2.Shared.Models;

/// <summary>
/// Provides management operations for Snapcast clients.
/// </summary>
public interface IClientManager
{
    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    /// <param name="clientIndex">The client Index.</param>
    /// <returns>A result containing the client if found.</returns>
    Task<Result<IClient>> GetClientAsync(int clientIndex);

    /// <summary>
    /// Gets the state of a specific client.
    /// </summary>
    /// <param name="clientIndex">The client Index.</param>
    /// <returns>A result containing the client state if found.</returns>
    Task<Result<ClientState>> GetClientStateAsync(int clientIndex);

    /// <summary>
    /// Gets the state of all known clients.
    /// </summary>
    /// <returns>A result containing the list of all client states.</returns>
    Task<Result<List<ClientState>>> GetAllClientsAsync();

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <returns>A result containing the list of client states for the zone.</returns>
    Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneIndex);

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of client states for the zone.</returns>
    Task<Result<List<ClientState>>> GetClientsByZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a client to a zone.
    /// </summary>
    /// <param name="clientIndex">The client Index.</param>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AssignClientToZoneAsync(int clientIndex, int zoneIndex);

    /// <summary>
    /// Gets a client by its Snapcast client Index for event bridging.
    /// Used internally by SnapcastService to bridge external events to IClient notifications.
    /// </summary>
    /// <param name="snapcastClientId">The Snapcast client Index.</param>
    /// <returns>The client and its index if found, null and 0 otherwise.</returns>
    Task<(IClient? Client, int ClientIndex)> GetClientBySnapcastIdAsync(string snapcastClientId);
}
