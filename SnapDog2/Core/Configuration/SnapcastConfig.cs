namespace SnapDog2.Core.Configuration;

/// <summary>
/// Snapcast service configuration.
/// </summary>
public class SnapcastConfig
{
    /// <summary>
    /// Whether Snapcast integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Snapcast server address.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_ADDRESS
    /// </summary>
    [Env(Key = "ADDRESS", Default = "localhost")]
    public string Address { get; set; } = "localhost";

    /// <summary>
    /// Snapcast server port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 1705)]
    public int Port { get; set; } = 1705;

    /// <summary>
    /// Connection timeout in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 30)]
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// Reconnect interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_RECONNECT_INTERVAL
    /// </summary>
    [Env(Key = "RECONNECT_INTERVAL", Default = 5)]
    public int ReconnectInterval { get; set; } = 5;

    /// <summary>
    /// Whether auto-reconnect is enabled.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_AUTO_RECONNECT
    /// </summary>
    [Env(Key = "AUTO_RECONNECT", Default = true)]
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Snapcast HTTP port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_HTTP_PORT
    /// </summary>
    [Env(Key = "HTTP_PORT", Default = 1780)]
    public int HttpPort { get; set; } = 1780;

    /// <summary>
    /// Snapcast base URL for reverse proxy support.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_BASE_URL
    /// </summary>
    [Env(Key = "BASE_URL", Default = "")]
    public string BaseUrl { get; set; } = "";
}
