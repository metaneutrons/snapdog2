namespace SnapDog2.Core.Configuration;

/// <summary>
/// Configuration for the simple periodic zone grouping service.
/// </summary>
public class ZoneGroupingConfig
{
    /// <summary>
    /// Gets or sets the interval in milliseconds between periodic zone grouping checks.
    /// This is the main interval for ensuring zones are properly configured.
    /// Default: 5000ms (5 seconds).
    /// </summary>
    public int PeriodicCheckIntervalMs { get; set; } = 5_000;
}
