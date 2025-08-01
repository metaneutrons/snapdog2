namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

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
}

/// <summary>
/// OTLP configuration for Jaeger integration.
/// </summary>
public class OtlpConfig
{
    /// <summary>
    /// Whether OTLP export is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// OTLP endpoint URL.
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_ENDPOINT
    /// </summary>
    [Env(Key = "ENDPOINT", Default = "http://localhost:14268")]
    public string Endpoint { get; set; } = "http://localhost:14268";

    /// <summary>
    /// OTLP agent address.
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_AGENT_ADDRESS
    /// </summary>
    [Env(Key = "AGENT_ADDRESS", Default = "localhost")]
    public string AgentAddress { get; set; } = "localhost";

    /// <summary>
    /// OTLP agent port.
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_AGENT_PORT
    /// </summary>
    [Env(Key = "AGENT_PORT", Default = 6831)]
    public int AgentPort { get; set; } = 6831;
}

/// <summary>
/// Prometheus configuration.
/// </summary>
public class PrometheusConfig
{
    /// <summary>
    /// Whether Prometheus metrics are enabled.
    /// Maps to: SNAPDOG_TELEMETRY_PROMETHEUS_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Prometheus metrics port.
    /// Maps to: SNAPDOG_TELEMETRY_PROMETHEUS_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 9090)]
    public int Port { get; set; } = 9090;

    /// <summary>
    /// Prometheus metrics path.
    /// Maps to: SNAPDOG_TELEMETRY_PROMETHEUS_PATH
    /// </summary>
    [Env(Key = "PATH", Default = "/metrics")]
    public string Path { get; set; } = "/metrics";
}
