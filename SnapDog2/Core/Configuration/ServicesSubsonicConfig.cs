namespace SnapDog2.Core.Configuration;

/// <summary>
/// ServicesSubsonic service configuration.
/// </summary>
public class ServicesSubsonicConfig
{
    /// <summary>
    /// Whether ServicesSubsonic integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// ServicesSubsonic server URL.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_URL
    /// </summary>
    [Env(Key = "URL")]
    public string? Url { get; set; }

    /// <summary>
    /// ServicesSubsonic username.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_USERNAME
    /// </summary>
    [Env(Key = "USERNAME")]
    public string? Username { get; set; }

    /// <summary>
    /// ServicesSubsonic password.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD")]
    public string? Password { get; set; }

    /// <summary>
    /// ServicesSubsonic connection timeout in milliseconds.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 10000)]
    public int Timeout { get; set; } = 10000;
}
