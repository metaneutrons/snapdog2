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
    /// Whether debug mode is enabled.
    /// Maps to: SNAPDOG_SYSTEM_DEBUG_ENABLED
    /// </summary>
    [Env(Key = "DEBUG_ENABLED", Default = false)]
    public bool DebugEnabled { get; set; } = false;

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
    /// External service health check timeout in seconds.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_EXTERNAL_SERVICE_TIMEOUT
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_EXTERNAL_SERVICE_TIMEOUT", Default = 10)]
    public int HealthChecksExternalServiceTimeout { get; set; } = 10;

    /// <summary>
    /// Health check tags.
    /// Maps to: SNAPDOG_SYSTEM_HEALTH_CHECKS_TAGS
    /// </summary>
    [Env(Key = "HEALTH_CHECKS_TAGS", Default = "ready,live")]
    public string HealthChecksTags { get; set; } = "ready,live";
}
