using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// System-level configuration settings for SnapDog2.
/// Maps environment variables with SNAPDOG_SYSTEM_ prefix.
///
/// Examples:
/// - SNAPDOG_SYSTEM_ENVIRONMENT → Environment
/// - SNAPDOG_SYSTEM_LOG_LEVEL → LogLevel
/// - SNAPDOG_SYSTEM_MQTT_BASE_TOPIC → Mqtt.BaseTopic
/// </summary>
public class SystemConfiguration
{
    /// <summary>
    /// Gets or sets the application environment (Development, Production, etc.).
    /// Maps to: SNAPDOG_SYSTEM_ENVIRONMENT
    /// </summary>
    [Env(Key = "ENVIRONMENT", Default = "Development")]
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Gets or sets the minimum log level for the application.
    /// Maps to: SNAPDOG_SYSTEM_LOG_LEVEL
    /// </summary>
    [Env(Key = "LOG_LEVEL", Default = "Information")]
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether debug mode is enabled.
    /// Maps to: SNAPDOG_SYSTEM_DEBUG_ENABLED
    /// </summary>
    [Env(Key = "DEBUG_ENABLED", Default = false)]
    public bool DebugEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the system-wide MQTT configuration.
    /// Maps environment variables with prefix: SNAPDOG_SYSTEM_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public SystemMqttConfiguration Mqtt { get; set; } = new();

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public HealthCheckConfiguration HealthChecks { get; init; } = new();
}

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

/// <summary>
/// Configuration settings for health checks.
/// Controls health check behavior, timeouts, and feature enablement.
/// </summary>
public record HealthCheckConfiguration
{
    /// <summary>
    /// Gets or sets whether health checks are enabled.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_ENABLED", Default = true)]
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the default timeout for health checks.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TIMEOUT
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TIMEOUT", Default = 30)]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for external service health checks.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_EXTERNAL_SERVICE_TIMEOUT
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_EXTERNAL_SERVICE_TIMEOUT", Default = "00:00:10")]
    public TimeSpan ExternalServiceTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the health check tags for categorization.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TAGS
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TAGS", Default = "ready,live")]
    public string[] Tags { get; init; } = ["ready", "live"];
}
