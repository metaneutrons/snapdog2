namespace SnapDog2.Core.Configuration;

/// <summary>
/// KNX service configuration.
/// </summary>
public class ServicesKnxConfig
{
    /// <summary>
    /// Whether KNX integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// KNX gateway address.
    /// Maps to: SNAPDOG_SERVICES_KNX_GATEWAY
    /// </summary>
    [Env(Key = "GATEWAY")]
    public string? Gateway { get; set; }

    /// <summary>
    /// KNX gateway port.
    /// Maps to: SNAPDOG_SERVICES_KNX_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 3671)]
    public int Port { get; set; } = 3671;

    /// <summary>
    /// KNX connection timeout in seconds.
    /// Maps to: SNAPDOG_SERVICES_KNX_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 10)]
    public int Timeout { get; set; } = 10;

    /// <summary>
    /// Whether auto-reconnect is enabled.
    /// Maps to: SNAPDOG_SERVICES_KNX_AUTO_RECONNECT
    /// </summary>
    [Env(Key = "AUTO_RECONNECT", Default = true)]
    public bool AutoReconnect { get; set; } = true;
}
