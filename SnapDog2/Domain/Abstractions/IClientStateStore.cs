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
using SnapDog2.Shared.Events;
using SnapDog2.Shared.Models;

/// <summary>
/// Interface for persisting client states across requests.
/// </summary>
public interface IClientStateStore
{
    /// <summary>
    /// Event raised when client state changes.
    /// </summary>
    [StatusId("CLIENT_STATE_CHANGED")]
    event EventHandler<ClientStateChangedEventArgs>? ClientStateChanged;

    /// <summary>
    /// Event raised when client volume changes.
    /// </summary>
    [StatusId("CLIENT_VOLUME_STATUS")]
    event EventHandler<ClientVolumeChangedEventArgs>? ClientVolumeChanged;

    /// <summary>
    /// Event raised when client connection changes.
    /// </summary>
    [StatusId("CLIENT_CONNECTED")]
    event EventHandler<ClientConnectionChangedEventArgs>? ClientConnectionChanged;

    /// <summary>
    /// Gets the current state for a client.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based)</param>
    /// <returns>Client state or null if not found</returns>
    ClientState? GetClientState(int clientIndex);

    /// <summary>
    /// Sets the current state for a client.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based)</param>
    /// <param name="state">Client state to store</param>
    void SetClientState(int clientIndex, ClientState state);

    /// <summary>
    /// Gets all client states.
    /// </summary>
    /// <returns>Dictionary of client states by client index</returns>
    Dictionary<int, ClientState> GetAllClientStates();

    /// <summary>
    /// Initializes default state for a client if it doesn't exist.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based)</param>
    /// <param name="defaultState">Default state to use</param>
    void InitializeClientState(int clientIndex, ClientState defaultState);
}
