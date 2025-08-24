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
namespace SnapDog2.Server.Features.Clients.Commands.Config;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set client latency compensation. Adjusts the audio latency for an individual Snapcast client.
/// </summary>
[CommandId("CLIENT_LATENCY")]
[MqttTopic("snapdog/client/{clientIndex}/latency/set")]
public record SetClientLatencyCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target client (1-based).
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the latency compensation in milliseconds. Positive values delay audio, negative values advance it.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
