namespace SnapDog2.Core.Configuration;

/// <summary>
/// KNX gateway configuration.
/// </summary>
public class KnxConfiguration
{
    /// <summary>
    /// Gets or sets whether KNX integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the KNX gateway hostname or IP address.
    /// Maps to: SNAPDOG_SERVICES_KNX_GATEWAY
    /// </summary>
    [Env(Key = "GATEWAY", Default = "192.168.1.1")]
    public string Gateway { get; set; } = "192.168.1.1";

    /// <summary>
    /// Gets or sets the KNX gateway port.
    /// Maps to: SNAPDOG_SERVICES_KNX_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 3671)]
    public int Port { get; set; } = 3671;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// Maps to: SNAPDOG_SERVICES_KNX_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 10)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether auto-reconnect is enabled.
    /// Maps to: SNAPDOG_SERVICES_KNX_AUTO_RECONNECT
    /// </summary>
    [Env(Key = "AUTO_RECONNECT", Default = true)]
    public bool AutoReconnect { get; set; } = true;
}
