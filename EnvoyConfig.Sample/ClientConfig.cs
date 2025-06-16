using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Configuration for an individual SNAPDOG client.
    /// Maps environment variables like SNAPDOG_CLIENT_X_* to properties.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Gets or sets the display name of the client.
        /// Maps to: SNAPDOG_CLIENT_X_NAME
        /// </summary>
        [Env(Key = "NAME")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MAC address of the client.
        /// Maps to: SNAPDOG_CLIENT_X_MAC
        /// </summary>
        [Env(Key = "MAC")]
        public string Mac { get; set; } = null!;

        /// <summary>
        /// Gets or sets the base MQTT topic for this client.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_BASETOPIC
        /// </summary>
        [Env(Key = "MQTT_BASETOPIC")]
        public string MqttBaseTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the default zone for this client.
        /// Maps to: SNAPDOG_CLIENT_X_DEFAULT_ZONE
        /// </summary>
        [Env(Key = "DEFAULT_ZONE", Default = 1)]
        public int DefaultZone { get; set; }

        /// <summary>
        /// Gets or sets the MQTT configuration for this client.
        /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_MQTT_*
        /// </summary>
        [Env(NestedPrefix = "MQTT_")]
        public ClientMqttConfig Mqtt { get; set; } = new();

        /// <summary>
        /// Gets or sets the KNX configuration for this client.
        /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_KNX_*
        /// </summary>
        [Env(NestedPrefix = "KNX_")]
        public ClientKnxConfig Knx { get; set; } = new();
    }
}
