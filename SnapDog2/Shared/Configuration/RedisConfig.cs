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
using EnvoyConfig.Attributes;

namespace SnapDog2.Shared.Configuration;

/// <summary>
/// Configuration for Redis persistent state storage.
/// </summary>
public class RedisConfig
{
    /// <summary>
    /// Gets or sets whether Redis is enabled for persistent state storage.
    /// </summary>
    [Env("ENABLED")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    [Env("CONNECTION_STRING")]
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the Redis database number to use.
    /// </summary>
    [Env("DATABASE")]
    public int Database { get; set; } = 0;

    /// <summary>
    /// Gets or sets the key prefix for all Redis keys.
    /// </summary>
    [Env("KEY_PREFIX")]
    public string KeyPrefix { get; set; } = "snapdog";

    /// <summary>
    /// Gets or sets the timeout for Redis operations in seconds.
    /// </summary>
    [Env("TIMEOUT_SECONDS")]
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Resilience configuration for Redis operations.
    /// Maps environment variables with prefix: SNAPDOG_REDIS_RESILIENCE_*
    /// </summary>
    [Env(NestedPrefix = "RESILIENCE_")]
    public ResilienceConfig Resilience { get; set; } = new();
}
