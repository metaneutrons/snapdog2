using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapDog2.Api.Configuration;

namespace SnapDog2.Api.Middleware;

/// <summary>
/// Custom rate limiting middleware with enhanced features for the SnapDog2 API.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingConfiguration _config;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The rate limiting configuration.</param>
    /// <param name="cache">The memory cache instance.</param>
    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitingConfiguration config,
        IMemoryCache cache
    )
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting if disabled
        if (!_config.Enabled)
        {
            await _next(context);
            return;
        }

        // Get client identifier
        var clientId = GetClientIdentifier(context);

        // Check if client is whitelisted
        if (IsWhitelisted(context, clientId))
        {
            _logger.LogDebug("Client {ClientId} is whitelisted, skipping rate limiting", clientId);
            await _next(context);
            return;
        }

        // Check rate limits
        var rateLimitResult = await CheckRateLimitAsync(context, clientId);

        if (!rateLimitResult.IsAllowed)
        {
            await ReturnRateLimitExceededResponse(context, rateLimitResult);
            return;
        }

        // Add rate limit headers
        if (_config.EnableRateLimitHeaders)
        {
            AddRateLimitHeaders(context, rateLimitResult);
        }

        await _next(context);
    }

    /// <summary>
    /// Gets the client identifier from the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client identifier.</returns>
    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get client ID from header first
        if (context.Request.Headers.TryGetValue(_config.ClientIdHeader, out var clientIdHeader))
        {
            return clientIdHeader.ToString();
        }

        // Fall back to IP address
        var ipAddress = GetClientIpAddress(context);
        return $"ip:{ipAddress}";
    }

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client IP address.</returns>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check for real IP header first
        if (context.Request.Headers.TryGetValue(_config.RealIpHeader, out var realIpHeader))
        {
            return realIpHeader.ToString();
        }

        // Check for X-Forwarded-For header
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedHeader))
        {
            var forwardedIps = forwardedHeader.ToString().Split(',');
            if (forwardedIps.Length > 0)
            {
                return forwardedIps[0].Trim();
            }
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Checks if the client is whitelisted.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>True if the client is whitelisted; otherwise, false.</returns>
    private bool IsWhitelisted(HttpContext context, string clientId)
    {
        // Check client whitelist
        if (clientId.StartsWith("ip:"))
        {
            var ipAddress = clientId.Substring(3);
            return _config.IpWhitelist.Any(ip => IsIpInRange(ipAddress, ip));
        }
        else
        {
            return _config.ClientWhitelist.Contains(clientId);
        }
    }

    /// <summary>
    /// Checks if an IP address is in the specified range.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="range">The IP range or single IP.</param>
    /// <returns>True if the IP is in range; otherwise, false.</returns>
    private bool IsIpInRange(string ipAddress, string range)
    {
        _logger.LogDebug("DIAGNOSTIC: Checking IP range - IP: '{IpAddress}', Range: '{Range}'", ipAddress, range);

        if (string.Equals(ipAddress, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("DIAGNOSTIC: Cannot check IP range for 'unknown' IP address - returning false");
            return false;
        }

        try
        {
            if (!range.Contains('/'))
            {
                _logger.LogDebug(
                    "DIAGNOSTIC: Single IP comparison - IP: '{IpAddress}' == Range: '{Range}' = {Result}",
                    ipAddress,
                    range,
                    ipAddress == range
                );
                return ipAddress == range;
            }

            var parts = range.Split('/');
            var networkIp = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            _logger.LogDebug("DIAGNOSTIC: About to parse request IP: '{IpAddress}'", ipAddress);
            var requestIp = IPAddress.Parse(ipAddress);

            var result = IsInSubnet(requestIp, networkIp, prefixLength);
            _logger.LogDebug("DIAGNOSTIC: Subnet check result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DIAGNOSTIC: Error checking IP range {Range} for IP {IpAddress}", range, ipAddress);
            return false;
        }
    }

    /// <summary>
    /// Checks if an IP address is in a subnet.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="networkIp">The network IP address.</param>
    /// <param name="prefixLength">The subnet prefix length.</param>
    /// <returns>True if the IP is in the subnet; otherwise, false.</returns>
    private bool IsInSubnet(IPAddress ipAddress, IPAddress networkIp, int prefixLength)
    {
        if (ipAddress.AddressFamily != networkIp.AddressFamily)
            return false;

        var ipBytes = ipAddress.GetAddressBytes();
        var networkBytes = networkIp.GetAddressBytes();

        var bytesToCheck = prefixLength / 8;
        var bitsToCheck = prefixLength % 8;

        for (int i = 0; i < bytesToCheck; i++)
        {
            if (ipBytes[i] != networkBytes[i])
                return false;
        }

        if (bitsToCheck > 0)
        {
            var mask = (byte)(0xFF << (8 - bitsToCheck));
            return (ipBytes[bytesToCheck] & mask) == (networkBytes[bytesToCheck] & mask);
        }

        return true;
    }

    /// <summary>
    /// Checks the rate limit for the client and endpoint.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The rate limit check result.</returns>
    private async Task<RateLimitResult> CheckRateLimitAsync(HttpContext context, string clientId)
    {
        var endpoint = GetEndpointKey(context);
        var result = new RateLimitResult { IsAllowed = true };

        // Check applicable rules
        var rules = GetApplicableRules(endpoint);

        foreach (var rule in rules)
        {
            var key = GenerateKey(clientId, endpoint, rule.Period);
            var limit = rule.Limit;
            var period = ParsePeriod(rule.Period);

            var currentCount = await GetCurrentCountAsync(key);

            if (currentCount >= limit)
            {
                result.IsAllowed = false;
                result.Rule = rule;
                result.Limit = (int)limit;
                result.Remaining = 0;
                result.Reset = DateTime.UtcNow.Add(period);

                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. Limit: {Limit}, Current: {Current}",
                    clientId,
                    endpoint,
                    limit,
                    currentCount
                );

                return result;
            }

            // Update counters
            await IncrementCountAsync(key, period);

            // Update result with the most restrictive remaining count
            var remaining = limit - currentCount - 1;
            if (result.Remaining == null || remaining < result.Remaining)
            {
                result.Remaining = (int)remaining;
                result.Limit = (int)limit;
                result.Reset = DateTime.UtcNow.Add(period);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the endpoint key for rate limiting.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The endpoint key.</returns>
    private string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method.ToUpperInvariant();
        return $"{method}:{path}";
    }

    /// <summary>
    /// Gets the applicable rate limit rules for an endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint key.</param>
    /// <returns>The applicable rules.</returns>
    private IEnumerable<RateLimitRule> GetApplicableRules(string endpoint)
    {
        var rules = new List<RateLimitRule>();

        // Add default rules
        rules.AddRange(_config.DefaultRules);

        // Add specific endpoint rules
        foreach (var rule in _config.EndpointRules)
        {
            if (IsEndpointMatch(endpoint, rule.Endpoint))
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    /// <summary>
    /// Checks if an endpoint matches a rule pattern.
    /// </summary>
    /// <param name="endpoint">The endpoint to check.</param>
    /// <param name="pattern">The rule pattern.</param>
    /// <returns>True if the endpoint matches; otherwise, false.</returns>
    private bool IsEndpointMatch(string endpoint, string pattern)
    {
        if (pattern == "*")
            return true;

        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(endpoint, pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a cache key for rate limiting.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="endpoint">The endpoint key.</param>
    /// <param name="period">The period string.</param>
    /// <returns>The cache key.</returns>
    private string GenerateKey(string clientId, string endpoint, string period)
    {
        var timestamp = GetPeriodTimestamp(period);
        return $"{_config.RateLimitCounterPrefix}:{clientId}:{endpoint}:{period}:{timestamp}";
    }

    /// <summary>
    /// Gets the current timestamp for a period.
    /// </summary>
    /// <param name="period">The period string.</param>
    /// <returns>The timestamp.</returns>
    private long GetPeriodTimestamp(string period)
    {
        var now = DateTime.UtcNow;

        return period.ToLowerInvariant() switch
        {
            var p when p.EndsWith("s") => now.Ticks / TimeSpan.TicksPerSecond,
            var p when p.EndsWith("m") => now.Ticks / TimeSpan.TicksPerMinute,
            var p when p.EndsWith("h") => now.Ticks / TimeSpan.TicksPerHour,
            var p when p.EndsWith("d") => now.Ticks / TimeSpan.TicksPerDay,
            _ => now.Ticks / TimeSpan.TicksPerMinute,
        };
    }

    /// <summary>
    /// Parses a period string to TimeSpan.
    /// </summary>
    /// <param name="period">The period string.</param>
    /// <returns>The TimeSpan.</returns>
    private TimeSpan ParsePeriod(string period)
    {
        var periodLower = period.ToLowerInvariant();

        if (periodLower.EndsWith("s"))
        {
            var seconds = int.Parse(periodLower.Substring(0, periodLower.Length - 1));
            return TimeSpan.FromSeconds(seconds);
        }

        if (periodLower.EndsWith("m"))
        {
            var minutes = int.Parse(periodLower.Substring(0, periodLower.Length - 1));
            return TimeSpan.FromMinutes(minutes);
        }

        if (periodLower.EndsWith("h"))
        {
            var hours = int.Parse(periodLower.Substring(0, periodLower.Length - 1));
            return TimeSpan.FromHours(hours);
        }

        if (periodLower.EndsWith("d"))
        {
            var days = int.Parse(periodLower.Substring(0, periodLower.Length - 1));
            return TimeSpan.FromDays(days);
        }

        return TimeSpan.FromMinutes(1); // Default fallback
    }

    /// <summary>
    /// Gets the current count for a rate limit key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The current count.</returns>
    private async Task<int> GetCurrentCountAsync(string key)
    {
        return await Task.FromResult(_cache.Get<int>(key));
    }

    /// <summary>
    /// Increments the count for a rate limit key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="expiration">The expiration time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task IncrementCountAsync(string key, TimeSpan expiration)
    {
        var currentCount = _cache.Get<int>(key);
        var newCount = currentCount + 1;

        _cache.Set(key, newCount, expiration);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Adds rate limit headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="result">The rate limit result.</param>
    private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        if (result.Limit.HasValue)
        {
            context.Response.Headers.Append("X-RateLimit-Limit", result.Limit.Value.ToString());
        }

        if (result.Remaining.HasValue)
        {
            context.Response.Headers.Append("X-RateLimit-Remaining", result.Remaining.Value.ToString());
        }

        if (result.Reset.HasValue)
        {
            var resetUnixTime = ((DateTimeOffset)result.Reset.Value).ToUnixTimeSeconds();
            context.Response.Headers.Append("X-RateLimit-Reset", resetUnixTime.ToString());
        }
    }

    /// <summary>
    /// Returns a rate limit exceeded response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="result">The rate limit result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ReturnRateLimitExceededResponse(HttpContext context, RateLimitResult result)
    {
        context.Response.StatusCode = _config.HttpStatusCode;
        context.Response.ContentType = "application/json";

        // Add rate limit headers
        if (_config.EnableRateLimitHeaders)
        {
            AddRateLimitHeaders(context, result);
        }

        var response = new
        {
            error = "rate_limit_exceeded",
            message = _config.QuotaExceededMessage,
            limit = result.Limit,
            remaining = result.Remaining,
            reset = result.Reset?.ToString("O"),
        };

        var json = System.Text.Json.JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Represents the result of a rate limit check.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the rule that was violated (if any).
    /// </summary>
    public RateLimitRule? Rule { get; set; }

    /// <summary>
    /// Gets or sets the rate limit.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets the remaining requests.
    /// </summary>
    public int? Remaining { get; set; }

    /// <summary>
    /// Gets or sets the reset time.
    /// </summary>
    public DateTime? Reset { get; set; }
}
