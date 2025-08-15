namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Basic system configuration settings.
/// </summary>
public class SystemConfig
{
    /// <summary>
    /// Logging level for the application.
    /// Maps to: SNAPDOG_SYSTEM_LOG_LEVEL
    /// </summary>
    [Env(Key = "LOG_LEVEL", Default = "Information")]
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Application environment (Development, Staging, Production).
    /// Maps to: SNAPDOG_SYSTEM_ENVIRONMENT
    /// </summary>
    [Env(Key = "ENVIRONMENT", Default = "Development")]
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Whether health checks are enabled.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_ENABLED", Default = true)]
    public bool HealthChecksEnabled { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TIMEOUT
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TIMEOUT", Default = 30)]
    public int HealthChecksTimeout { get; set; } = 30;

    /// <summary>
    /// Health check tags.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TAGS
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TAGS", Default = "ready,live")]
    public string HealthChecksTags { get; set; } = "ready,live";

    /// <summary>
    /// System-wide MQTT base topic.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_BASE_TOPIC
    /// </summary>
    [Env(Key = "MQTT_BASE_TOPIC", Default = "snapdog")]
    public string MqttBaseTopic { get; set; } = "snapdog";

    /// <summary>
    /// System-wide MQTT status topic.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_STATUS_TOPIC
    /// </summary>
    [Env(Key = "MQTT_STATUS_TOPIC", Default = "status")]
    public string MqttStatusTopic { get; set; } = "status";

    /// <summary>
    /// System-wide MQTT error topic.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_ERROR_TOPIC
    /// </summary>
    [Env(Key = "MQTT_ERROR_TOPIC", Default = "error")]
    public string MqttErrorTopic { get; set; } = "error";

    /// <summary>
    /// System-wide MQTT version topic.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_VERSION_TOPIC
    /// </summary>
    [Env(Key = "MQTT_VERSION_TOPIC", Default = "version")]
    public string MqttVersionTopic { get; set; } = "version";

    /// <summary>
    /// System-wide MQTT zones topic.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_ZONES_TOPIC
    /// </summary>
    [Env(Key = "MQTT_ZONES_TOPIC", Default = "system/zones")]
    public string MqttZonesTopic { get; set; } = "system/zones";

    /// <summary>
    /// System-wide MQTT stats topic.
    /// Maps to: SNAPDOG_SYSTEM_MQTT_STATS_TOPIC
    /// </summary>
    [Env(Key = "MQTT_STATS_TOPIC", Default = "stats")]
    public string MqttStatsTopic { get; set; } = "stats";

    /// <summary>
    /// Optional log file path. If not set, file logging is disabled.
    /// Maps to: SNAPDOG_SYSTEM_LOG_FILE
    /// </summary>
    [Env(Key = "LOG_FILE")]
    public string? LogFile { get; set; }
}
