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
/// Configuration for an individual client device.
/// Maps environment variables like SNAPDOG_CLIENT_X_* to properties.
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// Display name of the client.
    /// Maps to: SNAPDOG_CLIENT_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// MAC address of the client device.
    /// Maps to: SNAPDOG_CLIENT_X_MAC
    /// </summary>
    [Env(Key = "MAC")]
    [RegularExpression(
        @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
        ErrorMessage = "MAC address must be in format XX:XX:XX:XX:XX:XX"
    )]
    public string? Mac { get; set; }

    /// <summary>
    /// Default zone ID for this client (1-based).
    /// Maps to: SNAPDOG_CLIENT_X_DEFAULT_ZONE
    /// </summary>
    [Env(Key = "DEFAULT_ZONE", Default = 1)]
    [Range(1, 100)]
    public int DefaultZone { get; set; } = 1;

    /// <summary>
    /// Optional UTF-8 icon character for the client (single character).
    /// Maps to: SNAPDOG_CLIENT_X_ICON
    /// </summary>
    [Env(Key = "ICON", Default = "🎵")]
    public string Icon { get; set; } = "🎵";

    /// <summary>
    /// KNX configuration for this client.
    /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ClientKnxConfig Knx { get; set; } = new();
}
