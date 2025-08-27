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
namespace SnapDog2.Server.Features.Clients.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to decrease a client's volume by a specified step.
/// </summary>
[CommandId("CLIENT_VOLUME_DOWN")]
[MqttTopic("snapdog/clients/{clientIndex}/volume/down")]
public record ClientVolumeDownCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the client index (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the volume step to decrease by (default: 5).
    /// </summary>
    public int Step { get; init; } = 5;

    /// <summary>
    /// Gets the command source.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
