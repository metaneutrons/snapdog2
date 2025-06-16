using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample;

public class SampleZoneMqttConfig
{
    [Env(Key = "BASETOPIC")]
    public string BaseTopic { get; set; } = null!;

    [Env(Key = "CONTROL_SET_TOPIC")]
    public string ControlSetTopic { get; set; } = null!;

    [Env(Key = "TRACK_SET_TOPIC")]
    public string TrackSetTopic { get; set; } = null!;

    [Env(Key = "PLAYLIST_SET_TOPIC")]
    public string PlaylistSetTopic { get; set; } = null!;

    [Env(Key = "VOLUME_SET_TOPIC")]
    public string VolumeSetTopic { get; set; } = null!;

    [Env(Key = "MUTE_SET_TOPIC")]
    public string MuteSetTopic { get; set; } = null!;

    [Env(Key = "CONTROL_TOPIC")]
    public string ControlTopic { get; set; } = null!;

    [Env(Key = "TRACK_TOPIC")]
    public string TrackTopic { get; set; } = null!;

    [Env(Key = "PLAYLIST_TOPIC")]
    public string PlaylistTopic { get; set; } = null!;

    [Env(Key = "VOLUME_TOPIC")]
    public string VolumeTopic { get; set; } = null!;

    [Env(Key = "MUTE_TOPIC")]
    public string MuteTopic { get; set; } = null!;

    [Env(Key = "STATE_TOPIC")]
    public string StateTopic { get; set; } = null!;
}
