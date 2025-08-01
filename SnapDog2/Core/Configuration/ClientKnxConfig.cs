namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// KNX configuration for a client.
/// </summary>
public class ClientKnxConfig
{
    /// <summary>
    /// Whether KNX is enabled for this client.
    /// Maps to: SNAPDOG_CLIENT_X_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    // Volume addresses
    [Env(Key = "VOLUME")]
    public string? Volume { get; set; }

    [Env(Key = "VOLUME_STATUS")]
    public string? VolumeStatus { get; set; }

    [Env(Key = "VOLUME_UP")]
    public string? VolumeUp { get; set; }

    [Env(Key = "VOLUME_DOWN")]
    public string? VolumeDown { get; set; }

    // Mute addresses
    [Env(Key = "MUTE")]
    public string? Mute { get; set; }

    [Env(Key = "MUTE_STATUS")]
    public string? MuteStatus { get; set; }

    [Env(Key = "MUTE_TOGGLE")]
    public string? MuteToggle { get; set; }

    // Other addresses
    [Env(Key = "LATENCY")]
    public string? Latency { get; set; }

    [Env(Key = "ZONE")]
    public string? Zone { get; set; }

    [Env(Key = "CONNECTED_STATUS")]
    public string? ConnectedStatus { get; set; }
}
