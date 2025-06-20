namespace SnapDog2.Api.Configuration;

/// <summary>
/// Configuration for API rate limiting policies and settings.
/// </summary>
public class RateLimitingConfiguration
{
    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default rate limit rules.
    /// </summary>
    public IList<RateLimitRule> DefaultRules { get; set; } = new List<RateLimitRule>();

    /// <summary>
    /// Gets or sets the rate limit rules for specific endpoints.
    /// </summary>
    public IList<RateLimitRule> EndpointRules { get; set; } = new List<RateLimitRule>();

    /// <summary>
    /// Gets or sets the IP whitelist.
    /// </summary>
    public IList<string> IpWhitelist { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the client whitelist.
    /// </summary>
    public IList<string> ClientWhitelist { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the status code to return when rate limit is exceeded.
    /// </summary>
    public int HttpStatusCode { get; set; } = 429;

    /// <summary>
    /// Gets or sets the quota exceeded response message.
    /// </summary>
    public string QuotaExceededMessage { get; set; } = "API rate limit exceeded. Please try again later.";

    /// <summary>
    /// Gets or sets whether to include rate limit headers in responses.
    /// </summary>
    public bool EnableRateLimitHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to stack blocked requests.
    /// </summary>
    public bool StackBlockedRequests { get; set; } = false;

    /// <summary>
    /// Gets or sets the rate limit counter prefix.
    /// </summary>
    public string RateLimitCounterPrefix { get; set; } = "rl";

    /// <summary>
    /// Gets or sets the real IP header name.
    /// </summary>
    public string RealIpHeader { get; set; } = "X-Real-IP";

    /// <summary>
    /// Gets or sets the client ID header name.
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-ClientId";

    /// <summary>
    /// Creates default rate limiting configuration.
    /// </summary>
    /// <returns>Default rate limiting configuration.</returns>
    public static RateLimitingConfiguration CreateDefault()
    {
        return new RateLimitingConfiguration
        {
            Enabled = true,
            DefaultRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100,
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 1000,
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1d",
                    Limit = 10000,
                },
            },
            EndpointRules = new List<RateLimitRule>
            {
                // More restrictive limits for authentication endpoints
                new RateLimitRule
                {
                    Endpoint = "*/system/health",
                    Period = "1m",
                    Limit = 30,
                },
                // Control endpoints have lower limits
                new RateLimitRule
                {
                    Endpoint = "*/audiostreams/*/start",
                    Period = "1m",
                    Limit = 10,
                },
                new RateLimitRule
                {
                    Endpoint = "*/audiostreams/*/stop",
                    Period = "1m",
                    Limit = 10,
                },
                new RateLimitRule
                {
                    Endpoint = "*/zones/*/volume",
                    Period = "1m",
                    Limit = 30,
                },
            },
            IpWhitelist = new List<string> { "127.0.0.1", "::1", "192.168.1.0/24" },
            ClientWhitelist = new List<string> { "admin-client", "monitoring-client" },
            HttpStatusCode = 429,
            QuotaExceededMessage = "API rate limit exceeded. Please try again later.",
            EnableRateLimitHeaders = true,
            StackBlockedRequests = false,
            RateLimitCounterPrefix = "snapdog2_rl",
            RealIpHeader = "X-Real-IP",
            ClientIdHeader = "X-ClientId",
        };
    }
}

/// <summary>
/// Represents a rate limiting rule.
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Gets or sets the endpoint pattern that this rule applies to.
    /// </summary>
    public string Endpoint { get; set; } = "*";

    /// <summary>
    /// Gets or sets the time period for the rate limit (e.g., "1m", "1h", "1d").
    /// </summary>
    public string Period { get; set; } = "1m";

    /// <summary>
    /// Gets or sets the maximum number of requests allowed in the period.
    /// </summary>
    public long Limit { get; set; } = 100;
}
