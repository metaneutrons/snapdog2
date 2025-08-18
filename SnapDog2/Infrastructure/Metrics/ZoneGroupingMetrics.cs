namespace SnapDog2.Infrastructure.Metrics;

using System.Diagnostics.Metrics;
using SnapDog2.Core.Models;

/// <summary>
/// OpenTelemetry metrics for zone grouping operations.
/// Provides comprehensive monitoring of zone grouping health and performance.
/// </summary>
public class ZoneGroupingMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _reconciliationCounter;
    private readonly Histogram<double> _reconciliationDuration;
    private readonly ObservableGauge<int> _totalZones;
    private readonly ObservableGauge<int> _healthyZones;
    private readonly ObservableGauge<int> _degradedZones;
    private readonly ObservableGauge<int> _unhealthyZones;
    private readonly Counter<long> _clientUpdatesCounter;
    private readonly Counter<long> _errorsCounter;

    // Current state for observable gauges
    private volatile int _currentTotalZones;
    private volatile int _currentHealthyZones;
    private volatile int _currentDegradedZones;
    private volatile int _currentUnhealthyZones;

    public ZoneGroupingMetrics()
    {
        _meter = new Meter("SnapDog2.ZoneGrouping", "1.0.0");

        // Counters for events
        _reconciliationCounter = _meter.CreateCounter<long>(
            "zone_grouping_reconciliations_total",
            description: "Total number of zone grouping reconciliations performed"
        );

        _clientUpdatesCounter = _meter.CreateCounter<long>(
            "zone_grouping_client_updates_total",
            description: "Total number of client name updates performed"
        );

        _errorsCounter = _meter.CreateCounter<long>(
            "zone_grouping_errors_total",
            description: "Total number of zone grouping errors encountered"
        );

        // Histogram for performance
        _reconciliationDuration = _meter.CreateHistogram<double>(
            "zone_grouping_reconciliation_duration_seconds",
            unit: "s",
            description: "Duration of zone grouping reconciliation operations"
        );

        // Observable gauges for current state
        _totalZones = _meter.CreateObservableGauge<int>(
            "zone_grouping_zones_total",
            observeValue: () => _currentTotalZones,
            description: "Total number of zones configured"
        );

        _healthyZones = _meter.CreateObservableGauge<int>(
            "zone_grouping_zones_healthy",
            observeValue: () => _currentHealthyZones,
            description: "Number of zones in healthy state"
        );

        _degradedZones = _meter.CreateObservableGauge<int>(
            "zone_grouping_zones_degraded",
            observeValue: () => _currentDegradedZones,
            description: "Number of zones in degraded state"
        );

        _unhealthyZones = _meter.CreateObservableGauge<int>(
            "zone_grouping_zones_unhealthy",
            observeValue: () => _currentUnhealthyZones,
            description: "Number of zones in unhealthy state"
        );
    }

    /// <summary>
    /// Records a reconciliation operation with its results.
    /// </summary>
    /// <param name="durationSeconds">Duration of the reconciliation in seconds</param>
    /// <param name="success">Whether the reconciliation was successful</param>
    /// <param name="clientUpdates">Number of client updates performed</param>
    /// <param name="errorType">Type of error if unsuccessful (optional)</param>
    public void RecordReconciliation(
        double durationSeconds,
        bool success,
        int clientUpdates = 0,
        string? errorType = null
    )
    {
        var tags = new KeyValuePair<string, object?>[] { new("success", success.ToString().ToLowerInvariant()) };

        _reconciliationCounter.Add(1, tags);
        _reconciliationDuration.Record(durationSeconds, tags);

        if (clientUpdates > 0)
        {
            _clientUpdatesCounter.Add(clientUpdates);
        }

        if (!success && !string.IsNullOrEmpty(errorType))
        {
            _errorsCounter.Add(1, new KeyValuePair<string, object?>[] { new("error_type", errorType) });
        }
    }

    /// <summary>
    /// Updates the current zone status metrics.
    /// </summary>
    /// <param name="status">Current zone grouping status</param>
    public void UpdateZoneStatus(ZoneGroupingStatus status)
    {
        _currentTotalZones = status.TotalZones;

        // Count zones by health status
        var healthyCount = 0;
        var degradedCount = 0;
        var unhealthyCount = 0;

        foreach (var zone in status.ZoneDetails)
        {
            switch (zone.Health)
            {
                case ZoneGroupingHealth.Healthy:
                    healthyCount++;
                    break;
                case ZoneGroupingHealth.Degraded:
                    degradedCount++;
                    break;
                case ZoneGroupingHealth.Unhealthy:
                    unhealthyCount++;
                    break;
            }
        }

        _currentHealthyZones = healthyCount;
        _currentDegradedZones = degradedCount;
        _currentUnhealthyZones = unhealthyCount;
    }

    /// <summary>
    /// Records an error in zone grouping operations.
    /// </summary>
    /// <param name="errorType">Type of error encountered</param>
    /// <param name="operation">Operation where error occurred</param>
    public void RecordError(string errorType, string operation)
    {
        _errorsCounter.Add(
            1,
            new KeyValuePair<string, object?>[] { new("error_type", errorType), new("operation", operation) }
        );
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
