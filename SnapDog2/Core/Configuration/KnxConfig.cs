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
using SnapDog2.Core.Enums;

/// <summary>
/// KNX service configuration.
/// </summary>
public class KnxConfig
{
    /// <summary>
    /// Whether KNX integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// KNX connection type (tunnel, router, usb).
    /// Maps to: SNAPDOG_SERVICES_KNX_CONNECTION_TYPE
    /// </summary>
    [Env(Key = "CONNECTION_TYPE", Default = KnxConnectionType.Router)]
    public KnxConnectionType ConnectionType { get; set; } = KnxConnectionType.Router;

    /// <summary>
    /// KNX gateway address (required for tunnel connections only).
    /// Maps to: SNAPDOG_SERVICES_KNX_GATEWAY
    /// </summary>
    [Env(Key = "GATEWAY")]
    public string? Gateway { get; set; }

    /// <summary>
    /// KNX multicast address for router connections (default: 224.0.23.12).
    /// Maps to: SNAPDOG_SERVICES_KNX_MULTICAST_ADDRESS
    /// </summary>
    [Env(Key = "MULTICAST_ADDRESS", Default = "224.0.23.12")]
    public string MulticastAddress { get; set; } = "224.0.23.12";

    /// <summary>
    /// KNX USB device identifier for USB connections.
    /// Maps to: SNAPDOG_SERVICES_KNX_USB_DEVICE
    /// </summary>
    [Env(Key = "USB_DEVICE")]
    public string? UsbDevice { get; set; }

    /// <summary>
    /// KNX port number (default: 3671).
    /// Maps to: SNAPDOG_SERVICES_KNX_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 3671)]
    public int Port { get; set; } = 3671;

    /// <summary>
    /// KNX connection timeout in seconds.
    /// Maps to: SNAPDOG_SERVICES_KNX_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 10)]
    public int Timeout { get; set; } = 10;

    /// <summary>
    /// Whether auto-reconnect is enabled.
    /// Maps to: SNAPDOG_SERVICES_KNX_AUTO_RECONNECT
    /// </summary>
    [Env(Key = "AUTO_RECONNECT", Default = true)]
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Resilience policy configuration for KNX operations.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_KNX_RESILIENCE_*
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
                TimeoutSeconds = 10, // Use KNX timeout default
            },
            Operation = new PolicyConfig
            {
                MaxRetries = 2,
                RetryDelayMs = 500,
                BackoffType = "Linear",
                UseJitter = false,
                TimeoutSeconds = 5,
            },
        };
}
