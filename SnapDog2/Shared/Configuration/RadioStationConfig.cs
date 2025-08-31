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
/// Configuration for a radio station.
/// Maps environment variables like SNAPDOG_RADIO_X_* to properties.
/// </summary>
public class RadioStationConfig
{
    /// <summary>
    /// Display name of the radio station.
    /// Maps to: SNAPDOG_RADIO_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Stream URL of the radio station.
    /// Maps to: SNAPDOG_RADIO_X_URL
    /// </summary>
    [Env(Key = "URL")]
    [Required]
    [Url]
    public string Url { get; set; } = null!;
}
