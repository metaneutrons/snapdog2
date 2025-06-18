using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;

namespace SnapDog2.Infrastructure.HealthChecks;

/// <summary>
/// Health check implementation for KNX service connectivity and operations.
/// Verifies KNX gateway connectivity, performs basic operations, and measures response times.
/// </summary>
public class KnxServiceHealthCheck : IHealthCheck
{
    private readonly IKnxService _knxService;
    private readonly ILogger<KnxServiceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxServiceHealthCheck"/> class.
    /// </summary>
    /// <param name="knxService">The KNX service instance.</param>
    /// <param name="logger">The logger instance.</param>
    public KnxServiceHealthCheck(IKnxService knxService, ILogger<KnxServiceHealthCheck> logger)
    {
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs the KNX service health check asynchronously.
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
            _logger.LogDebug("Starting KNX service health check");

            // Test KNX gateway connection
            var connectionTestTime = Stopwatch.StartNew();
            var isConnected = await _knxService.ConnectAsync(cancellationToken);
            connectionTestTime.Stop();

            data["connectionTime"] = connectionTestTime.ElapsedMilliseconds;
            data["isConnected"] = isConnected;

            if (!isConnected)
            {
                _logger.LogWarning("KNX gateway connection failed");
                return CreateFailureResult(stopwatch, data, "KNX gateway connection failed", null);
            }

            _logger.LogDebug(
                "KNX gateway connection test passed in {ElapsedMs}ms",
                connectionTestTime.ElapsedMilliseconds
            );

            // Test subscription to a health check group address
            // Using a commonly available system group address for testing (0/0/1 - General switching)
            var healthCheckAddress = new KnxAddress(0, 0, 1);
            var subscriptionTestTime = Stopwatch.StartNew();
            var subscriptionResult = await _knxService.SubscribeToGroupAsync(healthCheckAddress, cancellationToken);
            subscriptionTestTime.Stop();

            data["subscriptionTime"] = subscriptionTestTime.ElapsedMilliseconds;
            data["subscriptionResult"] = subscriptionResult;
            data["testAddress"] = healthCheckAddress.ToString();

            if (!subscriptionResult)
            {
                _logger.LogWarning("KNX subscription test failed for address {Address}", healthCheckAddress);
                // Don't fail the health check for subscription issues, as it might be a permissions or addressing issue
                data["subscriptionWarning"] = "Subscription test failed but connection is healthy";
            }
            else
            {
                _logger.LogDebug(
                    "KNX subscription test passed in {ElapsedMs}ms",
                    subscriptionTestTime.ElapsedMilliseconds
                );
            }

            // Test reading from a group address (if subscription was successful)
            if (subscriptionResult)
            {
                var readTestTime = Stopwatch.StartNew();
                try
                {
                    var readValue = await _knxService.ReadGroupValueAsync(healthCheckAddress, cancellationToken);
                    readTestTime.Stop();

                    data["readTime"] = readTestTime.ElapsedMilliseconds;
                    data["readResult"] = readValue != null;
                    data["readValueLength"] = readValue?.Length ?? 0;

                    _logger.LogDebug("KNX read test completed in {ElapsedMs}ms", readTestTime.ElapsedMilliseconds);
                }
                catch (Exception readEx)
                {
                    readTestTime.Stop();
                    data["readTime"] = readTestTime.ElapsedMilliseconds;
                    data["readResult"] = false;
                    data["readWarning"] = "Read test failed but connection is healthy";

                    _logger.LogDebug(readEx, "KNX read test failed but connection is healthy");
                }

                // Clean up subscription
                await _knxService.UnsubscribeFromGroupAsync(healthCheckAddress, cancellationToken);
            }

            stopwatch.Stop();
            data["totalTime"] = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("KNX service health check passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("KNX service is healthy and responsive", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("KNX service health check was cancelled");
            return CreateFailureResult(stopwatch, data, "KNX service health check was cancelled", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KNX service health check failed with exception");
            return CreateFailureResult(stopwatch, data, "KNX service health check failed", ex);
        }
        finally
        {
            // Ensure we disconnect to clean up resources
            try
            {
                await _knxService.DisconnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect KNX service during health check cleanup");
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
