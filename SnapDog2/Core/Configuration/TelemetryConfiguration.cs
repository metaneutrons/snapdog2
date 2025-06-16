using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Telemetry and monitoring configuration settings for SnapDog2.
/// Maps environment variables with SNAPDOG_TELEMETRY_ prefix.
///
/// Examples:
/// - SNAPDOG_TELEMETRY_ENABLED → Enabled
/// - SNAPDOG_TELEMETRY_SERVICE_NAME → ServiceName
/// - SNAPDOG_TELEMETRY_PROMETHEUS_ENABLED → PrometheusEnabled
/// - SNAPDOG_TELEMETRY_JAEGER_ENDPOINT → JaegerEndpoint
/// </summary>
public class TelemetryConfiguration
{
    /// <summary>
    /// Gets or sets whether telemetry is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for telemetry identification.
    /// Maps to: SNAPDOG_TELEMETRY_SERVICE_NAME
    /// </summary>
    [Env(Key = "SERVICE_NAME", Default = "snapdog2")]
    public string ServiceName { get; set; } = "snapdog2";

    /// <summary>
    /// Gets or sets the telemetry sampling rate (0.0 to 1.0).
    /// Maps to: SNAPDOG_TELEMETRY_SAMPLING_RATE
    /// </summary>
    [Env(Key = "SAMPLING_RATE", Default = 1.0)]
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether Prometheus metrics are enabled.
    /// Maps to: SNAPDOG_TELEMETRY_PROMETHEUS_ENABLED
    /// </summary>
    [Env(Key = "PROMETHEUS_ENABLED", Default = true)]
    public bool PrometheusEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Prometheus metrics endpoint path.
    /// Maps to: SNAPDOG_TELEMETRY_PROMETHEUS_PATH
    /// </summary>
    [Env(Key = "PROMETHEUS_PATH", Default = "/metrics")]
    public string PrometheusPath { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the Prometheus metrics port.
    /// Maps to: SNAPDOG_TELEMETRY_PROMETHEUS_PORT
    /// </summary>
    [Env(Key = "PROMETHEUS_PORT", Default = 9090)]
    public int PrometheusPort { get; set; } = 9090;

    /// <summary>
    /// Gets or sets whether Jaeger tracing is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_JAEGER_ENABLED
    /// </summary>
    [Env(Key = "JAEGER_ENABLED", Default = true)]
    public bool JaegerEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Jaeger collector endpoint.
    /// Maps to: SNAPDOG_TELEMETRY_JAEGER_ENDPOINT
    /// </summary>
    [Env(Key = "JAEGER_ENDPOINT", Default = "http://jaeger:14268")]
    public string JaegerEndpoint { get; set; } = "http://jaeger:14268";

    /// <summary>
    /// Gets or sets the Jaeger agent host.
    /// Maps to: SNAPDOG_TELEMETRY_JAEGER_AGENT_HOST
    /// </summary>
    [Env(Key = "JAEGER_AGENT_HOST", Default = "jaeger")]
    public string JaegerAgentHost { get; set; } = "jaeger";

    /// <summary>
    /// Gets or sets the Jaeger agent port.
    /// Maps to: SNAPDOG_TELEMETRY_JAEGER_AGENT_PORT
    /// </summary>
    [Env(Key = "JAEGER_AGENT_PORT", Default = 6831)]
    public int JaegerAgentPort { get; set; } = 6831;

    /// <summary>
    /// Gets or sets whether OpenTelemetry is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_OPENTEL_ENABLED
    /// </summary>
    [Env(Key = "OPENTEL_ENABLED", Default = true)]
    public bool OpenTelemetryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the OpenTelemetry endpoint.
    /// Maps to: SNAPDOG_TELEMETRY_OPENTEL_ENDPOINT
    /// </summary>
    [Env(Key = "OPENTEL_ENDPOINT", Default = "http://otel-collector:4317")]
    public string OpenTelemetryEndpoint { get; set; } = "http://otel-collector:4317";

    /// <summary>
    /// Gets or sets whether health checks are enabled.
    /// Maps to: SNAPDOG_TELEMETRY_HEALTH_CHECKS_ENABLED
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_ENABLED", Default = true)]
    public bool HealthChecksEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the health check endpoint path.
    /// Maps to: SNAPDOG_TELEMETRY_HEALTH_CHECK_PATH
    /// </summary>
    [Env(Key = "HEALTH_CHECK_PATH", Default = "/health")]
    public string HealthCheckPath { get; set; } = "/health";
}
