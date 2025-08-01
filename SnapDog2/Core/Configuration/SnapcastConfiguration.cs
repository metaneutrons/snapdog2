namespace SnapDog2.Core.Configuration;

/// <summary>
/// Snapcast server connection configuration.
/// </summary>
public class SnapcastConfiguration
{
    /// <summary>
    /// Gets or sets the Snapcast server hostname or IP address.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_ADDRESS
    /// </summary>
    [Env(Key = "ADDRESS", Default = "localhost")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the Snapcast server port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 1705)]
    public int Port { get; set; } = 1705;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the reconnection interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_RECONNECT_INTERVAL
    /// </summary>
    [Env(Key = "RECONNECT_INTERVAL", Default = 5)]
    public int ReconnectIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether auto-reconnect is enabled.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_AUTO_RECONNECT
    /// </summary>
    [Env(Key = "AUTO_RECONNECT", Default = true)]
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Snapcast integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the reconnection delay in seconds.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_RECONNECT_DELAY
    /// </summary>
    [Env(Key = "RECONNECT_DELAY", Default = 5)]
    public int ReconnectDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of reconnection attempts.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_MAX_RECONNECT_ATTEMPTS
    /// </summary>
    [Env(Key = "MAX_RECONNECT_ATTEMPTS", Default = 3)]
    public int MaxReconnectAttempts { get; set; } = 3;
}
