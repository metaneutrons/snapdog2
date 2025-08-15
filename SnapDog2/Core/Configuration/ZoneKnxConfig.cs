namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// KNX configuration for a zone.
/// </summary>
public class ZoneKnxConfig
{
    /// <summary>
    /// Whether KNX is enabled for this zone.
    /// Maps to: SNAPDOG_ZONE_X_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    // Control addresses
    [Env(Key = "PLAY")]
    public string? Play { get; set; }

    [Env(Key = "PAUSE")]
    public string? Pause { get; set; }

    [Env(Key = "STOP")]
    public string? Stop { get; set; }

    [Env(Key = "CONTROL_STATUS")]
    public string? ControlStatus { get; set; }

    // Track addresses
    [Env(Key = "TRACK_NEXT")]
    public string? TrackNext { get; set; }

    [Env(Key = "TRACK_PREVIOUS")]
    public string? TrackPrevious { get; set; }

    [Env(Key = "TRACK")]
    public string? Track { get; set; }

    [Env(Key = "TRACK_STATUS")]
    public string? TrackStatus { get; set; }

    [Env(Key = "TRACK_REPEAT")]
    public string? TrackRepeat { get; set; }

    [Env(Key = "TRACK_REPEAT_STATUS")]
    public string? TrackRepeatStatus { get; set; }

    [Env(Key = "TRACK_REPEAT_TOGGLE")]
    public string? TrackRepeatToggle { get; set; }

    // Playlist addresses
    [Env(Key = "PLAYLIST")]
    public string? Playlist { get; set; }

    [Env(Key = "PLAYLIST_STATUS")]
    public string? PlaylistStatus { get; set; }

    [Env(Key = "PLAYLIST_NEXT")]
    public string? PlaylistNext { get; set; }

    [Env(Key = "PLAYLIST_PREVIOUS")]
    public string? PlaylistPrevious { get; set; }

    [Env(Key = "SHUFFLE")]
    public string? Shuffle { get; set; }

    [Env(Key = "SHUFFLE_TOGGLE")]
    public string? ShuffleToggle { get; set; }

    [Env(Key = "SHUFFLE_STATUS")]
    public string? ShuffleStatus { get; set; }

    [Env(Key = "REPEAT")]
    public string? Repeat { get; set; }

    [Env(Key = "REPEAT_TOGGLE")]
    public string? RepeatToggle { get; set; }

    [Env(Key = "REPEAT_STATUS")]
    public string? RepeatStatus { get; set; }

    // Volume addresses
    [Env(Key = "VOLUME")]
    public string? Volume { get; set; }

    [Env(Key = "VOLUME_UP")]
    public string? VolumeUp { get; set; }

    [Env(Key = "VOLUME_DOWN")]
    public string? VolumeDown { get; set; }

    [Env(Key = "VOLUME_STATUS")]
    public string? VolumeStatus { get; set; }

    [Env(Key = "MUTE")]
    public string? Mute { get; set; }

    [Env(Key = "MUTE_TOGGLE")]
    public string? MuteToggle { get; set; }

    [Env(Key = "MUTE_STATUS")]
    public string? MuteStatus { get; set; }
}
