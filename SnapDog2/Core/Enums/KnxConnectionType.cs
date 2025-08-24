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
namespace SnapDog2.Core.Enums;

/// <summary>
/// Defines the available KNX connection types supported by Knx.Falcon.Sdk.
/// </summary>
public enum KnxConnectionType
{
    /// <summary>
    /// IP Tunneling connection - connects to KNX/IP gateway via UDP tunneling.
    /// Most common connection type for KNX installations.
    /// Uses IpTunnelingConnectorParameters.
    /// </summary>
    Tunnel,

    /// <summary>
    /// IP Routing connection - connects to KNX/IP router via UDP multicast.
    /// Used for direct access to KNX backbone without gateway.
    /// Uses IpRoutingConnectorParameters.
    /// </summary>
    Router,

    /// <summary>
    /// USB connection - connects directly to KNX USB interface.
    /// Used for direct hardware connection to KNX bus.
    /// Uses UsbConnectorParameters.
    /// </summary>
    Usb,
}
