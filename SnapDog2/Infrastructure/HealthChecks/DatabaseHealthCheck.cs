using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.HealthChecks;

/// <summary>
/// Health check implementation for database connectivity and operations.
/// Verifies database connection, performs basic operations, and measures response times.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly SnapDogDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for health checks.</param>
    /// <param name="logger">The logger instance.</param>
    public DatabaseHealthCheck(SnapDogDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs the database health check asynchronously.
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
            _logger.LogDebug("Starting database health check");

            // Test database connection
            var connectionTestTime = Stopwatch.StartNew();
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            connectionTestTime.Stop();

            if (!canConnect)
            {
                _logger.LogWarning("Database connection test failed");
                return CreateFailureResult(stopwatch, data, "Database connection test failed", null);
            }

            data["connectionTime"] = connectionTestTime.ElapsedMilliseconds;
            _logger.LogDebug(
                "Database connection test passed in {ElapsedMs}ms",
                connectionTestTime.ElapsedMilliseconds
            );

            // Test basic database operations
            var queryTestTime = Stopwatch.StartNew();
            var audioStreamCount = await _dbContext.AudioStreams.CountAsync(cancellationToken);
            var clientCount = await _dbContext.Clients.CountAsync(cancellationToken);
            var zoneCount = await _dbContext.Zones.CountAsync(cancellationToken);
            queryTestTime.Stop();

            data["queryTime"] = queryTestTime.ElapsedMilliseconds;
            data["audioStreamCount"] = audioStreamCount;
            data["clientCount"] = clientCount;
            data["zoneCount"] = zoneCount;

            _logger.LogDebug("Database query test completed in {ElapsedMs}ms", queryTestTime.ElapsedMilliseconds);

            // Get database provider information
            var providerName = _dbContext.Database.ProviderName;
            data["provider"] = providerName ?? "Unknown";

            stopwatch.Stop();
            data["totalTime"] = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Database health check passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("Database is healthy and responsive", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Database health check was cancelled");
            return CreateFailureResult(stopwatch, data, "Database health check was cancelled", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return CreateFailureResult(stopwatch, data, "Database health check failed", ex);
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
