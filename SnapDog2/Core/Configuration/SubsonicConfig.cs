namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

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

    /// <summary>
    /// Resilience policy configuration for Subsonic operations.
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_SUBSONIC_RESILIENCE_*
    /// </summary>
    [Env(NestedPrefix = "RESILIENCE_")]
    public ResilienceConfig Resilience { get; set; } =
        new()
        {
            Connection = new PolicyConfig
            {
                MaxRetries = 3,
                RetryDelayMs = 1000,
                BackoffType = "Exponential",
                UseJitter = true,
                TimeoutSeconds = 10,
            },
            Operation = new PolicyConfig
            {
                MaxRetries = 2,
                RetryDelayMs = 500,
                BackoffType = "Linear",
                UseJitter = false,
                TimeoutSeconds = 30,
            },
        };
}
