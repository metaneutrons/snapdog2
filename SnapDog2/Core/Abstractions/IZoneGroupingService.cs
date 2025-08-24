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
using SnapDog2.Core.Models;

namespace SnapDog2.Core.Abstractions;

/// <summary>
/// Service responsible for managing Snapcast client grouping based on zone assignments.
/// Ensures clients assigned to the same zone are grouped together in Snapcast for synchronized audio playback.
/// </summary>
public interface IZoneGroupingService
{
    /// <summary>
    /// Ensures all zones are properly configured with correct client groupings and streams.
    /// This is the main method for periodic zone grouping checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the zone grouping operation</returns>
    Task<Result> EnsureZoneGroupingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures all clients assigned to a specific zone are grouped together in Snapcast.
    /// </summary>
    /// <param name="zoneId">The zone ID to synchronize grouping for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the grouping operation</returns>
    Task<Result> SynchronizeZoneGroupingAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a specific client is placed in the correct Snapcast group for its assigned zone.
    /// </summary>
    /// <param name="clientId">The client ID to group</param>
    /// <param name="zoneId">The zone ID the client should be grouped with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of the client grouping</returns>
    Task<Result> EnsureClientInZoneGroupAsync(int clientId, int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the current Snapcast grouping matches the logical zone assignments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating whether grouping is consistent</returns>
    Task<Result> ValidateGroupingConsistencyAsync(CancellationToken cancellationToken = default);
}
