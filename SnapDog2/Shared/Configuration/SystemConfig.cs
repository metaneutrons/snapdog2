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
/// Basic system configuration settings.
/// </summary>
public class SystemConfig
{
    /// <summary>
    /// Logging level for the application.
    /// Maps to: SNAPDOG_SYSTEM_LOG_LEVEL
    /// </summary>
    [Env(Key = "LOG_LEVEL", Default = "Information")]
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Application environment (Development, Staging, Production).
    /// Maps to: SNAPDOG_SYSTEM_ENVIRONMENT
    /// </summary>
    [Env(Key = "ENVIRONMENT", Default = "Development")]
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Whether health checks are enabled.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_ENABLED", Default = true)]
    public bool HealthChecksEnabled { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TIMEOUT
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TIMEOUT", Default = 30)]
    public int HealthChecksTimeout { get; set; } = 30;

    /// <summary>
    /// Health check tags.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TAGS
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TAGS", Default = "ready,live")]
    public string HealthChecksTags { get; set; } = "ready,live";

    /// <summary>
    /// Optional log file path. If not set, file logging is disabled.
    /// Maps to: SNAPDOG_SYSTEM_LOG_FILE
    /// </summary>
    [Env(Key = "LOG_FILE")]
    public string? LogFile { get; set; }
}
