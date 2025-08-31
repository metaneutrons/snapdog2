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
/// Service for managing audio zones.
/// </summary>
public interface IZoneManager
{
    /// <summary>
    /// Gets a zone by its ID.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <returns>The zone service if found.</returns>
    Task<Result<IZoneService>> GetZoneAsync(int zoneIndex);

    /// <summary>
    /// Gets a zone by its ID.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The zone state if found.</returns>
    Task<Result<ZoneState>> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available zones.
    /// </summary>
    /// <returns>Collection of all zones.</returns>
    Task<Result<IEnumerable<IZoneService>>> GetAllZonesAsync();

    /// <summary>
    /// Gets all available zones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of all zone states.</returns>
    Task<Result<List<ZoneState>>> GetAllZonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of a specific zone.
    /// </summary>
    /// <param name="zoneIndex">The zone ID.</param>
    /// <returns>The zone state if found.</returns>
    Task<Result<ZoneState>> GetZoneStateAsync(int zoneIndex);

    /// <summary>
    /// Gets the states of all zones.
    /// </summary>
    /// <returns>Collection of all zone states.</returns>
    Task<Result<List<ZoneState>>> GetAllZoneStatesAsync();

    /// <summary>
    /// Checks if a zone exists.
    /// </summary>
    /// <param name="zoneIndex">The zone ID to check.</param>
    /// <returns>True if the zone exists.</returns>
    Task<bool> ZoneExistsAsync(int zoneIndex);
}
