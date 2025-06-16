using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Configuration for individual Snapcast clients.
/// Enhanced with EnvoyConfig attributes for environment variable mapping.
/// Maps environment variables like SNAPDOG_CLIENT_X_* to properties.
///
/// Examples:
/// - SNAPDOG_CLIENT_1_NAME → Name
/// - SNAPDOG_CLIENT_1_MAC → Mac
/// - SNAPDOG_CLIENT_1_MQTT_BASETOPIC → MqttBaseTopic
/// - SNAPDOG_CLIENT_1_MQTT_VOLUME_SET_TOPIC → Mqtt.VolumeSetTopic
/// - SNAPDOG_CLIENT_1_KNX_ENABLED → Knx.Enabled
/// - SNAPDOG_CLIENT_1_KNX_VOLUME → Knx.Volume
/// </summary>
public class ClientConfiguration
{
    /// <summary>
    /// Gets or sets the display name of the client.
    /// Maps to: SNAPDOG_CLIENT_X_NAME
    /// </summary>
    [Env(Key = "NAME", Default = "")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MAC address of the client.
    /// Maps to: SNAPDOG_CLIENT_X_MAC
    /// </summary>
    [Env(Key = "MAC", Default = "")]
    public string Mac { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base MQTT topic for this client.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_BASETOPIC
    /// </summary>
    [Env(Key = "MQTT_BASETOPIC", Default = "")]
    public string MqttBaseTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default zone for this client.
    /// Maps to: SNAPDOG_CLIENT_X_DEFAULT_ZONE
    /// </summary>
    [Env(Key = "DEFAULT_ZONE", Default = 1)]
    public int DefaultZone { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether this client is enabled.
    /// Maps to: SNAPDOG_CLIENT_X_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the client description.
    /// Maps to: SNAPDOG_CLIENT_X_DESCRIPTION
    /// </summary>
    [Env(Key = "DESCRIPTION", Default = "")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client location/room.
    /// Maps to: SNAPDOG_CLIENT_X_LOCATION
    /// </summary>
    [Env(Key = "LOCATION", Default = "")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT configuration for this client.
    /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_MQTT_*
    ///
    /// Examples:
    /// - SNAPDOG_CLIENT_X_MQTT_VOLUME_SET_TOPIC → Mqtt.VolumeSetTopic
    /// - SNAPDOG_CLIENT_X_MQTT_MUTE_SET_TOPIC → Mqtt.MuteSetTopic
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public MqttClientConfiguration Mqtt { get; set; } = new();

    /// <summary>
    /// Gets or sets the KNX configuration for this client.
    /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_KNX_*
    ///
    /// Examples:
    /// - SNAPDOG_CLIENT_X_KNX_ENABLED → Knx.Enabled
    /// - SNAPDOG_CLIENT_X_KNX_VOLUME → Knx.Volume
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public KnxClientConfiguration Knx { get; set; } = new();
}

/// <summary>
/// MQTT configuration for a client.
/// Enhanced with EnvoyConfig attributes for environment variable mapping.
/// Maps environment variables like SNAPDOG_CLIENT_X_MQTT_* to properties.
/// </summary>
public class MqttClientConfiguration
{
    /// <summary>
    /// Gets or sets the MQTT topic for setting volume.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_VOLUME_SET_TOPIC
    /// </summary>
    [Env(Key = "VOLUME_SET_TOPIC", Default = "")]
    public string VolumeSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for setting mute state.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_MUTE_SET_TOPIC
    /// </summary>
    [Env(Key = "MUTE_SET_TOPIC", Default = "")]
    public string MuteSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for setting latency.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_LATENCY_SET_TOPIC
    /// </summary>
    [Env(Key = "LATENCY_SET_TOPIC", Default = "")]
    public string LatencySetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for setting zone.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_ZONE_SET_TOPIC
    /// </summary>
    [Env(Key = "ZONE_SET_TOPIC", Default = "")]
    public string ZoneSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for control commands.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_CONTROL_TOPIC
    /// </summary>
    [Env(Key = "CONTROL_TOPIC", Default = "")]
    public string ControlTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for connection status.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_CONNECTED_TOPIC
    /// </summary>
    [Env(Key = "CONNECTED_TOPIC", Default = "")]
    public string ConnectedTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for volume status.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_VOLUME_TOPIC
    /// </summary>
    [Env(Key = "VOLUME_TOPIC", Default = "")]
    public string VolumeTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for mute status.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_MUTE_TOPIC
    /// </summary>
    [Env(Key = "MUTE_TOPIC", Default = "")]
    public string MuteTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for latency status.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_LATENCY_TOPIC
    /// </summary>
    [Env(Key = "LATENCY_TOPIC", Default = "")]
    public string LatencyTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for zone status.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_ZONE_TOPIC
    /// </summary>
    [Env(Key = "ZONE_TOPIC", Default = "")]
    public string ZoneTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for general state information.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_STATE_TOPIC
    /// </summary>
    [Env(Key = "STATE_TOPIC", Default = "")]
    public string StateTopic { get; set; } = string.Empty;
}

/// <summary>
/// KNX configuration for a client.
/// Enhanced with EnvoyConfig attributes for environment variable mapping.
/// Maps environment variables like SNAPDOG_CLIENT_X_KNX_* to properties.
/// </summary>
public class KnxClientConfiguration
{
    /// <summary>
    /// Gets or sets whether KNX integration is enabled for this client.
    /// Maps to: SNAPDOG_CLIENT_X_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

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
