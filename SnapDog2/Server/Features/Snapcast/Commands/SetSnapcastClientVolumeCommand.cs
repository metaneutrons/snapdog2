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
namespace SnapDog2.Server.Features.Snapcast.Commands;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the volume of a Snapcast client.
/// </summary>
public record SetSnapcastClientVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// The Snapcast client Index.
    /// </summary>
    public required string ClientIndex { get; init; }

    /// <summary>
    /// The volume percentage (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// The source of the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Api;
}
