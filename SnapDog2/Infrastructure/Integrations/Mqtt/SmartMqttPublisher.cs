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
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

namespace SnapDog2.Infrastructure.Integrations.Mqtt;

/// <summary>
/// Smart MQTT publisher that uses direct publishing with queue fallback for maximum reliability and performance.
/// Implements the hybrid pattern: Direct publish for speed, queue for resilience.
/// </summary>
public sealed partial class SmartMqttPublisher : ISmartMqttPublisher
{
    private readonly IMqttService _mqttService;
    private readonly INotificationQueue _notificationQueue;
    private readonly ILogger<SmartMqttPublisher> _logger;

    // Circuit breaker state for intelligent fallback
    private volatile bool _directPublishingEnabled = true;
    private volatile int _consecutiveFailures = 0;
    private readonly int _maxConsecutiveFailures = 3;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly TimeSpan _circuitBreakerResetTime = TimeSpan.FromMinutes(1);

    public SmartMqttPublisher(
        IMqttService mqttService,
        INotificationQueue notificationQueue,
        ILogger<SmartMqttPublisher> logger
    )
    {
        this._mqttService = mqttService;
        this._notificationQueue = notificationQueue;
        this._logger = logger;
    }

    /// <summary>
    /// Publishes zone status using smart hybrid approach.
    /// </summary>
    public async Task<Result> PublishZoneStatusAsync<T>(
        int zoneIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        // Try direct publishing first if circuit breaker is closed
        if (this._directPublishingEnabled && this._mqttService.IsConnected)
        {
            try
            {
                var result = await this._mqttService.PublishZoneStatusAsync(
                    zoneIndex,
                    eventType,
                    payload,
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    // Reset circuit breaker on success
                    this.ResetCircuitBreaker();
                    this.LogDirectPublishSuccess("Zone", zoneIndex.ToString(), eventType);
                    return result;
                }

                // Direct publish failed - handle failure
                this.HandleDirectPublishFailure("Zone", zoneIndex.ToString(), eventType, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                this.HandleDirectPublishFailure("Zone", zoneIndex.ToString(), eventType, ex.Message);
            }
        }

        // Fallback to queue-based publishing
        this.LogFallingBackToQueue("Zone", zoneIndex.ToString(), eventType);
        await this._notificationQueue.EnqueueZoneAsync(eventType, zoneIndex, payload, cancellationToken);
        return Result.Success(); // Queue always succeeds
    }

    /// <summary>
    /// Publishes client status using smart hybrid approach.
    /// </summary>
    public async Task<Result> PublishClientStatusAsync<T>(
        string clientIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        // Try direct publishing first if circuit breaker is closed
        if (this._directPublishingEnabled && this._mqttService.IsConnected)
        {
            try
            {
                var result = await this._mqttService.PublishClientStatusAsync(
                    clientIndex,
                    eventType,
                    payload,
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    // Reset circuit breaker on success
                    this.ResetCircuitBreaker();
                    this.LogDirectPublishSuccess("Client", clientIndex, eventType);
                    return result;
                }

                // Direct publish failed - handle failure
                this.HandleDirectPublishFailure("Client", clientIndex, eventType, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                this.HandleDirectPublishFailure("Client", clientIndex, eventType, ex.Message);
            }
        }

        // Fallback to queue-based publishing
        this.LogFallingBackToQueue("Client", clientIndex, eventType);
        await this._notificationQueue.EnqueueClientAsync(eventType, clientIndex, payload, cancellationToken);
        return Result.Success(); // Queue always succeeds
    }

    /// <summary>
    /// Publishes global status using smart hybrid approach.
    /// </summary>
    public async Task<Result> PublishGlobalStatusAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        // Try direct publishing first if circuit breaker is closed
        if (this._directPublishingEnabled && this._mqttService.IsConnected)
        {
            try
            {
                var result = await this._mqttService.PublishGlobalStatusAsync(eventType, payload, cancellationToken);

                if (result.IsSuccess)
                {
                    // Reset circuit breaker on success
                    this.ResetCircuitBreaker();
                    this.LogDirectPublishSuccess("Global", "system", eventType);
                    return result;
                }

                // Direct publish failed - handle failure
                this.HandleDirectPublishFailure("Global", "system", eventType, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                this.HandleDirectPublishFailure("Global", "system", eventType, ex.Message);
            }
        }

        // Fallback to queue-based publishing
        this.LogFallingBackToQueue("Global", "system", eventType);
        await this._notificationQueue.EnqueueGlobalAsync(eventType, payload, cancellationToken);
        return Result.Success(); // Queue always succeeds
    }

    private void HandleDirectPublishFailure(string entityType, string entityId, string eventType, string? errorMessage)
    {
        this._consecutiveFailures++;
        this._lastFailureTime = DateTime.UtcNow;

        this.LogDirectPublishFailure(entityType, entityId, eventType, errorMessage ?? "Unknown error", this._consecutiveFailures);

        // Open circuit breaker if too many consecutive failures
        if (this._consecutiveFailures >= this._maxConsecutiveFailures)
        {
            this._directPublishingEnabled = false;
            this.LogCircuitBreakerOpened(this._consecutiveFailures, this._circuitBreakerResetTime.TotalMinutes);
        }
    }

    private void ResetCircuitBreaker()
    {
        if (this._consecutiveFailures > 0)
        {
            this._consecutiveFailures = 0;
            this._directPublishingEnabled = true;
            this.LogCircuitBreakerReset();
        }
    }

    /// <summary>
    /// Checks if circuit breaker should be reset based on time elapsed.
    /// </summary>
    public void CheckCircuitBreakerReset()
    {
        if (!this._directPublishingEnabled && DateTime.UtcNow - this._lastFailureTime > this._circuitBreakerResetTime)
        {
            this._directPublishingEnabled = true;
            this._consecutiveFailures = 0;
            this.LogCircuitBreakerAutoReset();
        }
    }

    #region Logging

    [LoggerMessage(
        EventId = 4500,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "✅ Direct MQTT publish success: {EntityType} {EntityId} {EventType}"
    )]
    private partial void LogDirectPublishSuccess(string entityType, string entityId, string eventType);

    [LoggerMessage(
        EventId = 4501,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "❌ Direct MQTT publish failed: {EntityType} {EntityId} {EventType} - {ErrorMessage} (Failure #{FailureCount})"
    )]
    private partial void LogDirectPublishFailure(
        string entityType,
        string entityId,
        string eventType,
        string errorMessage,
        int failureCount
    );

    [LoggerMessage(
        EventId = 4502,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🔄 Falling back to queue: {EntityType} {EntityId} {EventType}"
    )]
    private partial void LogFallingBackToQueue(string entityType, string entityId, string eventType);

    [LoggerMessage(
        EventId = 4503,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "🚫 Circuit breaker opened after {FailureCount} failures - switching to queue-only mode for {ResetTimeMinutes} minutes"
    )]
    private partial void LogCircuitBreakerOpened(int failureCount, double resetTimeMinutes);

    [LoggerMessage(
        EventId = 4504,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Circuit breaker reset - direct publishing re-enabled"
    )]
    private partial void LogCircuitBreakerReset();

    [LoggerMessage(
        EventId = 4505,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🔄 Circuit breaker auto-reset after timeout - direct publishing re-enabled"
    )]
    private partial void LogCircuitBreakerAutoReset();

    #endregion
}

/// <summary>
/// Interface for smart MQTT publishing with hybrid direct/queue approach.
/// </summary>
public interface ISmartMqttPublisher
{
    Task<Result> PublishZoneStatusAsync<T>(
        int zoneIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );
    Task<Result> PublishClientStatusAsync<T>(
        string clientIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );
    Task<Result> PublishGlobalStatusAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );
    void CheckCircuitBreakerReset();
}
