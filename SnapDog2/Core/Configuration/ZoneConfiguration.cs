using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Zone configuration for SnapDog2 multi-room audio system.
/// Maps environment variables with SNAPDOG_ZONE_X_ prefix pattern.
///
/// Examples:
/// - SNAPDOG_ZONE_1_NAME → Zones[0].Name
/// - SNAPDOG_ZONE_1_DESCRIPTION → Zones[0].Description
/// - SNAPDOG_ZONE_2_ENABLED → Zones[1].Enabled
/// - SNAPDOG_ZONE_1_MQTT_TOPIC → Zones[0].MqttTopic
/// </summary>
public class ZoneConfiguration
{
    /// <summary>
    /// Gets or sets the zone identifier (unique within the system).
    /// Maps to: SNAPDOG_ZONE_X_ID
    /// </summary>
    [Env(Key = "ID", Default = 1)]
    public int Id { get; set; } = 1;

    /// <summary>
    /// Gets or sets the display name of the zone.
    /// Maps to: SNAPDOG_ZONE_X_NAME
    /// </summary>
    [Env(Key = "NAME", Default = "")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zone description.
    /// Maps to: SNAPDOG_ZONE_X_DESCRIPTION
    /// </summary>
    [Env(Key = "DESCRIPTION", Default = "")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the zone is enabled.
    /// Maps to: SNAPDOG_ZONE_X_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the zone icon identifier.
    /// Maps to: SNAPDOG_ZONE_X_ICON
    /// </summary>
    [Env(Key = "ICON", Default = "speaker")]
    public string Icon { get; set; } = "speaker";

    /// <summary>
    /// Gets or sets the zone color (hex code).
    /// Maps to: SNAPDOG_ZONE_X_COLOR
    /// </summary>
    [Env(Key = "COLOR", Default = "#007bff")]
    public string Color { get; set; } = "#007bff";

    /// <summary>
    /// Gets or sets the default volume level for this zone (0-100).
    /// Maps to: SNAPDOG_ZONE_X_DEFAULT_VOLUME
    /// </summary>
    [Env(Key = "DEFAULT_VOLUME", Default = 50)]
    public int DefaultVolume { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum volume level for this zone (0-100).
    /// Maps to: SNAPDOG_ZONE_X_MAX_VOLUME
    /// </summary>
    [Env(Key = "MAX_VOLUME", Default = 100)]
    public int MaxVolume { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum volume level for this zone (0-100).
    /// Maps to: SNAPDOG_ZONE_X_MIN_VOLUME
    /// </summary>
    [Env(Key = "MIN_VOLUME", Default = 0)]
    public int MinVolume { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether the zone starts muted by default.
    /// Maps to: SNAPDOG_ZONE_X_DEFAULT_MUTED
    /// </summary>
    [Env(Key = "DEFAULT_MUTED", Default = false)]
    public bool DefaultMuted { get; set; } = false;

    /// <summary>
    /// Gets or sets the audio delay/latency for this zone in milliseconds.
    /// Maps to: SNAPDOG_ZONE_X_LATENCY
    /// </summary>
    [Env(Key = "LATENCY", Default = 0)]
    public int LatencyMs { get; set; } = 0;

    /// <summary>
    /// Gets or sets the MQTT topic prefix for this zone.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TOPIC
    /// </summary>
    [Env(Key = "MQTT_TOPIC", Default = "")]
    public string MqttTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the physical location/room of this zone.
    /// Maps to: SNAPDOG_ZONE_X_LOCATION
    /// </summary>
    [Env(Key = "LOCATION", Default = "")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zone priority for automatic assignments (higher = more priority).
    /// Maps to: SNAPDOG_ZONE_X_PRIORITY
    /// </summary>
    [Env(Key = "PRIORITY", Default = 1)]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Gets or sets additional zone tags (comma-separated).
    /// Maps to: SNAPDOG_ZONE_X_TAGS
    /// </summary>
    [Env(Key = "TAGS", Default = "")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this zone supports stereo playback.
    /// Maps to: SNAPDOG_ZONE_X_STEREO_ENABLED
    /// </summary>
    [Env(Key = "STEREO_ENABLED", Default = true)]
    public bool StereoEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the audio quality setting for this zone.
    /// Maps to: SNAPDOG_ZONE_X_AUDIO_QUALITY (low, medium, high, lossless)
    /// </summary>
    [Env(Key = "AUDIO_QUALITY", Default = "high")]
    public string AudioQuality { get; set; } = "high";

    /// <summary>
    /// Gets or sets whether zone grouping is allowed.
    /// Maps to: SNAPDOG_ZONE_X_GROUPING_ENABLED
    /// </summary>
    [Env(Key = "GROUPING_ENABLED", Default = true)]
    public bool GroupingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Snapcast sink path for this zone.
    /// Maps to: SNAPDOG_ZONE_X_SINK
    /// </summary>
    [Env(Key = "SINK", Default = "")]
    public string Sink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base MQTT topic for this zone.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_BASETOPIC
    /// </summary>
    [Env(Key = "MQTT_BASETOPIC", Default = "")]
    public string MqttBaseTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public MqttZoneConfiguration Mqtt { get; set; } = new();

    /// <summary>
    /// Gets or sets the KNX configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public KnxZoneConfiguration Knx { get; set; } = new();
}

/// <summary>
/// MQTT configuration for a zone.
/// Maps environment variables like SNAPDOG_ZONE_X_MQTT_* to properties.
/// </summary>
public class MqttZoneConfiguration
{
    /// <summary>
    /// Gets or sets the MQTT topic for control commands.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_CONTROL_SET_TOPIC
    /// </summary>
    [Env(Key = "CONTROL_SET_TOPIC", Default = "")]
    public string ControlSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for track control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TRACK_SET_TOPIC
    /// </summary>
    [Env(Key = "TRACK_SET_TOPIC", Default = "")]
    public string TrackSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_SET_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_SET_TOPIC", Default = "")]
    public string PlaylistSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for volume control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_VOLUME_SET_TOPIC
    /// </summary>
    [Env(Key = "VOLUME_SET_TOPIC", Default = "")]
    public string VolumeSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for mute control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_MUTE_SET_TOPIC
    /// </summary>
    [Env(Key = "MUTE_SET_TOPIC", Default = "")]
    public string MuteSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for control status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_CONTROL_TOPIC
    /// </summary>
    [Env(Key = "CONTROL_TOPIC", Default = "")]
    public string ControlTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for track status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TRACK_TOPIC
    /// </summary>
    [Env(Key = "TRACK_TOPIC", Default = "")]
    public string TrackTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_TOPIC", Default = "")]
    public string PlaylistTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for volume status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_VOLUME_TOPIC
    /// </summary>
    [Env(Key = "VOLUME_TOPIC", Default = "")]
    public string VolumeTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for mute status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_MUTE_TOPIC
    /// </summary>
    [Env(Key = "MUTE_TOPIC", Default = "")]
    public string MuteTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for general state information.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_STATE_TOPIC
    /// </summary>
    [Env(Key = "STATE_TOPIC", Default = "")]
    public string StateTopic { get; set; } = string.Empty;
}

/// <summary>
/// KNX configuration for a zone.
/// Maps environment variables like SNAPDOG_ZONE_X_KNX_* to properties.
/// </summary>
public class KnxZoneConfiguration
{
    /// <summary>
    /// Gets or sets whether KNX integration is enabled for this zone.
    /// Maps to: SNAPDOG_ZONE_X_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the KNX group address for play command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PLAY
    /// </summary>
    [Env(Key = "PLAY")]
    public KnxAddress? Play { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for pause command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PAUSE
    /// </summary>
    [Env(Key = "PAUSE")]
    public KnxAddress? Pause { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for stop command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_STOP
    /// </summary>
    [Env(Key = "STOP")]
    public KnxAddress? Stop { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for control status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_CONTROL_STATUS
    /// </summary>
    [Env(Key = "CONTROL_STATUS")]
    public KnxAddress? ControlStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for next track command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_NEXT
    /// </summary>
    [Env(Key = "NEXT")]
    public KnxAddress? Next { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for previous track command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PREVIOUS
    /// </summary>
    [Env(Key = "PREVIOUS")]
    public KnxAddress? Previous { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for volume control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_VOLUME
    /// </summary>
    [Env(Key = "VOLUME")]
    public KnxAddress? Volume { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for volume status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_VOLUME_STATUS
    /// </summary>
    [Env(Key = "VOLUME_STATUS")]
    public KnxAddress? VolumeStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for volume up command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_VOLUME_UP
    /// </summary>
    [Env(Key = "VOLUME_UP")]
    public KnxAddress? VolumeUp { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for volume down command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_VOLUME_DOWN
    /// </summary>
    [Env(Key = "VOLUME_DOWN")]
    public KnxAddress? VolumeDown { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for mute control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_MUTE
    /// </summary>
    [Env(Key = "MUTE")]
    public KnxAddress? Mute { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for mute status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_MUTE_STATUS
    /// </summary>
    [Env(Key = "MUTE_STATUS")]
    public KnxAddress? MuteStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for mute toggle command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_MUTE_TOGGLE
    /// </summary>
    [Env(Key = "MUTE_TOGGLE")]
    public KnxAddress? MuteToggle { get; set; }
}
