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
/// Snapcast service configuration.
/// </summary>
public class SnapcastConfig
{
    /// <summary>
    /// Snapcast server address.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_ADDRESS
    /// </summary>
    [Env(Key = "ADDRESS", Default = "localhost")]
    public string Address { get; set; } = "localhost";

    /// <summary>
    /// Snapcast server port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT
    /// </summary>
    [Env(Key = "JSONRPC_PORT", Default = 1705)]
    public int JsonRpcPort { get; set; } = 1705;

    /// <summary>
    /// Connection timeout in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 30)]
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// Resilience policy configuration for Snapcast operations.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SNAPCAST_RESILIENCE_*
    /// </summary>
    [Env(NestedPrefix = "RESILIENCE_")]
    public ResilienceConfig Resilience { get; set; } =
        new()
        {
            Connection = new PolicyConfig
            {
                MaxRetries = 3,
                RetryDelayMs = 2000,
                BackoffType = "Exponential",
                UseJitter = true,
                TimeoutSeconds = 30,
            },
            Operation = new PolicyConfig
            {
                MaxRetries = 2,
                RetryDelayMs = 500,
                BackoffType = "Linear",
                UseJitter = false,
                TimeoutSeconds = 10,
            },
        };

    /// <summary>
    /// Reconnect interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_RECONNECT_INTERVAL
    /// </summary>
    [Env(Key = "RECONNECT_INTERVAL", Default = 5)]
    public int ReconnectInterval { get; set; } = 5;

    /// <summary>
    /// Whether auto-reconnect is enabled.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_AUTO_RECONNECT
    /// </summary>
    [Env(Key = "AUTO_RECONNECT", Default = true)]
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in milliseconds between periodic zone grouping checks.
    /// This is the main interval for ensuring zones are properly configured.
    /// Maps to: SNAPDOG_SERVICES_ZONE_GROUPING_CHECK_INTERVAL_MS
    /// Default: 5000ms (5 seconds).
    /// </summary>
    [Env("ZONE_GROUPING_CHECK_INTERVAL_MS")]
    public int ZoneGroupingCheckIntervalMs { get; set; } = 5_000;

    /// <summary>
    /// Snapcast HTTP port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_HTTP_PORT
    /// </summary>
    [Env(Key = "HTTP_PORT", Default = 1780)]
    public int HttpPort { get; set; } = 1780;

    /// <summary>
    /// Snapcast base URL for reverse proxy support.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_BASE_URL
    /// </summary>
    [Env(Key = "BASE_URL", Default = "")]
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// Snapcast WebSocket URL for JSON-RPC communication.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_WEBSOCKET_URL
    /// </summary>
    [Env(Key = "WEBSOCKET_URL", Default = "ws://localhost:1780/jsonrpc")]
    public string WebSocketUrl { get; set; } = "ws://localhost:1780/jsonrpc";
}
