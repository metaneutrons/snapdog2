using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// System-wide MQTT topic configuration.
/// Maps environment variables with SNAPDOG_SYSTEM_MQTT_ prefix.
/// </summary>
public class SystemMqttConfiguration
{
    /// <summary>
    /// Gets or sets the base topic for system-wide MQTT messages.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_BASE_TOPIC
    /// </summary>
    [Env(Key = "BASE_TOPIC", Default = "snapdog")]
    public string BaseTopic { get; set; } = "snapdog";

    /// <summary>
    /// Gets or sets the topic for system status messages.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_STATUS_TOPIC
    /// </summary>
    [Env(Key = "STATUS_TOPIC", Default = "status")]
    public string StatusTopic { get; set; } = "status";

    /// <summary>
    /// Gets or sets the topic for system error messages.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_ERROR_TOPIC
    /// </summary>
    [Env(Key = "ERROR_TOPIC", Default = "error")]
    public string ErrorTopic { get; set; } = "error";

    /// <summary>
    /// Gets or sets the topic for system version information.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_VERSION_TOPIC
    /// </summary>
    [Env(Key = "VERSION_TOPIC", Default = "version")]
    public string VersionTopic { get; set; } = "version";

    /// <summary>
    /// Gets or sets the topic for system statistics.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_STATS_TOPIC
    /// </summary>
    [Env(Key = "STATS_TOPIC", Default = "stats")]
    public string StatsTopic { get; set; } = "stats";
}
