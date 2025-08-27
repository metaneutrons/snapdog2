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
namespace SnapDog2.Server.Features.Zones.Commands.Playback;

using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to start playback in a zone. Can play current playlist, specific track, or media URL.
/// </summary>
[CommandId("PLAY")]
[MqttTopic("snapdog/zones/{zoneIndex}/play")]
public record PlayCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the index of the target zone (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the specific track index to play (1-based). If null, plays current or first track.
    /// </summary>
    public int? TrackIndex { get; init; }

    /// <summary>
    /// Gets the media URL to play directly. If provided, overrides playlist/track selection.
    /// </summary>
    public string? MediaUrl { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// Used for tracking command origin and audit purposes.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
