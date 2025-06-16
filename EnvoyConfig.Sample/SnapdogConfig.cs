using System.Collections.Generic;
using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Main configuration class for SNAPDOG clients.
    /// Maps environment variables like SNAPDOG_CLIENT_X_* to a list of client configurations.
    /// This replaces SampleConfig as the primary configuration class for the SNAPDOG system.
    /// </summary>
    public class SnapdogConfig
    {
        /// <summary>
        /// Gets or sets the list of SNAPDOG client configurations.
        /// Maps environment variables with pattern: SNAPDOG_CLIENT_X_*
        /// Where X is the client index (1, 2, 3, etc.)
        ///
        /// Example mappings:
        /// - SNAPDOG_CLIENT_1_NAME → Clients[0].Name
        /// - SNAPDOG_CLIENT_1_MAC → Clients[0].Mac
        /// - SNAPDOG_CLIENT_1_MQTT_BASETOPIC → Clients[0].MqttBaseTopic
        /// - SNAPDOG_CLIENT_1_MQTT_VOLUME_SET_TOPIC → Clients[0].Mqtt.VolumeSetTopic
        /// - SNAPDOG_CLIENT_1_KNX_ENABLED → Clients[0].Knx.Enabled
        /// - SNAPDOG_CLIENT_1_KNX_VOLUME → Clients[0].Knx.Volume
        /// </summary>
        [Env(NestedListPrefix = "SNAPDOG_CLIENT_", NestedListSuffix = "_")]
        public List<ClientConfig> Clients { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of SNAPDOG radio station configurations.
        /// Maps environment variables with pattern: SNAPDOG_RADIO_X_*
        /// Where X is the radio station index (1, 2, 3, etc.)
        ///
        /// Example mappings:
        /// - SNAPDOG_RADIO_1_NAME → RadioStations[0].Name
        /// - SNAPDOG_RADIO_1_URL → RadioStations[0].URL
        /// - SNAPDOG_RADIO_2_NAME → RadioStations[1].Name
        /// - SNAPDOG_RADIO_2_URL → RadioStations[1].URL
        /// </summary>
        [Env(NestedListPrefix = "SNAPDOG_RADIO_", NestedListSuffix = "_")]
        public List<RadioStation> RadioStations { get; set; } = [];
    }
}
