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
using Cortex.Mediator.Commands;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

namespace SnapDog2.Core.Commands;

/// <summary>
/// Command to set zone name.
/// </summary>
[CommandId("ZONE_NAME")]
public class ZoneNameCommand : ICommand<Result>
{
    /// <summary>
    /// Gets or sets the zone index.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Gets or sets the zone name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
