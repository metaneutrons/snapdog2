namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// MQTT service configuration.
/// </summary>
public class MqttConfig
{
    /// <summary>
    /// Whether MQTT integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// MQTT broker address.
    /// Maps to: SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS
    /// </summary>
    [Env(Key = "BROKER_ADDRESS", Default = "localhost")]
    public string BrokerAddress { get; set; } = "localhost";

    /// <summary>
    /// MQTT broker port.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 1883)]
    public int Port { get; set; } = 1883;

    /// <summary>
    /// MQTT client ID.
    /// Maps to: SNAPDOG_SERVICES_MQTT_CLIENT_ID
    /// </summary>
    [Env(Key = "CLIENT_ID", Default = "snapdog-server")]
    public string ClientIndex { get; set; } = "snapdog-server";

    /// <summary>
    /// Whether SSL is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_SSL_ENABLED
    /// </summary>
    [Env(Key = "SSL_ENABLED", Default = false)]
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// MQTT username.
    /// Maps to: SNAPDOG_SERVICES_MQTT_USERNAME
    /// </summary>
    [Env(Key = "USERNAME")]
    public string? Username { get; set; }

    /// <summary>
    /// MQTT password.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD")]
    public string? Password { get; set; }

    /// <summary>
    /// MQTT keep alive interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_MQTT_KEEP_ALIVE
    /// </summary>
    [Env(Key = "KEEP_ALIVE", Default = 60)]
    public int KeepAlive { get; set; } = 60;

    /// <summary>
    /// Resilience policy configuration for MQTT operations.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_MQTT_RESILIENCE_*
    /// </summary>
    [Env(NestedPrefix = "RESILIENCE_")]
    public ResilienceConfig Resilience { get; set; } =
        new()
        {
            Connection = new PolicyConfig
            {
                MaxRetries = 3,
                RetryDelayMs = 2000,
                BackoffType = "Exponential",
                UseJitter = true,
                TimeoutSeconds = 30,
            },
            Operation = new PolicyConfig
            {
                MaxRetries = 2,
                RetryDelayMs = 500,
                BackoffType = "Linear",
                UseJitter = false,
                TimeoutSeconds = 10,
            },
        };
}
