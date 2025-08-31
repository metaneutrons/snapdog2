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
namespace SnapDog2.Infrastructure.Resilience;

using System;
using Polly;
using Polly.Retry;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Factory for creating Polly resilience pipelines from configuration.
/// </summary>
public static class ResiliencePolicyFactory
{
    /// <summary>
    /// Creates a resilience pipeline from policy configuration.
    /// </summary>
    /// <param name="config">The policy configuration.</param>
    /// <param name="serviceName">Service name for logging context.</param>
    /// <returns>Configured resilience pipeline.</returns>
    public static ResiliencePipeline CreatePipeline(PolicyConfig config, string serviceName = "Unknown")
    {
        var builder = new ResiliencePipelineBuilder();

        // Add retry policy
        if (config.MaxRetries > 0)
        {
            builder.AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = config.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(config.RetryDelayMs),
                    BackoffType = ParseBackoffType(config.BackoffType),
                    UseJitter = config.UseJitter,
                    OnRetry = args =>
                    {
                        // Optional: Add logging here if needed
                        return ValueTask.CompletedTask;
                    },
                }
            );
        }

        // Add timeout policy
        if (config.TimeoutSeconds > 0)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds));
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates connection resilience pipeline from resilience configuration.
    /// </summary>
    /// <param name="resilience">The resilience configuration.</param>
    /// <param name="serviceName">Service name for logging context.</param>
    /// <returns>Configured connection resilience pipeline.</returns>
    public static ResiliencePipeline CreateConnectionPipeline(
        ResilienceConfig resilience,
        string serviceName = "Unknown"
    )
    {
        return CreatePipeline(resilience.Connection, $"{serviceName}-Connection");
    }

    /// <summary>
    /// Creates operation resilience pipeline from resilience configuration.
    /// </summary>
    /// <param name="resilience">The resilience configuration.</param>
    /// <param name="serviceName">Service name for logging context.</param>
    /// <returns>Configured operation resilience pipeline.</returns>
    public static ResiliencePipeline CreateOperationPipeline(
        ResilienceConfig resilience,
        string serviceName = "Unknown"
    )
    {
        return CreatePipeline(resilience.Operation, $"{serviceName}-Operation");
    }

    /// <summary>
    /// Parses backoff type string to DelayBackoffType enum.
    /// </summary>
    /// <param name="backoffType">The backoff type string.</param>
    /// <returns>Corresponding DelayBackoffType.</returns>
    private static DelayBackoffType ParseBackoffType(string backoffType)
    {
        return backoffType?.ToLowerInvariant() switch
        {
            "linear" => DelayBackoffType.Linear,
            "exponential" => DelayBackoffType.Exponential,
            "constant" => DelayBackoffType.Constant,
            _ => DelayBackoffType.Exponential, // Default to exponential
        };
    }

    /// <summary>
    /// Validates policy configuration and provides defaults for invalid values.
    /// </summary>
    /// <param name="config">The policy configuration to validate.</param>
    /// <returns>Validated policy configuration.</returns>
    public static PolicyConfig ValidateAndNormalize(PolicyConfig config)
    {
        return new PolicyConfig
        {
            MaxRetries = Math.Max(0, Math.Min(config.MaxRetries, 10)), // Limit to 0-10 retries
            RetryDelayMs = Math.Max(100, Math.Min(config.RetryDelayMs, 60000)), // 100ms to 60s
            BackoffType = IsValidBackoffType(config.BackoffType) ? config.BackoffType : "Exponential",
            UseJitter = config.UseJitter,
            TimeoutSeconds = Math.Max(1, Math.Min(config.TimeoutSeconds, 300)), // 1s to 5min
            JitterPercentage = Math.Max(0, Math.Min(config.JitterPercentage, 100)), // 0-100%
        };
    }

    /// <summary>
    /// Checks if the backoff type string is valid.
    /// </summary>
    /// <param name="backoffType">The backoff type to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidBackoffType(string backoffType)
    {
        return backoffType?.ToLowerInvariant() switch
        {
            "linear" or "exponential" or "constant" => true,
            _ => false,
        };
    }
}
