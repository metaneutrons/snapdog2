namespace SnapDog2.Core.Configuration;

/// <summary>
/// Configuration for the zone grouping service behavior.
/// </summary>
public class ZoneGroupingConfig
{
    /// <summary>
    /// Gets or sets the interval in milliseconds between periodic zone grouping checks.
    /// This is the main interval for ensuring zones are properly configured.
    /// Default: 5000ms (5 seconds).
    /// </summary>
    public int PeriodicCheckIntervalMs { get; set; } = 5_000;

    /// <summary>
    /// Gets or sets the interval in milliseconds between zone grouping consistency validations.
    /// Default: 30000ms (30 seconds).
    /// </summary>
    public int ValidationIntervalMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds for event-driven zone grouping operations.
    /// This prevents excessive grouping operations when multiple events occur rapidly.
    /// Default: 500ms.
    /// </summary>
    public int EventDebounceDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the interval in milliseconds between full zone grouping reconciliations.
    /// Full reconciliation includes client name synchronization and comprehensive grouping validation.
    /// Default: 3600000ms (1 hour).
    /// </summary>
    public int FullReconciliationIntervalMs { get; set; } = 3_600_000;

    /// <summary>
    /// Gets or sets whether event-driven zone grouping is enabled.
    /// When enabled, zone grouping operations are triggered immediately by relevant events.
    /// Default: false (disabled for simplicity).
    /// </summary>
    public bool EventDrivenGroupingEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to perform initial reconciliation on service startup.
    /// Default: true.
    /// </summary>
    public bool PerformInitialReconciliation { get; set; } = true;

    /// <summary>
    /// Gets or sets the startup delay in milliseconds before beginning zone grouping operations.
    /// This allows other services to initialize first.
    /// Default: 5000ms (5 seconds).
    /// </summary>
    public int StartupDelayMs { get; set; } = 5_000;

    /// <summary>
    /// Gets or sets whether to log detailed zone grouping statistics periodically.
    /// Default: true.
    /// </summary>
    public bool EnableStatisticsLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for logging zone grouping statistics.
    /// Statistics are logged every N validation cycles.
    /// Default: 10.
    /// </summary>
    public int StatisticsLoggingInterval { get; set; } = 10;
}
