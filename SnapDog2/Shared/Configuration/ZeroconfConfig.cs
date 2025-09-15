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

using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for Zeroconf/Bonjour service advertisement.
/// Maps to: SNAPDOG_ZEROCONF_*
/// </summary>
public class ZeroconfConfig
{
    /// <summary>
    /// Enable/disable Zeroconf service advertisement.
    /// Maps to: SNAPDOG_ZEROCONF_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = "true")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Advertise the main API service.
    /// Maps to: SNAPDOG_ZEROCONF_ADVERTISE_API
    /// </summary>
    [Env(Key = "ADVERTISE_API", Default = "true")]
    public bool AdvertiseApi { get; set; } = true;

    /// <summary>
    /// Advertise the WebUI service.
    /// Maps to: SNAPDOG_ZEROCONF_ADVERTISE_WEBUI
    /// </summary>
    [Env(Key = "ADVERTISE_WEBUI", Default = "true")]
    public bool AdvertiseWebUI { get; set; } = true;

    /// <summary>
    /// Custom instance name for service advertisement.
    /// If null, auto-generates from hostname.
    /// Maps to: SNAPDOG_ZEROCONF_INSTANCE_NAME
    /// </summary>
    [Env(Key = "INSTANCE_NAME")]
    public string? InstanceName { get; set; }

    /// <summary>
    /// Optional friendly location name for TXT records.
    /// Maps to: SNAPDOG_ZEROCONF_LOCATION
    /// </summary>
    [Env(Key = "LOCATION")]
    public string? Location { get; set; }
}
