using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// MQTT broker configuration.
/// </summary>
public class MqttConfiguration
{
    /// <summary>
    /// Gets or sets the MQTT broker hostname or IP address.
    /// Maps to: SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS
    /// </summary>
    [Env(Key = "BROKER", Default = "localhost")]
    public string Broker { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the MQTT broker port.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 1883)]
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets the MQTT username.
    /// Maps to: SNAPDOG_SERVICES_MQTT_USERNAME
    /// </summary>
    [Env(Key = "USERNAME", Default = "")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT password.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD", Default = "")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT client ID.
    /// Maps to: SNAPDOG_SERVICES_MQTT_CLIENT_ID
    /// </summary>
    [Env(Key = "CLIENT_ID", Default = "snapdog2")]
    public string ClientId { get; set; } = "snapdog2";

    /// <summary>
    /// Gets or sets whether SSL/TLS is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_SSL_ENABLED
    /// </summary>
    [Env(Key = "SSL_ENABLED", Default = false)]
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the keep-alive interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_MQTT_KEEP_ALIVE
    /// </summary>
    [Env(Key = "KEEP_ALIVE", Default = 60)]
    public int KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether MQTT integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the base topic for MQTT messages from system configuration.
    /// This is a temporary property for backward compatibility.
    /// TODO: Update MqttService to use SystemMqttConfiguration directly.
    /// </summary>
    public string BaseTopic { get; set; } = "snapdog";
}
