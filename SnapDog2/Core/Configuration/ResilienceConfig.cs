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
/// Resilience configuration for Polly policies.
/// Provides configurable retry and timeout settings for connection and operation policies.
/// </summary>
public class ResilienceConfig
{
    /// <summary>
    /// Connection policy configuration for establishing connections.
    /// Maps environment variables with suffix: *_CONNECTION_*
    /// </summary>
    [Env(NestedPrefix = "CONNECTION_")]
    public PolicyConfig Connection { get; set; } = new();

    /// <summary>
    /// Operation policy configuration for individual operations.
    /// Maps environment variables with suffix: *_OPERATION_*
    /// </summary>
    [Env(NestedPrefix = "OPERATION_")]
    public PolicyConfig Operation { get; set; } = new();
}

/// <summary>
/// Individual policy configuration with retry and timeout settings.
/// </summary>
public class PolicyConfig
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// Maps to: *_MAX_RETRIES
    /// </summary>
    [Env(Key = "MAX_RETRIES", Default = 3)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds.
    /// Maps to: *_RETRY_DELAY_MS
    /// </summary>
    [Env(Key = "RETRY_DELAY_MS", Default = 1000)]
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Backoff type for retry delays.
    /// Options: Linear, Exponential, Constant
    /// Maps to: *_BACKOFF_TYPE
    /// </summary>
    [Env(Key = "BACKOFF_TYPE", Default = "Exponential")]
    public string BackoffType { get; set; } = "Exponential";

    /// <summary>
    /// Whether to use jitter in retry delays to avoid thundering herd.
    /// Maps to: *_USE_JITTER
    /// </summary>
    [Env(Key = "USE_JITTER", Default = true)]
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Timeout in seconds for individual operations.
    /// Maps to: *_TIMEOUT_SECONDS
    /// </summary>
    [Env(Key = "TIMEOUT_SECONDS", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum jitter percentage (0-100) when UseJitter is true.
    /// Maps to: *_JITTER_PERCENTAGE
    /// </summary>
    [Env(Key = "JITTER_PERCENTAGE", Default = 25)]
    public int JitterPercentage { get; set; } = 25;
}
