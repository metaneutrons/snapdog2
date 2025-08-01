namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// API authentication and security configuration.
/// </summary>
public class ApiConfig
{
    /// <summary>
    /// Whether API authentication is enabled.
    /// Maps to: SNAPDOG_API_AUTH_ENABLED
    /// </summary>
    [Env(Key = "AUTH_ENABLED", Default = true)]
    public bool AuthEnabled { get; set; } = true;

    /// <summary>
    /// List of API keys for authentication.
    /// Maps environment variables with pattern: SNAPDOG_API_APIKEY_X
    /// Where X is the key index (1, 2, 3, etc.)
    /// </summary>
    [Env(ListPrefix = "APIKEY_")]
    public List<string> ApiKeys { get; set; } = [];
}
