using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Zone configuration for SnapDog2 multi-room audio system.
/// Maps environment variables with SNAPDOG_ZONE_X_ prefix pattern.
///
/// Examples:
/// - SNAPDOG_ZONE_1_NAME → Zones[0].Name
/// - SNAPDOG_ZONE_1_SINK → Zones[0].Sink
/// - SNAPDOG_ZONE_1_MQTT_BASE_TOPIC → Zones[0].MqttBaseTopic
/// </summary>
public class ZoneConfiguration
{
    /// <summary>
    /// Gets or sets the display name of the zone.
    /// Maps to: SNAPDOG_ZONE_X_NAME
    /// </summary>
    [Env(Key = "NAME", Default = "")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Snapcast sink path for this zone.
    /// Maps to: SNAPDOG_ZONE_X_SINK
    /// </summary>
    [Env(Key = "SINK", Default = "")]
    public string Sink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base MQTT topic for this zone.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_BASE_TOPIC
    /// </summary>
    [Env(Key = "MQTT_BASE_TOPIC", Default = "")]
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
    /// Gets or sets the MQTT topic for state control commands.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_STATE_SET_TOPIC
    /// </summary>
    [Env(Key = "STATE_SET_TOPIC", Default = "")]
    public string StateSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for track repeat control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TRACK_REPEAT_SET_TOPIC
    /// </summary>
    [Env(Key = "TRACK_REPEAT_SET_TOPIC", Default = "")]
    public string TrackRepeatSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist repeat control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_REPEAT_SET_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_REPEAT_SET_TOPIC", Default = "")]
    public string PlaylistRepeatSetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist shuffle control.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_SHUFFLE_SET_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_SHUFFLE_SET_TOPIC", Default = "")]
    public string PlaylistShuffleSetTopic { get; set; } = string.Empty;

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
    /// Gets or sets the MQTT topic for detailed track information.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TRACK_INFO_TOPIC
    /// </summary>
    [Env(Key = "TRACK_INFO_TOPIC", Default = "")]
    public string TrackInfoTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for track repeat status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TRACK_REPEAT_TOPIC
    /// </summary>
    [Env(Key = "TRACK_REPEAT_TOPIC", Default = "")]
    public string TrackRepeatTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_TOPIC", Default = "")]
    public string PlaylistTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for detailed playlist information.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_INFO_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_INFO_TOPIC", Default = "")]
    public string PlaylistInfoTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist repeat status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_REPEAT_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_REPEAT_TOPIC", Default = "")]
    public string PlaylistRepeatTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT topic for playlist shuffle status.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_PLAYLIST_SHUFFLE_TOPIC
    /// </summary>
    [Env(Key = "PLAYLIST_SHUFFLE_TOPIC", Default = "")]
    public string PlaylistShuffleTopic { get; set; } = string.Empty;

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
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK_NEXT
    /// </summary>
    [Env(Key = "TRACK_NEXT")]
    public KnxAddress? TrackNext { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for previous track command.
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK_PREVIOUS
    /// </summary>
    [Env(Key = "TRACK_PREVIOUS")]
    public KnxAddress? TrackPrevious { get; set; }

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

    /// <summary>
    /// Gets or sets the KNX group address for track control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK
    /// </summary>
    [Env(Key = "TRACK")]
    public KnxAddress? Track { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for track status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK_STATUS
    /// </summary>
    [Env(Key = "TRACK_STATUS")]
    public KnxAddress? TrackStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for track repeat control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK_REPEAT
    /// </summary>
    [Env(Key = "TRACK_REPEAT")]
    public KnxAddress? TrackRepeat { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for track repeat status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK_REPEAT_STATUS
    /// </summary>
    [Env(Key = "TRACK_REPEAT_STATUS")]
    public KnxAddress? TrackRepeatStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for track repeat toggle.
    /// Maps to: SNAPDOG_ZONE_X_KNX_TRACK_REPEAT_TOGGLE
    /// </summary>
    [Env(Key = "TRACK_REPEAT_TOGGLE")]
    public KnxAddress? TrackRepeatToggle { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for playlist control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PLAYLIST
    /// </summary>
    [Env(Key = "PLAYLIST")]
    public KnxAddress? Playlist { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for playlist status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PLAYLIST_STATUS
    /// </summary>
    [Env(Key = "PLAYLIST_STATUS")]
    public KnxAddress? PlaylistStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for playlist next.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PLAYLIST_NEXT
    /// </summary>
    [Env(Key = "PLAYLIST_NEXT")]
    public KnxAddress? PlaylistNext { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for playlist previous.
    /// Maps to: SNAPDOG_ZONE_X_KNX_PLAYLIST_PREVIOUS
    /// </summary>
    [Env(Key = "PLAYLIST_PREVIOUS")]
    public KnxAddress? PlaylistPrevious { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for shuffle control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_SHUFFLE
    /// </summary>
    [Env(Key = "SHUFFLE")]
    public KnxAddress? Shuffle { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for shuffle status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_SHUFFLE_STATUS
    /// </summary>
    [Env(Key = "SHUFFLE_STATUS")]
    public KnxAddress? ShuffleStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for shuffle toggle.
    /// Maps to: SNAPDOG_ZONE_X_KNX_SHUFFLE_TOGGLE
    /// </summary>
    [Env(Key = "SHUFFLE_TOGGLE")]
    public KnxAddress? ShuffleToggle { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for repeat control.
    /// Maps to: SNAPDOG_ZONE_X_KNX_REPEAT
    /// </summary>
    [Env(Key = "REPEAT")]
    public KnxAddress? Repeat { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for repeat status.
    /// Maps to: SNAPDOG_ZONE_X_KNX_REPEAT_STATUS
    /// </summary>
    [Env(Key = "REPEAT_STATUS")]
    public KnxAddress? RepeatStatus { get; set; }

    /// <summary>
    /// Gets or sets the KNX group address for repeat toggle.
    /// Maps to: SNAPDOG_ZONE_X_KNX_REPEAT_TOGGLE
    /// </summary>
    [Env(Key = "REPEAT_TOGGLE")]
    public KnxAddress? RepeatToggle { get; set; }
}
