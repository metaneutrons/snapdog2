using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Configuration for KNX integration specific to a SNAPDOG client.
    /// Maps environment variables like SNAPDOG_CLIENT_X_KNX_* to properties.
    /// </summary>
    public class ClientKnxConfig
    {
        /// <summary>
        /// Gets or sets whether KNX integration is enabled for this client.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_ENABLED
        /// </summary>
        [Env(Key = "ENABLED", Default = false)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for volume control.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_VOLUME
        /// </summary>
        [Env(Key = "VOLUME")]
        public KnxAddress? Volume { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for volume status feedback.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_VOLUME_STATUS
        /// </summary>
        [Env(Key = "VOLUME_STATUS")]
        public KnxAddress? VolumeStatus { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for volume up command.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_VOLUME_UP
        /// </summary>
        [Env(Key = "VOLUME_UP")]
        public KnxAddress? VolumeUp { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for volume down command.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_VOLUME_DOWN
        /// </summary>
        [Env(Key = "VOLUME_DOWN")]
        public KnxAddress? VolumeDown { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for mute control.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_MUTE
        /// </summary>
        [Env(Key = "MUTE")]
        public KnxAddress? Mute { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for mute status feedback.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_MUTE_STATUS
        /// </summary>
        [Env(Key = "MUTE_STATUS")]
        public KnxAddress? MuteStatus { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for mute toggle command.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_MUTE_TOGGLE
        /// </summary>
        [Env(Key = "MUTE_TOGGLE")]
        public KnxAddress? MuteToggle { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for latency control.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_LATENCY
        /// </summary>
        [Env(Key = "LATENCY")]
        public KnxAddress? Latency { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for latency status feedback.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_LATENCY_STATUS
        /// </summary>
        [Env(Key = "LATENCY_STATUS")]
        public KnxAddress? LatencyStatus { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for zone control.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_ZONE
        /// </summary>
        [Env(Key = "ZONE")]
        public KnxAddress? Zone { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for zone status feedback.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_ZONE_STATUS
        /// </summary>
        [Env(Key = "ZONE_STATUS")]
        public KnxAddress? ZoneStatus { get; set; }

        /// <summary>
        /// Gets or sets the KNX group address for connection status feedback.
        /// Maps to: SNAPDOG_CLIENT_X_KNX_CONNECTED_STATUS
        /// </summary>
        [Env(Key = "CONNECTED_STATUS")]
        public KnxAddress? ConnectedStatus { get; set; }
    }
}
