using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;

namespace SnapDog2.Infrastructure.HealthChecks;

/// <summary>
/// Health check implementation for Snapcast service connectivity and operations.
/// Verifies Snapcast server availability, performs basic operations, and measures response times.
/// </summary>
public class SnapcastServiceHealthCheck : IHealthCheck
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<SnapcastServiceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapcastServiceHealthCheck"/> class.
    /// </summary>
    /// <param name="snapcastService">The Snapcast service instance.</param>
    /// <param name="logger">The logger instance.</param>
    public SnapcastServiceHealthCheck(ISnapcastService snapcastService, ILogger<SnapcastServiceHealthCheck> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs the Snapcast service health check asynchronously.
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
            _logger.LogDebug("Starting Snapcast service health check");

            // Test server availability
            var availabilityTestTime = Stopwatch.StartNew();
            var isAvailable = await _snapcastService.IsServerAvailableAsync(cancellationToken);
            availabilityTestTime.Stop();

            data["availabilityTime"] = availabilityTestTime.ElapsedMilliseconds;
            data["isAvailable"] = isAvailable;

            if (!isAvailable)
            {
                _logger.LogWarning("Snapcast server is not available");
                return CreateFailureResult(stopwatch, data, "Snapcast server is not available", null);
            }

            _logger.LogDebug(
                "Snapcast server availability test passed in {ElapsedMs}ms",
                availabilityTestTime.ElapsedMilliseconds
            );

            // Test server status retrieval
            var statusTestTime = Stopwatch.StartNew();
            var serverStatus = await _snapcastService.GetServerStatusAsync(cancellationToken);
            statusTestTime.Stop();

            data["statusTime"] = statusTestTime.ElapsedMilliseconds;
            data["hasStatus"] = !string.IsNullOrEmpty(serverStatus);

            _logger.LogDebug(
                "Snapcast server status test completed in {ElapsedMs}ms",
                statusTestTime.ElapsedMilliseconds
            );

            // Test groups retrieval
            var groupsTestTime = Stopwatch.StartNew();
            var groups = await _snapcastService.GetGroupsAsync(cancellationToken);
            var groupsList = groups.ToList();
            groupsTestTime.Stop();

            data["groupsTime"] = groupsTestTime.ElapsedMilliseconds;
            data["groupCount"] = groupsList.Count;

            _logger.LogDebug("Snapcast groups test completed in {ElapsedMs}ms", groupsTestTime.ElapsedMilliseconds);

            // Test clients retrieval
            var clientsTestTime = Stopwatch.StartNew();
            var clients = await _snapcastService.GetClientsAsync(cancellationToken);
            var clientsList = clients.ToList();
            clientsTestTime.Stop();

            data["clientsTime"] = clientsTestTime.ElapsedMilliseconds;
            data["clientCount"] = clientsList.Count;

            _logger.LogDebug("Snapcast clients test completed in {ElapsedMs}ms", clientsTestTime.ElapsedMilliseconds);

            stopwatch.Stop();
            data["totalTime"] = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "Snapcast service health check passed in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );

            return HealthCheckResult.Healthy("Snapcast service is healthy and responsive", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Snapcast service health check was cancelled");
            return CreateFailureResult(stopwatch, data, "Snapcast service health check was cancelled", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Snapcast service health check failed with exception");
            return CreateFailureResult(stopwatch, data, "Snapcast service health check failed", ex);
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
