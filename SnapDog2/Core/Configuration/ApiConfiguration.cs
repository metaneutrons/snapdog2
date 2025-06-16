using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// API configuration settings for SnapDog2.
/// Maps environment variables with SNAPDOG_API_ prefix.
///
/// Examples:
/// - SNAPDOG_API_PORT → Port
/// - SNAPDOG_API_HTTPS_ENABLED → HttpsEnabled
/// - SNAPDOG_API_AUTH_ENABLED → AuthEnabled
/// - SNAPDOG_API_APIKEY_1 → ApiKeys[0]
/// </summary>
public class ApiConfiguration
{
    /// <summary>
    /// Gets or sets the API listening port.
    /// Maps to: SNAPDOG_API_PORT
    /// </summary>
    [Env(Key = "PORT", Default = 5000)]
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether HTTPS is enabled.
    /// Maps to: SNAPDOG_API_HTTPS_ENABLED
    /// </summary>
    [Env(Key = "HTTPS_ENABLED", Default = false)]
    public bool HttpsEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the HTTPS port.
    /// Maps to: SNAPDOG_API_HTTPS_PORT
    /// </summary>
    [Env(Key = "HTTPS_PORT", Default = 5001)]
    public int HttpsPort { get; set; } = 5001;

    /// <summary>
    /// Gets or sets the SSL certificate path.
    /// Maps to: SNAPDOG_API_SSL_CERT_PATH
    /// </summary>
    [Env(Key = "SSL_CERT_PATH", Default = "")]
    public string SslCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SSL certificate password.
    /// Maps to: SNAPDOG_API_SSL_CERT_PASSWORD
    /// </summary>
    [Env(Key = "SSL_CERT_PASSWORD", Default = "")]
    public string SslCertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether API authentication is enabled.
    /// Maps to: SNAPDOG_API_AUTH_ENABLED
    /// </summary>
    [Env(Key = "AUTH_ENABLED", Default = false)]
    public bool AuthEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the primary API key for authentication (backward compatibility).
    /// Maps to: SNAPDOG_API_APIKEY
    /// </summary>
    [Env(Key = "APIKEY", Default = "")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of valid API keys for authentication.
    /// Maps environment variables with pattern: SNAPDOG_API_APIKEY_X
    /// Where X is the key index (1, 2, 3, etc.)
    ///
    /// Examples:
    /// - SNAPDOG_API_APIKEY_1 → ApiKeys[0]
    /// - SNAPDOG_API_APIKEY_2 → ApiKeys[1]
    /// </summary>
    [Env(ListPrefix = "APIKEY_")]
    public List<string> ApiKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets whether CORS is enabled.
    /// Maps to: SNAPDOG_API_CORS_ENABLED
    /// </summary>
    [Env(Key = "CORS_ENABLED", Default = true)]
    public bool CorsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the allowed CORS origins.
    /// Maps to: SNAPDOG_API_CORS_ORIGINS (comma-separated list)
    /// </summary>
    [Env(Key = "CORS_ORIGINS", Default = "*")]
    public string CorsOrigins { get; set; } = "*";

    /// <summary>
    /// Gets or sets the API request timeout in seconds.
    /// Maps to: SNAPDOG_API_REQUEST_TIMEOUT
    /// </summary>
    [Env(Key = "REQUEST_TIMEOUT", Default = 30)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum request body size in bytes.
    /// Maps to: SNAPDOG_API_MAX_REQUEST_SIZE
    /// </summary>
    [Env(Key = "MAX_REQUEST_SIZE", Default = 10485760)] // 10MB
    public long MaxRequestBodySize { get; set; } = 10485760;

    /// <summary>
    /// Gets or sets whether API rate limiting is enabled.
    /// Maps to: SNAPDOG_API_RATE_LIMIT_ENABLED
    /// </summary>
    [Env(Key = "RATE_LIMIT_ENABLED", Default = true)]
    public bool RateLimitEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the rate limit requests per minute.
    /// Maps to: SNAPDOG_API_RATE_LIMIT_RPM
    /// </summary>
    [Env(Key = "RATE_LIMIT_RPM", Default = 100)]
    public int RateLimitRequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether API documentation (Swagger) is enabled.
    /// Maps to: SNAPDOG_API_SWAGGER_ENABLED
    /// </summary>
    [Env(Key = "SWAGGER_ENABLED", Default = true)]
    public bool SwaggerEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Swagger UI endpoint path.
    /// Maps to: SNAPDOG_API_SWAGGER_PATH
    /// </summary>
    [Env(Key = "SWAGGER_PATH", Default = "/swagger")]
    public string SwaggerPath { get; set; } = "/swagger";
}
