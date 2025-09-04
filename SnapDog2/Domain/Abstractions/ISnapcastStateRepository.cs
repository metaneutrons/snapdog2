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

using SnapDog2.Infrastructure.Integrations.Snapcast.Models;

/// <summary>
/// Repository for managing raw Snapcast server state.
/// Holds the latest known state received from the Snapcast server using raw library models.
/// </summary>
public interface ISnapcastStateRepository
{
    /// <summary>
    /// Updates the complete server state from a status response.
    /// </summary>
    /// <param name="server">Complete server state from Snapcast.</param>
    void UpdateServerState(Server server);

    /// <summary>
    /// Updates a specific client's state.
    /// </summary>
    /// <param name="client">Updated client information.</param>
    void UpdateClient(SnapClient client);

    /// <summary>
    /// Removes a client from the state repository.
    /// </summary>
    /// <param name="clientIndex">Client Index to remove.</param>
    void RemoveClient(string clientIndex);

    /// <summary>
    /// Gets a specific client by ID.
    /// </summary>
    /// <param name="clientIndex">Client Index to retrieve.</param>
    /// <returns>Client information or null if not found.</returns>
    SnapClient? GetClient(string clientIndex);

    /// <summary>
    /// Gets a client by SnapDog2 client index (1-based).
    /// This method handles the mapping between SnapDog2 client index and Snapcast client data.
    /// It will look up the client configuration by index, then find the matching Snapcast client by MAC address.
    /// </summary>
    /// <param name="clientIndex">SnapDog2 client index (1-based).</param>
    /// <returns>Client information or null if not found.</returns>
    SnapClient? GetClientByIndex(int clientIndex);

    /// <summary>
    /// Gets all clients currently known to the repository.
    /// </summary>
    /// <returns>Collection of all clients.</returns>
    IEnumerable<SnapClient> GetAllClients();

    /// <summary>
    /// Updates a specific group's state.
    /// </summary>
    /// <param name="group">Updated group information.</param>
    void UpdateGroup(Group group);

    /// <summary>
    /// Removes a group from the state repository.
    /// </summary>
    /// <param name="groupId">Group ID to remove.</param>
    void RemoveGroup(string groupId);

    /// <summary>
    /// Gets a specific group by ID.
    /// </summary>
    /// <param name="groupId">Group ID to retrieve.</param>
    /// <returns>Group information or null if not found.</returns>
    Group? GetGroup(string groupId);

    /// <summary>
    /// Gets all groups currently known to the repository.
    /// </summary>
    /// <returns>Collection of all groups.</returns>
    IEnumerable<Group> GetAllGroups();

    /// <summary>
    /// Updates a specific stream's state.
    /// </summary>
    /// <param name="stream">Updated stream information.</param>
    void UpdateStream(Stream stream);

    /// <summary>
    /// Removes a stream from the state repository.
    /// </summary>
    /// <param name="streamId">Stream ID to remove.</param>
    void RemoveStream(string streamId);

    /// <summary>
    /// Gets a specific stream by ID.
    /// </summary>
    /// <param name="streamId">Stream ID to retrieve.</param>
    /// <returns>Stream information or null if not found.</returns>
    Stream? GetStream(string streamId);

    /// <summary>
    /// Gets all streams currently known to the repository.
    /// </summary>
    /// <returns>Collection of all streams.</returns>
    IEnumerable<Stream> GetAllStreams();

    /// <summary>
    /// Gets the server information.
    /// </summary>
    /// <returns>Server information.</returns>
    Server GetServerInfo();
}
