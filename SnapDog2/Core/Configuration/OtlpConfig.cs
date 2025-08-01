namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

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
