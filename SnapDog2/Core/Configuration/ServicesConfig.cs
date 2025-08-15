namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// External services configuration.
/// </summary>
public class ServicesConfig
{
    /// <summary>
    /// Snapcast integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SNAPCAST_*
    /// </summary>
    [Env(NestedPrefix = "SNAPCAST_")]
    public SnapcastConfig Snapcast { get; set; } = new();

    /// <summary>
    /// MQTT integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public MqttConfig Mqtt { get; set; } = new();

    /// <summary>
    /// KNX integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public KnxConfig Knx { get; set; } = new();

    /// <summary>
    /// Subsonic integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SUBSONIC_*
    /// </summary>
    [Env(NestedPrefix = "SUBSONIC_")]
    public SubsonicConfig Subsonic { get; set; } = new();

    /// <summary>
    /// Global audio configuration for Snapcast and LibVLC.
    /// Maps environment variables with prefix: SNAPDOG_AUDIO_*
    /// </summary>
    [Env(NestedPrefix = "AUDIO_")]
    public AudioConfig Audio { get; set; } = new();
}
