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
/// API server and authentication configuration.
/// </summary>
public class HttpConfig
{
    /// <summary>
    /// Port number for the HTTP server (API and WebUI).
    /// Maps to: SNAPDOG_HTTP_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 5555)]
    public int HttpPort { get; set; } = 5555;

    /// <summary>
    /// Whether the API server is enabled.
    /// Maps to: SNAPDOG_HTTP_API_ENABLED
    /// </summary>
    [Env(Key = "API_ENABLED", Default = true)]
    public bool ApiEnabled { get; set; } = true;

    /// <summary>
    /// Whether API authentication is enabled.
    /// Maps to: SNAPDOG_HTTP_API_AUTH_ENABLED
    /// </summary>
    [Env(Key = "API_AUTH_ENABLED", Default = true)]
    public bool ApiAuthEnabled { get; set; } = true;

    /// <summary>
    /// List of API keys for authentication.
    /// Maps environment variables with pattern: SNAPDOG_HTTP_API_APIKEY_X
    /// Where X is the key index (1, 2, 3, etc.)
    /// </summary>
    [Env(ListPrefix = "API_APIKEY_")]
    public List<string> ApiKeys { get; set; } = [];

    /// <summary>
    /// Whether WebUI authentication is enabled.
    /// Maps to: SNAPDOG_HTTP_WEBUI_ENABLED
    /// </summary>
    [Env(Key = "WEBUI_ENABLED", Default = true)]
    public bool WebUiEnabled { get; set; } = true;

    /// <summary>
    /// WebUI path for reverse proxy.
    /// Maps environment variables with pattern: SNAPDOG_HTTP_WEBUI_PATH
    /// </summary>
    [Env(Key = "WEBUI_PATH", Default = "/")]
    public string WebUiPath { get; set; } = "/";
}
