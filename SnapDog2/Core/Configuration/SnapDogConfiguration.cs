using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Main configuration class for SnapDog2 multi-room audio system.
/// This is the single root configuration class that orchestrates all subsystem configurations.
/// Environment variables are mapped with SNAPDOG_ prefix following EnvoyConfig patterns.
///
/// Examples:
/// - SNAPDOG_SYSTEM_ENVIRONMENT → System.Environment
/// - SNAPDOG_TELEMETRY_ENABLED → Telemetry.Enabled
/// - SNAPDOG_API_PORT → Api.Port
/// - SNAPDOG_CLIENT_1_NAME → Clients[0].Name
/// </summary>
public class SnapDogConfiguration
{
    /// <summary>
    /// Gets or sets the system-level configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_SYSTEM_*
    ///
    /// Examples:
    /// - SNAPDOG_SYSTEM_ENVIRONMENT → System.Environment
    /// - SNAPDOG_SYSTEM_LOG_LEVEL → System.LogLevel
    /// - SNAPDOG_SYSTEM_APPLICATION_NAME → System.ApplicationName
    /// </summary>
    [Env(NestedPrefix = "SYSTEM_")]
    public SystemConfiguration System { get; set; } = new();

    /// <summary>
    /// Gets or sets the telemetry and monitoring configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_*
    ///
    /// Examples:
    /// - SNAPDOG_TELEMETRY_ENABLED → Telemetry.Enabled
    /// - SNAPDOG_TELEMETRY_SERVICE_NAME → Telemetry.ServiceName
    /// - SNAPDOG_TELEMETRY_PROMETHEUS_ENABLED → Telemetry.PrometheusEnabled
    /// </summary>
    [Env(NestedPrefix = "TELEMETRY_")]
    public TelemetryConfiguration Telemetry { get; set; } = new();

    /// <summary>
    /// Gets or sets the API configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_API_*
    ///
    /// Examples:
    /// - SNAPDOG_API_PORT → Api.Port
    /// - SNAPDOG_API_HTTPS_ENABLED → Api.HttpsEnabled
    /// - SNAPDOG_API_AUTH_ENABLED → Api.AuthEnabled
    /// </summary>
    [Env(NestedPrefix = "API_")]
    public ApiConfiguration Api { get; set; } = new();

    /// <summary>
    /// Gets or sets the external services configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_SNAPCAST_HOST → Services.Snapcast.Host
    /// - SNAPDOG_SERVICES_MQTT_BROKER → Services.Mqtt.Broker
    /// - SNAPDOG_SERVICES_KNX_GATEWAY → Services.Knx.Gateway
    /// </summary>
    [Env(NestedPrefix = "SERVICES_")]
    public ServicesConfiguration Services { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of zone configurations.
    /// Maps environment variables with pattern: SNAPDOG_ZONE_X_*
    /// Where X is the zone index (1, 2, 3, etc.)
    ///
    /// Examples:
    /// - SNAPDOG_ZONE_1_NAME → Zones[0].Name
    /// - SNAPDOG_ZONE_1_DESCRIPTION → Zones[0].Description
    /// - SNAPDOG_ZONE_2_ENABLED → Zones[1].Enabled
    /// </summary>
    [Env(NestedListPrefix = "ZONE_", NestedListSuffix = "_")]
    public List<ZoneConfiguration> Zones { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of Snapcast client configurations.
    /// Maps environment variables with pattern: SNAPDOG_CLIENT_X_*
    /// Where X is the client index (1, 2, 3, etc.)
    ///
    /// Examples:
    /// - SNAPDOG_CLIENT_1_NAME → Clients[0].Name
    /// - SNAPDOG_CLIENT_1_MAC → Clients[0].Mac
    /// - SNAPDOG_CLIENT_1_MQTT_BASETOPIC → Clients[0].MqttBaseTopic
    /// - SNAPDOG_CLIENT_1_MQTT_VOLUME_SET_TOPIC → Clients[0].Mqtt.VolumeSetTopic
    /// - SNAPDOG_CLIENT_1_KNX_ENABLED → Clients[0].Knx.Enabled
    /// - SNAPDOG_CLIENT_1_KNX_VOLUME → Clients[0].Knx.Volume
    /// </summary>
    [Env(NestedListPrefix = "CLIENT_", NestedListSuffix = "_")]
    public List<ClientConfiguration> Clients { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of radio station configurations.
    /// Maps environment variables with pattern: SNAPDOG_RADIO_X_*
    /// Where X is the radio station index (1, 2, 3, etc.)
    ///
    /// Examples:
    /// - SNAPDOG_RADIO_1_NAME → RadioStations[0].Name
    /// - SNAPDOG_RADIO_1_URL → RadioStations[0].Url
    /// - SNAPDOG_RADIO_2_NAME → RadioStations[1].Name
    /// - SNAPDOG_RADIO_2_URL → RadioStations[1].Url
    /// </summary>
    [Env(NestedListPrefix = "RADIO_", NestedListSuffix = "_")]
    public List<RadioStationConfiguration> RadioStations { get; set; } = [];
}
