using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Configuration for MQTT topics specific to a SNAPDOG client.
    /// Maps environment variables like SNAPDOG_CLIENT_X_MQTT_* to properties.
    /// </summary>
    public class ClientMqttConfig
    {
        /// <summary>
        /// Gets or sets the MQTT topic for setting volume.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_VOLUME_SET_TOPIC
        /// </summary>
        [Env(Key = "VOLUME_SET_TOPIC")]
        public string VolumeSetTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for setting mute state.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_MUTE_SET_TOPIC
        /// </summary>
        [Env(Key = "MUTE_SET_TOPIC")]
        public string MuteSetTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for setting latency.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_LATENCY_SET_TOPIC
        /// </summary>
        [Env(Key = "LATENCY_SET_TOPIC")]
        public string LatencySetTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for setting zone.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_ZONE_SET_TOPIC
        /// </summary>
        [Env(Key = "ZONE_SET_TOPIC")]
        public string ZoneSetTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for control commands.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_CONTROL_TOPIC
        /// </summary>
        [Env(Key = "CONTROL_TOPIC")]
        public string ControlTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for connection status.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_CONNECTED_TOPIC
        /// </summary>
        [Env(Key = "CONNECTED_TOPIC")]
        public string ConnectedTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for volume status.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_VOLUME_TOPIC
        /// </summary>
        [Env(Key = "VOLUME_TOPIC")]
        public string VolumeTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for mute status.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_MUTE_TOPIC
        /// </summary>
        [Env(Key = "MUTE_TOPIC")]
        public string MuteTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for latency status.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_LATENCY_TOPIC
        /// </summary>
        [Env(Key = "LATENCY_TOPIC")]
        public string LatencyTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for zone status.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_ZONE_TOPIC
        /// </summary>
        [Env(Key = "ZONE_TOPIC")]
        public string ZoneTopic { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MQTT topic for general state information.
        /// Maps to: SNAPDOG_CLIENT_X_MQTT_STATE_TOPIC
        /// </summary>
        [Env(Key = "STATE_TOPIC")]
        public string StateTopic { get; set; } = null!;
    }
}
