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
/// Interface for persisting zone states across requests.
/// </summary>
public interface IZoneStateStore
{
    /// <summary>
    /// Gets the current state for a zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Zone state or null if not found</returns>
    ZoneState? GetZoneState(int zoneIndex);

    /// <summary>
    /// Sets the current state for a zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="state">Zone state to store</param>
    void SetZoneState(int zoneIndex, ZoneState state);

    /// <summary>
    /// Gets all zone states.
    /// </summary>
    /// <returns>Dictionary of zone states by zone index</returns>
    Dictionary<int, ZoneState> GetAllZoneStates();

    /// <summary>
    /// Initializes default state for a zone if it doesn't exist.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="defaultState">Default state to use</param>
    void InitializeZoneState(int zoneIndex, ZoneState defaultState);
}
