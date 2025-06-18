using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;

namespace SnapDog2.Infrastructure.HealthChecks;

/// <summary>
/// Health check implementation for MQTT service connectivity and operations.
/// Verifies MQTT broker connectivity, performs basic operations, and measures response times.
/// </summary>
public class MqttServiceHealthCheck : IHealthCheck
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<MqttServiceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttServiceHealthCheck"/> class.
    /// </summary>
    /// <param name="mqttService">The MQTT service instance.</param>
    /// <param name="logger">The logger instance.</param>
    public MqttServiceHealthCheck(IMqttService mqttService, ILogger<MqttServiceHealthCheck> logger)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs the MQTT service health check asynchronously.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            _logger.LogDebug("Starting MQTT service health check");

            // Test MQTT broker connection
            var connectionTestTime = Stopwatch.StartNew();
            var isConnected = await _mqttService.ConnectAsync(cancellationToken);
            connectionTestTime.Stop();

            data["connectionTime"] = connectionTestTime.ElapsedMilliseconds;
            data["isConnected"] = isConnected;

            if (!isConnected)
            {
                _logger.LogWarning("MQTT broker connection failed");
                return CreateFailureResult(stopwatch, data, "MQTT broker connection failed", null);
            }

            _logger.LogDebug(
                "MQTT broker connection test passed in {ElapsedMs}ms",
                connectionTestTime.ElapsedMilliseconds
            );

            // Test subscription to a health check topic
            var subscriptionTestTime = Stopwatch.StartNew();
            var healthCheckTopic = "snapdog/health/check";
            var subscriptionResult = await _mqttService.SubscribeAsync(healthCheckTopic, cancellationToken);
            subscriptionTestTime.Stop();

            data["subscriptionTime"] = subscriptionTestTime.ElapsedMilliseconds;
            data["subscriptionResult"] = subscriptionResult;

            if (!subscriptionResult)
            {
                _logger.LogWarning("MQTT subscription test failed");
                // Don't fail the health check for subscription issues, as it might be a permissions issue
                data["subscriptionWarning"] = "Subscription test failed but connection is healthy";
            }
            else
            {
                _logger.LogDebug(
                    "MQTT subscription test passed in {ElapsedMs}ms",
                    subscriptionTestTime.ElapsedMilliseconds
                );
            }

            // Test publishing a health check message
            var publishTestTime = Stopwatch.StartNew();
            var healthCheckMessage = $"Health check at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            var publishResult = await _mqttService.PublishAsync(
                healthCheckTopic,
                healthCheckMessage,
                cancellationToken
            );
            publishTestTime.Stop();

            data["publishTime"] = publishTestTime.ElapsedMilliseconds;
            data["publishResult"] = publishResult;

            if (!publishResult)
            {
                _logger.LogWarning("MQTT publish test failed");
                // Don't fail the health check for publish issues, as it might be a permissions issue
                data["publishWarning"] = "Publish test failed but connection is healthy";
            }
            else
            {
                _logger.LogDebug("MQTT publish test passed in {ElapsedMs}ms", publishTestTime.ElapsedMilliseconds);
            }

            // Clean up subscription
            if (subscriptionResult)
            {
                await _mqttService.UnsubscribeAsync(healthCheckTopic, cancellationToken);
            }

            stopwatch.Stop();
            data["totalTime"] = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("MQTT service health check passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("MQTT service is healthy and responsive", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MQTT service health check was cancelled");
            return CreateFailureResult(stopwatch, data, "MQTT service health check was cancelled", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT service health check failed with exception");
            return CreateFailureResult(stopwatch, data, "MQTT service health check failed", ex);
        }
        finally
        {
            // Ensure we disconnect to clean up resources
            try
            {
                await _mqttService.DisconnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect MQTT service during health check cleanup");
            }
        }
    }

    /// <summary>
    /// Creates a failure result with consistent data structure.
    /// </summary>
    /// <param name="stopwatch">The timing stopwatch.</param>
    /// <param name="data">The data dictionary to populate.</param>
    /// <param name="description">The failure description.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    /// <returns>An unhealthy health check result.</returns>
    private static HealthCheckResult CreateFailureResult(
        Stopwatch stopwatch,
        Dictionary<string, object> data,
        string description,
        Exception? exception
    )
    {
        stopwatch.Stop();
        data["totalTime"] = stopwatch.ElapsedMilliseconds;

        if (exception != null)
        {
            data["error"] = exception.Message;
            data["errorType"] = exception.GetType().Name;
        }

        return HealthCheckResult.Unhealthy(description, exception, data);
    }
}
