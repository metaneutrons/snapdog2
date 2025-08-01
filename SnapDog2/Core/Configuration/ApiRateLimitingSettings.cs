namespace SnapDog2.Core.Configuration;

public partial class ApiConfiguration
{
    /// <summary>
    /// Rate limiting settings for the API.
    /// </summary>
    public class ApiRateLimitingSettings
    {
        public bool Enabled { get; set; } = true;
        public IList<ApiRateLimitRule> DefaultRules { get; set; } = new List<ApiRateLimitRule>();
        public IList<ApiRateLimitRule> EndpointRules { get; set; } = new List<ApiRateLimitRule>();
        public IList<string> IpWhitelist { get; set; } = new List<string>();
        public IList<string> ClientWhitelist { get; set; } = new List<string>();
        public int HttpStatusCode { get; set; } = 429;
        public string QuotaExceededMessage { get; set; } = "API rate limit exceeded. Please try again later.";
        public bool EnableRateLimitHeaders { get; set; } = true;
        public bool StackBlockedRequests { get; set; } = false;
        public string RateLimitCounterPrefix { get; set; } = "snapdog2_rl";
        public string RealIpHeader { get; set; } = "X-Real-IP";
        public string ClientIdHeader { get; set; } = "X-ClientId";
    }
}
