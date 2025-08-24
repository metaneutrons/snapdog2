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
namespace SnapDog2.Server.Features.Zones.Commands.Track;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to seek to a specific position in the current track.
/// </summary>
[CommandId("TRACK_POSITION")]
[MqttTopic("snapdog/zone/{zoneIndex}/track/position/set")]
public record SeekPositionCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the position to seek to in milliseconds.
    /// </summary>
    public required long PositionMs { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
