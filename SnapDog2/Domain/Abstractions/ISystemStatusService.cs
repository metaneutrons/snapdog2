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
/// Service for managing system status information.
/// </summary>
public interface IAppStatusService
{
    /// <summary>
    /// Gets the current system status.
    /// </summary>
    /// <returns>The current system status.</returns>
    Task<SystemStatus> GetCurrentStatusAsync();

    /// <summary>
    /// Gets the current version information.
    /// </summary>
    /// <returns>The version details.</returns>
    Task<VersionDetails> GetVersionInfoAsync();

    /// <summary>
    /// Gets the current server performance statistics.
    /// </summary>
    /// <returns>The server statistics.</returns>
    Task<ServerStats> GetServerStatsAsync();
}
