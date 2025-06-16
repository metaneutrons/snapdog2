using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// System-level configuration settings for SnapDog2.
/// Maps environment variables with SNAPDOG_SYSTEM_ prefix.
///
/// Examples:
/// - SNAPDOG_SYSTEM_ENVIRONMENT → Environment
/// - SNAPDOG_SYSTEM_LOG_LEVEL → LogLevel
/// - SNAPDOG_SYSTEM_APPLICATION_NAME → ApplicationName
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
    /// Gets or sets the application name for identification.
    /// Maps to: SNAPDOG_SYSTEM_APPLICATION_NAME
    /// </summary>
    [Env(Key = "APPLICATION_NAME", Default = "SnapDog2")]
    public string ApplicationName { get; set; } = "SnapDog2";

    /// <summary>
    /// Gets or sets the application version.
    /// Maps to: SNAPDOG_SYSTEM_VERSION
    /// </summary>
    [Env(Key = "VERSION", Default = "1.0.0")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets whether debug mode is enabled.
    /// Maps to: SNAPDOG_SYSTEM_DEBUG_ENABLED
    /// </summary>
    [Env(Key = "DEBUG_ENABLED", Default = false)]
    public bool DebugEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the data directory path.
    /// Maps to: SNAPDOG_SYSTEM_DATA_PATH
    /// </summary>
    [Env(Key = "DATA_PATH", Default = "./data")]
    public string DataPath { get; set; } = "./data";

    /// <summary>
    /// Gets or sets the configuration file path.
    /// Maps to: SNAPDOG_SYSTEM_CONFIG_PATH
    /// </summary>
    [Env(Key = "CONFIG_PATH", Default = "./config")]
    public string ConfigPath { get; set; } = "./config";

    /// <summary>
    /// Gets or sets the logs directory path.
    /// Maps to: SNAPDOG_SYSTEM_LOGS_PATH
    /// </summary>
    [Env(Key = "LOGS_PATH", Default = "./logs")]
    public string LogsPath { get; set; } = "./logs";
}
