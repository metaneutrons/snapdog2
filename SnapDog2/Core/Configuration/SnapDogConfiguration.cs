namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Root configuration class for the SnapDog2 application.
/// Maps all environment variables starting with SNAPDOG_ to nested configuration objects.
/// </summary>
public class SnapDogConfiguration
{
    /// <summary>
    /// Basic system configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_SYSTEM_*
    /// </summary>
    [Env(NestedPrefix = "SYSTEM_")]
    public SystemConfig System { get; set; } = new();

    /// <summary>
    /// Telemetry and observability configuration.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_*
    /// </summary>
    [Env(NestedPrefix = "TELEMETRY_")]
    public TelemetryConfig Telemetry { get; set; } = new();

    /// <summary>
    /// API authentication and security configuration.
    /// Maps environment variables with prefix: SNAPDOG_API_*
    /// </summary>
    [Env(NestedPrefix = "API_")]
    public ApiConfig Api { get; set; } = new();

    /// <summary>
    /// External services configuration (Snapcast, MQTT, KNX, Subsonic).
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_*
    /// </summary>
    [Env(NestedPrefix = "SERVICES_")]
    public ServicesConfig Services { get; set; } = new();
}
