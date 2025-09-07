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
using SnapDog2.Shared.Models;

/// <summary>
/// In-memory implementation of zone state storage.
/// Thread-safe and persists state across HTTP requests.
/// </summary>
public class InMemoryZoneStateStore : IZoneStateStore
{
    private readonly ConcurrentDictionary<int, ZoneState> _zoneStates = new();

    /// <summary>
    /// Event raised when zone state changes.
    /// </summary>
    public event Action<int, ZoneState>? ZoneStateChanged;

    public ZoneState? GetZoneState(int zoneIndex)
    {
        return this._zoneStates.TryGetValue(zoneIndex, out var state) ? state : null;
    }

    public void SetZoneState(int zoneIndex, ZoneState state)
    {
        this._zoneStates.AddOrUpdate(zoneIndex, state, (_, _) => state);

        // Raise event to notify subscribers of state change
        this.ZoneStateChanged?.Invoke(zoneIndex, state);
    }

    public Dictionary<int, ZoneState> GetAllZoneStates()
    {
        return this._zoneStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void InitializeZoneState(int zoneIndex, ZoneState defaultState)
    {
        this._zoneStates.TryAdd(zoneIndex, defaultState);
    }
}
