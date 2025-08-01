using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// External services configuration settings for SnapDog2.
/// Maps environment variables with SNAPDOG_SERVICES_ prefix.
///
/// Examples:
/// - SNAPDOG_SERVICES_SNAPCAST_ADDRESS → Snapcast.Host
/// - SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS → Mqtt.Broker
/// - SNAPDOG_SERVICES_KNX_GATEWAY → Knx.Gateway
/// - SNAPDOG_SERVICES_SUBSONIC_URL → Subsonic.Url
/// </summary>
public class ServicesConfiguration
{
    /// <summary>
    /// Gets or sets the Snapcast server configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SNAPCAST_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_SNAPCAST_ADDRESS → Snapcast.Host
    /// - SNAPDOG_SERVICES_SNAPCAST_PORT → Snapcast.Port
    /// - SNAPDOG_SERVICES_SNAPCAST_TIMEOUT → Snapcast.TimeoutSeconds
    /// </summary>
    [Env(NestedPrefix = "SNAPCAST_")]
    public SnapcastConfiguration Snapcast { get; set; } = new();

    /// <summary>
    /// Gets or sets the MQTT broker configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_MQTT_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS → Mqtt.Broker
    /// - SNAPDOG_SERVICES_MQTT_PORT → Mqtt.Port
    /// - SNAPDOG_SERVICES_MQTT_USERNAME → Mqtt.Username
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public MqttConfiguration Mqtt { get; set; } = new();

    /// <summary>
    /// Gets or sets the KNX gateway configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_KNX_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_KNX_GATEWAY → Knx.Gateway
    /// - SNAPDOG_SERVICES_KNX_PORT → Knx.Port
    /// - SNAPDOG_SERVICES_KNX_ENABLED → Knx.Enabled
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public KnxConfiguration Knx { get; set; } = new();

    /// <summary>
    /// Gets or sets the Subsonic server configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SUBSONIC_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_SUBSONIC_URL → Subsonic.Url
    /// - SNAPDOG_SERVICES_SUBSONIC_USERNAME → Subsonic.Username
    /// - SNAPDOG_SERVICES_SUBSONIC_PASSWORD → Subsonic.Password
    /// </summary>
    [Env(NestedPrefix = "SUBSONIC_")]
    public SubsonicConfiguration Subsonic { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience policies configuration.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_RESILIENCE_*
    ///
    /// Examples:
    /// - SNAPDOG_SERVICES_RESILIENCE_RETRYATTEMPTS → Resilience.RetryAttempts
    /// - SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERDURATION → Resilience.CircuitBreakerDuration
    /// - SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERTHRESHOLD → Resilience.CircuitBreakerThreshold
    /// </summary>
    [Env(NestedPrefix = "RESILIENCE_")]
    public ResilienceConfiguration Resilience { get; set; } = new();
}

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

/// <summary>
/// MQTT broker configuration.
/// </summary>
public class MqttConfiguration
{
    /// <summary>
    /// Gets or sets the MQTT broker hostname or IP address.
    /// Maps to: SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS
    /// </summary>
    [Env(Key = "BROKER", Default = "localhost")]
    public string Broker { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the MQTT broker port.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 1883)]
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets the MQTT username.
    /// Maps to: SNAPDOG_SERVICES_MQTT_USERNAME
    /// </summary>
    [Env(Key = "USERNAME", Default = "")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT password.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD", Default = "")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT client ID.
    /// Maps to: SNAPDOG_SERVICES_MQTT_CLIENT_ID
    /// </summary>
    [Env(Key = "CLIENT_ID", Default = "snapdog2")]
    public string ClientId { get; set; } = "snapdog2";

    /// <summary>
    /// Gets or sets whether SSL/TLS is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_SSL_ENABLED
    /// </summary>
    [Env(Key = "SSL_ENABLED", Default = false)]
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the keep-alive interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_MQTT_KEEP_ALIVE
    /// </summary>
    [Env(Key = "KEEP_ALIVE", Default = 60)]
    public int KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether MQTT integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the base topic for MQTT messages from system configuration.
    /// This is a temporary property for backward compatibility.
    /// TODO: Update MqttService to use SystemMqttConfiguration directly.
    /// </summary>
    public string BaseTopic { get; set; } = "snapdog";
}

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

/// <summary>
/// Subsonic server configuration.
/// </summary>
public class SubsonicConfiguration
{
    /// <summary>
    /// Gets or sets whether Subsonic integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Subsonic server URL.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_URL
    /// </summary>
    [Env(Key = "URL", Default = "http://localhost:4040")]
    public string ServerUrl { get; set; } = "http://localhost:4040";

    /// <summary>
    /// Gets or sets the Subsonic username.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_USERNAME
    /// </summary>
    [Env(Key = "USERNAME", Default = "")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Subsonic password.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD", Default = "")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds for HTTP requests.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable automatic authentication on startup.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_AUTO_AUTH
    /// </summary>
    [Env(Key = "AUTO_AUTH", Default = true)]
    public bool AutoAuthenticate { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum bitrate for streaming (in kbps).
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_MAX_BITRATE
    /// </summary>
    [Env(Key = "MAX_BITRATE", Default = 192)]
    public int? MaxBitRate { get; set; } = 192;

    /// <summary>
    /// Gets or sets whether to enable SSL/TLS for the connection.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_SSL_ENABLED
    /// </summary>
    [Env(Key = "SSL_ENABLED", Default = false)]
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to verify SSL certificates.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_VERIFY_SSL
    /// </summary>
    [Env(Key = "VERIFY_SSL", Default = true)]
    public bool VerifySslCertificate { get; set; } = true;

    /// <summary>
    /// Gets or sets the API client name.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_CLIENT_NAME
    /// </summary>
    [Env(Key = "CLIENT_NAME", Default = "SnapDog2")]
    public string ClientId { get; set; } = "SnapDog2";

    /// <summary>
    /// Gets or sets the API version to use.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_API_VERSION
    /// </summary>
    [Env(Key = "API_VERSION", Default = "1.16.1")]
    public string ApiVersion { get; set; } = "1.16.1";

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ServerUrl)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && TimeoutSeconds > 0
            && Uri.TryCreate(ServerUrl, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Gets the server URL with proper formatting.
    /// </summary>
    public string GetFormattedServerUrl()
    {
        var url = ServerUrl.TrimEnd('/');
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = SslEnabled ? $"https://{url}" : $"http://{url}";
        }
        return url;
    }
}

/// <summary>
/// Resilience policies configuration for external service calls.
/// </summary>
public class ResilienceConfiguration
{
    /// <summary>
    /// Gets or sets the number of retry attempts for failed operations.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_RETRYATTEMPTS
    /// </summary>
    [Env(Key = "RETRYATTEMPTS", Default = 3)]
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the circuit breaker open duration in seconds.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERDURATION
    /// </summary>
    [Env(Key = "CIRCUITBREAKERDURATION", Default = 30)]
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERTHRESHOLD
    /// </summary>
    [Env(Key = "CIRCUITBREAKERTHRESHOLD", Default = 3)]
    public int CircuitBreakerThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default timeout for operations in seconds.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_DEFAULTTIMEOUT
    /// </summary>
    [Env(Key = "DEFAULTTIMEOUT", Default = 30)]
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the circuit breaker duration as TimeSpan.
    /// </summary>
    public TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(CircuitBreakerDurationSeconds);

    /// <summary>
    /// Gets the default timeout as TimeSpan.
    /// </summary>
    public TimeSpan DefaultTimeout => TimeSpan.FromSeconds(DefaultTimeoutSeconds);
}
