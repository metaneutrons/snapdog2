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
namespace SnapDog2.Server.Features.Zones.Commands.Volume;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the volume for a specific zone. Sets the absolute volume level for all clients in the zone.
/// </summary>
[CommandId("VOLUME")]
[MqttTopic("snapdog/zone/{zoneIndex}/volume/set")]
public record SetZoneVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the desired volume level (0-100). 0 = muted, 100 = maximum volume. Values outside this range will be clamped.
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
