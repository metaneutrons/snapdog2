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
namespace SnapDog2.Shared.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for an individual audio zone.
/// Maps environment variables like SNAPDOG_ZONE_X_* to properties.
/// </summary>
public class ZoneConfig
{
    /// <summary>
    /// Display name of the zone.
    /// Maps to: SNAPDOG_ZONE_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Snapcast sink path for this zone.
    /// Maps to: SNAPDOG_ZONE_X_SINK
    /// </summary>
    [Env(Key = "SINK")]
    [Required]
    public string Sink { get; set; } = null!;

    /// <summary>
    /// Optional UTF-8 icon character for the zone (single character).
    /// Maps to: SNAPDOG_ZONE_X_ICON
    /// </summary>
    [Env(Key = "ICON", Default = "🎵")]
    public string Icon { get; set; } = "🎵";

    /// <summary>
    /// KNX configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ZoneKnxConfig Knx { get; set; } = new();
}
