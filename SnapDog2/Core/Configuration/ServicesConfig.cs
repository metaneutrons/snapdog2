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
}

/// <summary>
/// Snapcast service configuration.
/// </summary>
public class SnapcastConfig
{
    /// <summary>
    /// Snapcast server address.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_ADDRESS
    /// </summary>
    [Env(Key = "ADDRESS", Default = "localhost")]
    public string Address { get; set; } = "localhost";

    /// <summary>
    /// Snapcast server port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT
    /// </summary>
    [Env(Key = "JSONRPC_PORT", Default = 1705)]
    public int JsonRpcPort { get; set; } = 1705;

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

/// <summary>
/// MQTT service configuration.
/// </summary>
public class MqttConfig
{
    /// <summary>
    /// Whether MQTT integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// MQTT broker address.
    /// Maps to: SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS
    /// </summary>
    [Env(Key = "BROKER_ADDRESS", Default = "localhost")]
    public string BrokerAddress { get; set; } = "localhost";

    /// <summary>
    /// MQTT broker port.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 1883)]
    public int Port { get; set; } = 1883;

    /// <summary>
    /// MQTT client ID.
    /// Maps to: SNAPDOG_SERVICES_MQTT_CLIENT_ID
    /// </summary>
    [Env(Key = "CLIENT_ID", Default = "snapdog-server")]
    public string ClientId { get; set; } = "snapdog-server";

    /// <summary>
    /// Whether SSL is enabled.
    /// Maps to: SNAPDOG_SERVICES_MQTT_SSL_ENABLED
    /// </summary>
    [Env(Key = "SSL_ENABLED", Default = false)]
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// MQTT username.
    /// Maps to: SNAPDOG_SERVICES_MQTT_USERNAME
    /// </summary>
    [Env(Key = "USERNAME")]
    public string? Username { get; set; }

    /// <summary>
    /// MQTT password.
    /// Maps to: SNAPDOG_SERVICES_MQTT_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD")]
    public string? Password { get; set; }

    /// <summary>
    /// MQTT keep alive interval in seconds.
    /// Maps to: SNAPDOG_SERVICES_MQTT_KEEP_ALIVE
    /// </summary>
    [Env(Key = "KEEP_ALIVE", Default = 60)]
    public int KeepAlive { get; set; } = 60;
}

/// <summary>
/// KNX service configuration.
/// </summary>
public class KnxConfig
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

/// <summary>
/// Subsonic service configuration.
/// </summary>
public class SubsonicConfig
{
    /// <summary>
    /// Whether Subsonic integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Subsonic server URL.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_URL
    /// </summary>
    [Env(Key = "URL")]
    public string? Url { get; set; }

    /// <summary>
    /// Subsonic username.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_USERNAME
    /// </summary>
    [Env(Key = "USERNAME")]
    public string? Username { get; set; }

    /// <summary>
    /// Subsonic password.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD")]
    public string? Password { get; set; }

    /// <summary>
    /// Subsonic connection timeout in milliseconds.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 10000)]
    public int Timeout { get; set; } = 10000;
}
