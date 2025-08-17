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

    [Env(Key = "TRACK_NEXT_TOPIC", Default = "next")]
    public string TrackNextTopic { get; set; } = "next";

    [Env(Key = "TRACK_PREVIOUS_TOPIC", Default = "previous")]
    public string TrackPreviousTopic { get; set; } = "previous";

    [Env(Key = "TRACK_TOPIC", Default = "track")]
    public string TrackTopic { get; set; } = "track";

    [Env(Key = "TRACK_METADATA_TOPIC", Default = "track/info")]
    public string TrackInfoTopic { get; set; } = "track/info";

    // Track repeat topics (aligned with StatusIds)
    [Env(Key = "TRACK_REPEAT_SET_TOPIC", Default = "repeat/track")]
    public string TrackRepeatSetTopic { get; set; } = "repeat/track";

    [Env(Key = "TRACK_REPEAT_TOPIC", Default = "repeat/track")]
    public string TrackRepeatTopic { get; set; } = "repeat/track";

    // Playlist topics
    [Env(Key = "PLAYLIST_SET_TOPIC", Default = "playlist/set")]
    public string PlaylistSetTopic { get; set; } = "playlist/set";

    [Env(Key = "PLAYLIST_NEXT_TOPIC", Default = "playlist/next")]
    public string PlaylistNextTopic { get; set; } = "playlist/next";

    [Env(Key = "PLAYLIST_PREVIOUS_TOPIC", Default = "playlist/previous")]
    public string PlaylistPreviousTopic { get; set; } = "playlist/previous";

    [Env(Key = "PLAYLIST_TOPIC", Default = "playlist")]
    public string PlaylistTopic { get; set; } = "playlist";

    [Env(Key = "PLAYLIST_INFO_TOPIC", Default = "playlist/info")]
    public string PlaylistInfoTopic { get; set; } = "playlist/info";

    // Playlist repeat topics (aligned with StatusIds)
    [Env(Key = "PLAYLIST_REPEAT_SET_TOPIC", Default = "repeat/set")]
    public string PlaylistRepeatSetTopic { get; set; } = "repeat/set";

    [Env(Key = "PLAYLIST_REPEAT_TOPIC", Default = "repeat")]
    public string PlaylistRepeatTopic { get; set; } = "repeat";

    // Playlist shuffle topics (simplified structure)
    [Env(Key = "PLAYLIST_SHUFFLE_SET_TOPIC", Default = "shuffle/set")]
    public string PlaylistShuffleSetTopic { get; set; } = "shuffle/set";

    [Env(Key = "PLAYLIST_SHUFFLE_TOPIC", Default = "shuffle")]
    public string PlaylistShuffleTopic { get; set; } = "shuffle";

    // Volume and mute topics
    [Env(Key = "VOLUME_SET_TOPIC", Default = "volume/set")]
    public string VolumeSetTopic { get; set; } = "volume/set";

    [Env(Key = "VOLUME_UP_TOPIC", Default = "volume/up")]
    public string VolumeUpTopic { get; set; } = "volume/up";

    [Env(Key = "VOLUME_DOWN_TOPIC", Default = "volume/down")]
    public string VolumeDownTopic { get; set; } = "volume/down";

    [Env(Key = "VOLUME_TOPIC", Default = "volume")]
    public string VolumeTopic { get; set; } = "volume";

    [Env(Key = "MUTE_SET_TOPIC", Default = "mute/set")]
    public string MuteSetTopic { get; set; } = "mute/set";

    [Env(Key = "MUTE_TOGGLE_TOPIC", Default = "mute/toggle")]
    public string MuteToggleTopic { get; set; } = "mute/toggle";

    [Env(Key = "MUTE_TOPIC", Default = "mute")]
    public string MuteTopic { get; set; } = "mute";

    // State topic
    [Env(Key = "STATE_TOPIC", Default = "state")]
    public string StateTopic { get; set; } = "state";

    // Response topics for command acknowledgments and errors
    [Env(Key = "ERROR_TOPIC", Default = "error")]
    public string ErrorTopic { get; set; } = "error";

    [Env(Key = "STATUS_TOPIC", Default = "status")]
    public string StatusTopic { get; set; } = "status";
}
