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
/// External services configuration.
/// </summary>
public class ServicesConfig
{
    /// <summary>
    /// Snapcast integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SNAPCAST_*
    /// </summary>
    [Env(NestedPrefix = "SNAPCAST_")]
    public SnapcastConfig Snapcast { get; set; } = new();

    /// <summary>
    /// MQTT integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public MqttConfig Mqtt { get; set; } = new();

    /// <summary>
    /// KNX integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public KnxConfig Knx { get; set; } = new();

    /// <summary>
    /// Subsonic integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SUBSONIC_*
    /// </summary>
    [Env(NestedPrefix = "SUBSONIC_")]
    public SubsonicConfig Subsonic { get; set; } = new();

    /// <summary>
    /// Position update debouncing interval in milliseconds.
    /// Maps environment variable: SNAPDOG_SERVICES_DEBOUNCING_MS
    /// </summary>
    [Env("DEBOUNCING_MS")]
    public int DebouncingMs { get; set; } = 500;

    /// <summary>
    /// Global audio configuration for Snapcast and LibVLC.
    /// Maps environment variables with prefix: SNAPDOG_AUDIO_*
    /// </summary>
    [Env(NestedPrefix = "AUDIO_")]
    public AudioConfig Audio { get; set; } = new();
}
