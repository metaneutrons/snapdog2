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
/// Represents the source of a command.
/// </summary>
public enum CommandSource
{
    /// <summary>
    /// Command originated internally within the application.
    /// </summary>
    Internal,

    /// <summary>
    /// Command originated from the REST API.
    /// </summary>
    Api,

    /// <summary>
    /// Command originated from MQTT.
    /// </summary>
    Mqtt,

    /// <summary>
    /// Command originated from KNX.
    /// </summary>
    Knx,

    /// <summary>
    /// Command originated from WebSocket connection.
    /// </summary>
    WebSocket,
}
