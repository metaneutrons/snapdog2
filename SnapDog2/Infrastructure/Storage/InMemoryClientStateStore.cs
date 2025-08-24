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
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

/// <summary>
/// In-memory implementation of client state storage.
/// Thread-safe and persists state across HTTP requests.
/// </summary>
public class InMemoryClientStateStore : IClientStateStore
{
    private readonly ConcurrentDictionary<int, ClientState> _clientStates = new();

    public ClientState? GetClientState(int clientIndex)
    {
        return this._clientStates.TryGetValue(clientIndex, out var state) ? state : null;
    }

    public void SetClientState(int clientIndex, ClientState state)
    {
        this._clientStates.AddOrUpdate(clientIndex, state, (_, _) => state);
    }

    public Dictionary<int, ClientState> GetAllClientStates()
    {
        return this._clientStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void InitializeClientState(int clientIndex, ClientState defaultState)
    {
        this._clientStates.TryAdd(clientIndex, defaultState);
    }
}
