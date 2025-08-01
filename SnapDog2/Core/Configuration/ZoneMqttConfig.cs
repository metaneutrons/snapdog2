namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// MQTT configuration for a zone.
/// </summary>
public class ZoneMqttConfig
{
    /// <summary>
    /// Base MQTT topic for this zone.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_BASE_TOPIC
    /// </summary>
    [Env(Key = "BASE_TOPIC")]
    public string? BaseTopic { get; set; }

    // Control topics
    [Env(Key = "CONTROL_SET_TOPIC", Default = "control/set")]
    public string ControlSetTopic { get; set; } = "control/set";

    [Env(Key = "CONTROL_TOPIC", Default = "control")]
    public string ControlTopic { get; set; } = "control";

    // Track topics
    [Env(Key = "TRACK_SET_TOPIC", Default = "track/set")]
    public string TrackSetTopic { get; set; } = "track/set";

    [Env(Key = "TRACK_TOPIC", Default = "track")]
    public string TrackTopic { get; set; } = "track";

    [Env(Key = "TRACK_INFO_TOPIC", Default = "track/info")]
    public string TrackInfoTopic { get; set; } = "track/info";

    [Env(Key = "TRACK_REPEAT_SET_TOPIC", Default = "track_repeat/set")]
    public string TrackRepeatSetTopic { get; set; } = "track_repeat/set";

    [Env(Key = "TRACK_REPEAT_TOPIC", Default = "track_repeat")]
    public string TrackRepeatTopic { get; set; } = "track_repeat";

    // Playlist topics
    [Env(Key = "PLAYLIST_SET_TOPIC", Default = "playlist/set")]
    public string PlaylistSetTopic { get; set; } = "playlist/set";

    [Env(Key = "PLAYLIST_TOPIC", Default = "playlist")]
    public string PlaylistTopic { get; set; } = "playlist";

    [Env(Key = "PLAYLIST_INFO_TOPIC", Default = "playlist/info")]
    public string PlaylistInfoTopic { get; set; } = "playlist/info";

    [Env(Key = "PLAYLIST_REPEAT_SET_TOPIC", Default = "playlist_repeat/set")]
    public string PlaylistRepeatSetTopic { get; set; } = "playlist_repeat/set";

    [Env(Key = "PLAYLIST_REPEAT_TOPIC", Default = "playlist_repeat")]
    public string PlaylistRepeatTopic { get; set; } = "playlist_repeat";

    [Env(Key = "PLAYLIST_SHUFFLE_SET_TOPIC", Default = "playlist_shuffle/set")]
    public string PlaylistShuffleSetTopic { get; set; } = "playlist_shuffle/set";

    [Env(Key = "PLAYLIST_SHUFFLE_TOPIC", Default = "playlist_shuffle")]
    public string PlaylistShuffleTopic { get; set; } = "playlist_shuffle";

    // Volume and mute topics
    [Env(Key = "VOLUME_SET_TOPIC", Default = "volume/set")]
    public string VolumeSetTopic { get; set; } = "volume/set";

    [Env(Key = "VOLUME_TOPIC", Default = "volume")]
    public string VolumeTopic { get; set; } = "volume";

    [Env(Key = "MUTE_SET_TOPIC", Default = "mute/set")]
    public string MuteSetTopic { get; set; } = "mute/set";

    [Env(Key = "MUTE_TOPIC", Default = "mute")]
    public string MuteTopic { get; set; } = "mute";

    // State topic
    [Env(Key = "STATE_TOPIC", Default = "state")]
    public string StateTopic { get; set; } = "state";
}
