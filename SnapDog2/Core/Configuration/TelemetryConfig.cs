namespace SnapDog2.Core.Configuration;

/// <summary>
/// Telemetry and observability configuration.
/// </summary>
public class TelemetryConfig
{
    /// <summary>
    /// Whether telemetry is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Service name for telemetry.
    /// Maps to: SNAPDOG_TELEMETRY_SERVICE_NAME
    /// </summary>
    [Env(Key = "SERVICE_NAME", Default = "SnapDog2")]
    public string ServiceName { get; set; } = "SnapDog2";

    /// <summary>
    /// Sampling rate for traces (0.0 to 1.0).
    /// Maps to: SNAPDOG_TELEMETRY_SAMPLING_RATE
    /// </summary>
    [Env(Key = "SAMPLING_RATE", Default = 1.0)]
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// OTLP configuration for Jaeger.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_OTLP_*
    /// </summary>
    [Env(NestedPrefix = "OTLP_")]
    public OtlpConfig Otlp { get; set; } = new();

    /// <summary>
    /// Prometheus configuration.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_PROMETHEUS_*
    /// </summary>
    [Env(NestedPrefix = "PROMETHEUS_")]
    public PrometheusConfig Prometheus { get; set; } = new();

    /// <summary>
    /// TelemetrySeq configuration.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_SEQ_*
    /// </summary>
    [Env(NestedPrefix = "SEQ_")]
    public TelemetrySeqConfig TelemetrySeq { get; set; } = new();
}
