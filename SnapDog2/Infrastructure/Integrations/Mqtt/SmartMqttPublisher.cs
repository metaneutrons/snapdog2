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
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

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
        _mqttService = mqttService;
        _notificationQueue = notificationQueue;
        _logger = logger;
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
        if (_directPublishingEnabled && _mqttService.IsConnected)
        {
            try
            {
                var result = await _mqttService.PublishZoneStatusAsync(
                    zoneIndex,
                    eventType,
                    payload,
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    // Reset circuit breaker on success
                    ResetCircuitBreaker();
                    LogDirectPublishSuccess("Zone", zoneIndex.ToString(), eventType);
                    return result;
                }

                // Direct publish failed - handle failure
                HandleDirectPublishFailure("Zone", zoneIndex.ToString(), eventType, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                HandleDirectPublishFailure("Zone", zoneIndex.ToString(), eventType, ex.Message);
            }
        }

        // Fallback to queue-based publishing
        LogFallingBackToQueue("Zone", zoneIndex.ToString(), eventType);
        await _notificationQueue.EnqueueZoneAsync(eventType, zoneIndex, payload, cancellationToken);
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
        if (_directPublishingEnabled && _mqttService.IsConnected)
        {
            try
            {
                var result = await _mqttService.PublishClientStatusAsync(
                    clientIndex,
                    eventType,
                    payload,
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    // Reset circuit breaker on success
                    ResetCircuitBreaker();
                    LogDirectPublishSuccess("Client", clientIndex, eventType);
                    return result;
                }

                // Direct publish failed - handle failure
                HandleDirectPublishFailure("Client", clientIndex, eventType, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                HandleDirectPublishFailure("Client", clientIndex, eventType, ex.Message);
            }
        }

        // Fallback to queue-based publishing
        LogFallingBackToQueue("Client", clientIndex, eventType);
        await _notificationQueue.EnqueueClientAsync(eventType, clientIndex, payload, cancellationToken);
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
        if (_directPublishingEnabled && _mqttService.IsConnected)
        {
            try
            {
                var result = await _mqttService.PublishGlobalStatusAsync(eventType, payload, cancellationToken);

                if (result.IsSuccess)
                {
                    // Reset circuit breaker on success
                    ResetCircuitBreaker();
                    LogDirectPublishSuccess("Global", "system", eventType);
                    return result;
                }

                // Direct publish failed - handle failure
                HandleDirectPublishFailure("Global", "system", eventType, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                HandleDirectPublishFailure("Global", "system", eventType, ex.Message);
            }
        }

        // Fallback to queue-based publishing
        LogFallingBackToQueue("Global", "system", eventType);
        await _notificationQueue.EnqueueGlobalAsync(eventType, payload, cancellationToken);
        return Result.Success(); // Queue always succeeds
    }

    private void HandleDirectPublishFailure(string entityType, string entityId, string eventType, string? errorMessage)
    {
        _consecutiveFailures++;
        _lastFailureTime = DateTime.UtcNow;

        LogDirectPublishFailure(entityType, entityId, eventType, errorMessage ?? "Unknown error", _consecutiveFailures);

        // Open circuit breaker if too many consecutive failures
        if (_consecutiveFailures >= _maxConsecutiveFailures)
        {
            _directPublishingEnabled = false;
            LogCircuitBreakerOpened(_consecutiveFailures, _circuitBreakerResetTime.TotalMinutes);
        }
    }

    private void ResetCircuitBreaker()
    {
        if (_consecutiveFailures > 0)
        {
            _consecutiveFailures = 0;
            _directPublishingEnabled = true;
            LogCircuitBreakerReset();
        }
    }

    /// <summary>
    /// Checks if circuit breaker should be reset based on time elapsed.
    /// </summary>
    public void CheckCircuitBreakerReset()
    {
        if (!_directPublishingEnabled && DateTime.UtcNow - _lastFailureTime > _circuitBreakerResetTime)
        {
            _directPublishingEnabled = true;
            _consecutiveFailures = 0;
            LogCircuitBreakerAutoReset();
        }
    }

    #region Logging

    [LoggerMessage(
        EventId = 1,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "✅ Direct MQTT publish success: {EntityType} {EntityId} {EventType}"
    )]
    private partial void LogDirectPublishSuccess(string entityType, string entityId, string eventType);

    [LoggerMessage(
        EventId = 2,
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
        EventId = 3,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🔄 Falling back to queue: {EntityType} {EntityId} {EventType}"
    )]
    private partial void LogFallingBackToQueue(string entityType, string entityId, string eventType);

    [LoggerMessage(
        EventId = 4,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "🚫 Circuit breaker opened after {FailureCount} failures - switching to queue-only mode for {ResetTimeMinutes} minutes"
    )]
    private partial void LogCircuitBreakerOpened(int failureCount, double resetTimeMinutes);

    [LoggerMessage(
        EventId = 5,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Circuit breaker reset - direct publishing re-enabled"
    )]
    private partial void LogCircuitBreakerReset();

    [LoggerMessage(
        EventId = 6,
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
