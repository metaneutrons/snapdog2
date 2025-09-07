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
namespace SnapDog2.Infrastructure.Storage;

using System.Collections.Concurrent;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Events;
using SnapDog2.Shared.Models;

/// <summary>
/// In-memory implementation of client state storage.
/// Thread-safe and persists state across HTTP requests.
/// </summary>
public class InMemoryClientStateStore : IClientStateStore
{
    private readonly ConcurrentDictionary<int, ClientState> _clientStates = new();

    /// <summary>
    /// Event raised when client state changes.
    /// </summary>
    public event EventHandler<ClientStateChangedEventArgs>? ClientStateChanged;

    /// <summary>
    /// Event raised when client volume changes.
    /// </summary>
    public event EventHandler<ClientVolumeChangedEventArgs>? ClientVolumeChanged;

    /// <summary>
    /// Event raised when client connection changes.
    /// </summary>
    public event EventHandler<ClientConnectionChangedEventArgs>? ClientConnectionChanged;

    public ClientState? GetClientState(int clientIndex)
    {
        return this._clientStates.TryGetValue(clientIndex, out var state) ? state : null;
    }

    public void SetClientState(int clientIndex, ClientState newState)
    {
        var oldState = GetClientState(clientIndex);
        this._clientStates.AddOrUpdate(clientIndex, newState, (_, _) => newState);

        // Detect and publish specific changes
        DetectAndPublishChanges(clientIndex, oldState, newState);

        // Always fire general state change
        ClientStateChanged?.Invoke(this, new ClientStateChangedEventArgs(clientIndex, oldState, newState));
    }

    public Dictionary<int, ClientState> GetAllClientStates()
    {
        return this._clientStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void InitializeClientState(int clientIndex, ClientState defaultState)
    {
        this._clientStates.TryAdd(clientIndex, defaultState);
    }

    private void DetectAndPublishChanges(int clientIndex, ClientState? oldState, ClientState newState)
    {
        // Volume changes
        if (oldState?.Volume != newState.Volume)
        {
            ClientVolumeChanged?.Invoke(this, new ClientVolumeChangedEventArgs(
                clientIndex, oldState?.Volume ?? 0, newState.Volume));
        }

        // Connection changes
        if (oldState?.Connected != newState.Connected)
        {
            ClientConnectionChanged?.Invoke(this, new ClientConnectionChangedEventArgs(
                clientIndex, oldState?.Connected ?? false, newState.Connected));
        }
    }
}
