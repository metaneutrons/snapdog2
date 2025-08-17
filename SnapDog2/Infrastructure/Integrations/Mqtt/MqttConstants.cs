namespace SnapDog2.Infrastructure.Integrations.Mqtt;

/// <summary>
/// Constants for MQTT topic structure and commands to eliminate magic strings.
/// </summary>
public static class MqttConstants
{
    /// <summary>
    /// Root topic for all SnapDog MQTT messages.
    /// </summary>
    public const string ROOT_TOPIC = "snapdog";

    /// <summary>
    /// Topic segments for MQTT message structure.
    /// </summary>
    public static class Segments
    {
        public const string CONTROL = "control";
        public const string SET = "set";
    }

    /// <summary>
    /// Entity types supported in MQTT topics.
    /// </summary>
    public static class EntityTypes
    {
        public const string ZONE = "zone";
        public const string CLIENT = "client";
    }

    /// <summary>
    /// MQTT command strings mapped to their string representations.
    /// </summary>
    public static class Commands
    {
        // Playback commands
        public const string PLAY = "play";
        public const string PAUSE = "pause";
        public const string STOP = "stop";

        // Navigation commands
        public const string NEXT = "next";
        public const string TRACK_NEXT = "track_next";
        public const string PREVIOUS = "previous";
        public const string TRACK_PREVIOUS = "track_previous";
        public const string PLAYLIST_NEXT = "playlist_next";
        public const string PLAYLIST_PREVIOUS = "playlist_previous";

        // Volume commands
        public const string VOLUME = "volume";
        public const string VOLUME_UP = "volume_up";
        public const string VOLUME_DOWN = "volume_down";

        // Mute commands
        public const string MUTE_ON = "mute_on";
        public const string MUTE_OFF = "mute_off";
        public const string MUTE_TOGGLE = "mute_toggle";

        // Track repeat commands
        public const string TRACK_REPEAT_ON = "track_repeat_on";
        public const string TRACK_REPEAT_OFF = "track_repeat_off";
        public const string TRACK_REPEAT_TOGGLE = "track_repeat_toggle";

        // Playlist shuffle commands
        public const string SHUFFLE_ON = "shuffle_on";
        public const string SHUFFLE_OFF = "shuffle_off";
        public const string SHUFFLE_TOGGLE = "shuffle_toggle";

        // Playlist repeat commands
        public const string REPEAT_ON = "repeat_on";
        public const string REPEAT_OFF = "repeat_off";
        public const string REPEAT_TOGGLE = "repeat_toggle";

        // Selection commands
        public const string TRACK = "track";
        public const string PLAYLIST = "playlist";

        // Client-specific commands
        public const string ZONE_ASSIGNMENT = "zone";
        public const string LATENCY = "latency";
    }

    /// <summary>
    /// Command parameter prefixes for complex commands.
    /// </summary>
    public static class Parameters
    {
        public const string TRACK_PREFIX = "track";
        public const string URL_PREFIX = "url";
    }
}
