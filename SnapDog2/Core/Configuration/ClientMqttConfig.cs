namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// MQTT configuration for a client.
/// </summary>
public class ClientMqttConfig
{
    /// <summary>
    /// Base MQTT topic for this client.
    /// Maps to: SNAPDOG_CLIENT_X_MQTT_BASE_TOPIC
    /// </summary>
    [Env(Key = "BASE_TOPIC")]
    public string? BaseTopic { get; set; }

    // Set topics (commands to client)
    [Env(Key = "VOLUME_SET_TOPIC", Default = "volume/set")]
    public string VolumeSetTopic { get; set; } = "volume/set";

    [Env(Key = "MUTE_SET_TOPIC", Default = "mute/set")]
    public string MuteSetTopic { get; set; } = "mute/set";

    [Env(Key = "LATENCY_SET_TOPIC", Default = "latency/set")]
    public string LatencySetTopic { get; set; } = "latency/set";

    [Env(Key = "ZONE_SET_TOPIC", Default = "zone/set")]
    public string ZoneSetTopic { get; set; } = "zone/set";

    // Status topics (status from client)
    [Env(Key = "CONNECTED_TOPIC", Default = "connected")]
    public string ConnectedTopic { get; set; } = "connected";

    [Env(Key = "VOLUME_TOPIC", Default = "volume")]
    public string VolumeTopic { get; set; } = "volume";

    [Env(Key = "MUTE_TOPIC", Default = "mute")]
    public string MuteTopic { get; set; } = "mute";

    [Env(Key = "LATENCY_TOPIC", Default = "latency")]
    public string LatencyTopic { get; set; } = "latency";

    [Env(Key = "ZONE_TOPIC", Default = "zone")]
    public string ZoneTopic { get; set; } = "zone";

    [Env(Key = "STATE_TOPIC", Default = "state")]
    public string StateTopic { get; set; } = "state";
}
