namespace SnapDog2.Core.Configuration;

/// <summary>
/// Subsonic server configuration.
/// </summary>
public class SubsonicConfiguration
{
    /// <summary>
    /// Gets or sets whether Subsonic integration is enabled.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Subsonic server URL.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_URL
    /// </summary>
    [Env(Key = "URL", Default = "http://localhost:4040")]
    public string ServerUrl { get; set; } = "http://localhost:4040";

    /// <summary>
    /// Gets or sets the Subsonic username.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_USERNAME
    /// </summary>
    [Env(Key = "USERNAME", Default = "")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Subsonic password.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD", Default = "")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds for HTTP requests.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable automatic authentication on startup.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_AUTO_AUTH
    /// </summary>
    [Env(Key = "AUTO_AUTH", Default = true)]
    public bool AutoAuthenticate { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum bitrate for streaming (in kbps).
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_MAX_BITRATE
    /// </summary>
    [Env(Key = "MAX_BITRATE", Default = 192)]
    public int? MaxBitRate { get; set; } = 192;

    /// <summary>
    /// Gets or sets whether to enable SSL/TLS for the connection.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_SSL_ENABLED
    /// </summary>
    [Env(Key = "SSL_ENABLED", Default = false)]
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to verify SSL certificates.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_VERIFY_SSL
    /// </summary>
    [Env(Key = "VERIFY_SSL", Default = true)]
    public bool VerifySslCertificate { get; set; } = true;

    /// <summary>
    /// Gets or sets the API client name.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_CLIENT_NAME
    /// </summary>
    [Env(Key = "CLIENT_NAME", Default = "SnapDog2")]
    public string ClientId { get; set; } = "SnapDog2";

    /// <summary>
    /// Gets or sets the API version to use.
    /// Maps to: SNAPDOG_SERVICES_SUBSONIC_API_VERSION
    /// </summary>
    [Env(Key = "API_VERSION", Default = "1.16.1")]
    public string ApiVersion { get; set; } = "1.16.1";

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ServerUrl)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && TimeoutSeconds > 0
            && Uri.TryCreate(ServerUrl, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Gets the server URL with proper formatting.
    /// </summary>
    public string GetFormattedServerUrl()
    {
        var url = ServerUrl.TrimEnd('/');
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = SslEnabled ? $"https://{url}" : $"http://{url}";
        }
        return url;
    }
}
