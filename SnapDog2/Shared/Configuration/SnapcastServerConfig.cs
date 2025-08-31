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
/// Snapcast server configuration (for container setup).
/// Maps environment variables with prefix: SNAPDOG_SNAPCAST_*
/// </summary>
public class SnapcastServerConfig
{
    /// <summary>
    /// SnapWeb HTTP port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_HTTP_PORT
    /// </summary>
    [Env(Key = "WEBSERVER_PORT", Default = 1780)]
    public int WebServerPort { get; set; } = 1780;

    /// <summary>
    /// JSON-RPC WebSocket port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_WEBSOCKET_PORT
    /// </summary>
    [Env(Key = "WEBSOCKET_PORT", Default = 1704)]
    public int WebSocketPort { get; set; } = 1704;

    /// <summary>
    /// JSON-RPC API port.
    /// Maps to: SNAPDOG_SNAPCAST_JSONRPC_PORT
    /// </summary>
    [Env(Key = "JSONRPC_PORT", Default = 1705)]
    public int JsonRpcPort { get; set; } = 1705;
}
