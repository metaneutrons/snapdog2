using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample;

using System.Collections.Generic;

public class SampleConfig
{
    [Env(Key = "ENVIRONMENT", Default = "Development")]
    public string Env { get; set; } = null!;

    [Env(Key = "LOG_LEVEL", Default = "Information")]
    public string LogLevel { get; set; } = null!;

    [Env(Key = "TELEMETRY_ENABLED", Default = true)]
    public bool TelemetryEnabled { get; set; }

    [Env(Key = "TELEMETRY_SERVICE_NAME", Default = "sample")]
    public string TelemetryServiceName { get; set; } = null!;

    [Env(Key = "TELEMETRY_SAMPLING_RATE")]
    public double TelemetrySamplingRate { get; set; }

    [Env(Key = "PROMETHEUS_ENABLED")]
    public bool PrometheusEnabled { get; set; }

    [Env(Key = "PROMETHEUS_PATH", Default = "/metrics")]
    public string PrometheusPath { get; set; } = null!;

    [Env(Key = "PROMETHEUS_PORT", Default = 9090)]
    public int PrometheusPort { get; set; }

    [Env(Key = "JAEGER_ENABLED", Default = true)]
    public bool JaegerEnabled { get; set; }

    [Env(Key = "JAEGER_ENDPOINT", Default = "http://jaeger:14268")]
    public string JaegerEndpoint { get; set; } = null!;

    [Env(Key = "JAEGER_AGENT_HOST", Default = "jaeger")]
    public string JaegerAgentHost { get; set; } = null!;

    [Env(Key = "JAEGER_AGENT_PORT")]
    public int JaegerAgentPort { get; set; }

    [Env(Key = "API_AUTH_ENABLED")]
    public bool ApiAuthEnabled { get; set; }

    [Env(ListPrefix = "API_APIKEY_")]
    public List<string> ApiKeys { get; set; } = [];

    [Env(Key = "ZONES", IsList = true)]
    public List<string> Zones { get; set; } = [];

    // Snapcast configuration as a map
    [Env(MapPrefix = "SNAPCAST_")]
    public Dictionary<string, string> Snapcast { get; set; } = [];

    // List of MQTT zone configs
    [Env(NestedListPrefix = "ZONE_", NestedListSuffix = "_MQTT_")]
    public List<SampleZoneMqttConfig> ZonesMqtt { get; set; } = [];

    /// <summary>
    /// List of SNAPDOG client configurations for multi-room audio system integration.
    /// Each client represents an individual SNAPDOG device with its MQTT topics and KNX addresses.
    /// </summary>
    [Env(NestedListPrefix = "CLIENT_", NestedListSuffix = "_")]
    public List<ClientConfig> SnapdogClients { get; set; } = [];

    /// <summary>
    /// List of radio station configurations for the SNAPDOG system.
    /// Each station includes a name and URL for streaming.
    /// </summary>
    [Env(NestedListPrefix = "RADIO_", NestedListSuffix = "_")]
    public List<RadioStation> RadioStations { get; set; } = [];
}
