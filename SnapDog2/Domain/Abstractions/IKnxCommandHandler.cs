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
/// Interface for handling KNX commands.
/// </summary>
public interface IKnxCommandHandler
{
    /// <summary>
    /// Handles a KNX command for a specific zone.
    /// </summary>
    /// <param name="commandId">The command ID to handle.</param>
    /// <param name="zoneIndex">The zone index.</param>
    /// <param name="parameters">Optional command parameters.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> HandleCommandAsync(string commandId, int zoneIndex, object? parameters = null);
}
