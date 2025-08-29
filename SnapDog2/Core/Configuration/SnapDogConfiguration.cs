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
namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Root configuration class for the SnapDog2 application.
/// Maps all environment variables starting with SNAPDOG_ to nested configuration objects.
/// </summary>
public class SnapDogConfiguration
{
    /// <summary>
    /// Basic system configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_SYSTEM_*
    /// </summary>
    [Env(NestedPrefix = "SYSTEM_")]
    public SystemConfig System { get; set; } = new();

    /// <summary>
    /// Telemetry and observability configuration.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_*
    /// </summary>
    [Env(NestedPrefix = "TELEMETRY_")]
    public TelemetryConfig Telemetry { get; set; } = new();

    /// <summary>
    /// HTTP server and authentication configuration.
    /// Maps environment variables with prefix: SNAPDOG_HTTP_*
    /// </summary>
    [Env(NestedPrefix = "HTTP_")]
    public HttpConfig Http { get; set; } = new();

    /// <summary>
    /// Redis persistent state storage configuration.
    /// Maps environment variables with prefix: SNAPDOG_REDIS_*
    /// </summary>
    [Env(NestedPrefix = "REDIS_")]
    public RedisConfig Redis { get; set; } = new();

    /// <summary>
    /// External services configuration (Snapcast, MQTT, KNX, ServicesSubsonic).
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_*
    /// </summary>
    [Env(NestedPrefix = "SERVICES_")]
    public ServicesConfig Services { get; set; } = new();

    /// <summary>
    /// Snapcast server configuration (for container setup).
    /// Maps environment variables with prefix: SNAPDOG_SNAPCAST_*
    /// </summary>
    [Env(NestedPrefix = "SNAPCAST_")]
    public SnapcastServerConfig SnapcastServer { get; set; } = new();

    /// <summary>
    /// List of audio zone configurations.
    /// Maps environment variables with pattern: SNAPDOG_ZONE_X_*
    /// Where X is the zone index (1, 2, 3, etc.)
    /// </summary>
    [Env(NestedListPrefix = "ZONE_", NestedListSuffix = "_")]
    public List<ZoneConfig> Zones { get; set; } = [];

    /// <summary>
    /// List of client device configurations.
    /// Maps environment variables with pattern: SNAPDOG_CLIENT_X_*
    /// Where X is the client index (1, 2, 3, etc.)
    /// </summary>
    [Env(NestedListPrefix = "CLIENT_", NestedListSuffix = "_")]
    public List<ClientConfig> Clients { get; set; } = [];

    /// <summary>
    /// List of radio station configurations.
    /// Maps environment variables with pattern: SNAPDOG_RADIO_X_*
    /// Where X is the radio station index (1, 2, 3, etc.)
    /// </summary>
    [Env(NestedListPrefix = "RADIO_", NestedListSuffix = "_")]
    public List<RadioStationConfig> RadioStations { get; set; } = [];

    /// <summary>
    /// Base MQTT topic prefix. Convenience property that maps to Services.Mqtt.MqttBaseTopic.
    /// </summary>
    public string MqttBaseTopic => Services.Mqtt.MqttBaseTopic;
}
