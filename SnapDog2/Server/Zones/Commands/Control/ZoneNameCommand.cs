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
namespace SnapDog2.Server.Zone.Commands.Control;

using Cortex.Mediator.Commands;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Command to set the name of a zone.
/// </summary>
[CommandId("ZONE_NAME")]
[MqttTopic("snapdog/zones/{zoneIndex}/name/set")]
public record ZoneNameCommand(int ZoneIndex, string Name, CommandSource CommandSource = CommandSource.Api) : ICommand<Result>;
