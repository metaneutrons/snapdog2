namespace SnapDog2.Core.Configuration;

/// <summary>
/// TelemetrySeq configuration.
/// </summary>
public class TelemetrySeqConfig
{
    /// <summary>
    /// Whether TelemetrySeq logging is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_SEQ_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// TelemetrySeq server URL.
    /// Maps to: SNAPDOG_TELEMETRY_SEQ_URL
    /// </summary>
    [Env(Key = "URL")]
    public string? Url { get; set; }
}
