namespace SnapDog2.Core.Configuration;

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
    public ServicesMqttConfig ServicesMqtt { get; set; } = new();

    /// <summary>
    /// KNX integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ServicesKnxConfig ServicesKnx { get; set; } = new();

    /// <summary>
    /// ServicesSubsonic integration configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SUBSONIC_*
    /// </summary>
    [Env(NestedPrefix = "SUBSONIC_")]
    public ServicesSubsonicConfig ServicesSubsonic { get; set; } = new();
}
