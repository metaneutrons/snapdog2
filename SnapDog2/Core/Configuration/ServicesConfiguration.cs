using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// External services configuration settings for SnapDog2.
/// Maps environment variables with SNAPDOG_SERVICES_ prefix.
///
/// Examples:
/// - SNAPDOG_SERVICES_SNAPCAST_ADDRESS → Snapcast.Host
/// - SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS → Mqtt.Broker
/// - SNAPDOG_SERVICES_KNX_GATEWAY → Knx.Gateway
/// - SNAPDOG_SERVICES_SUBSONIC_URL → Subsonic.Url
/// </summary>
public class ServicesConfiguration
{
    /// <summary>
    /// Gets or sets the Snapcast server configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SNAPCAST_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_SNAPCAST_ADDRESS → Snapcast.Host
    /// - SNAPDOG_SERVICES_SNAPCAST_PORT → Snapcast.Port
    /// - SNAPDOG_SERVICES_SNAPCAST_TIMEOUT → Snapcast.TimeoutSeconds
    /// </summary>
    [Env(NestedPrefix = "SNAPCAST_")]
    public SnapcastConfiguration Snapcast { get; set; } = new();

    /// <summary>
    /// Gets or sets the MQTT broker configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_MQTT_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS → Mqtt.Broker
    /// - SNAPDOG_SERVICES_MQTT_PORT → Mqtt.Port
    /// - SNAPDOG_SERVICES_MQTT_USERNAME → Mqtt.Username
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public ServicesMqttConfiguration Mqtt { get; set; } = new();

    /// <summary>
    /// Gets or sets the KNX gateway configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_KNX_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_KNX_GATEWAY → Knx.Gateway
    /// - SNAPDOG_SERVICES_KNX_PORT → Knx.Port
    /// - SNAPDOG_SERVICES_KNX_ENABLED → Knx.Enabled
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public KnxConfiguration Knx { get; set; } = new();

    /// <summary>
    /// Gets or sets the Subsonic server configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SUBSONIC_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_SUBSONIC_URL → Subsonic.Url
    /// - SNAPDOG_SERVICES_SUBSONIC_USERNAME → Subsonic.Username
    /// - SNAPDOG_SERVICES_SUBSONIC_PASSWORD → Subsonic.Password
    /// </summary>
    [Env(NestedPrefix = "SUBSONIC_")]
    public SubsonicConfiguration Subsonic { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience policies configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_RESILIENCE_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_RESILIENCE_RETRYATTEMPTS → Resilience.RetryAttempts
    /// - SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERDURATION → Resilience.CircuitBreakerDuration
    /// - SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERTHRESHOLD → Resilience.CircuitBreakerThreshold
    /// </summary>
    [Env(NestedPrefix = "RESILIENCE_")]
    public ResilienceConfiguration Resilience { get; set; } = new();
}
