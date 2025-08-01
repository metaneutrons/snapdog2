namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

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
