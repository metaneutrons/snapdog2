namespace SnapDog2.Api.Configuration;

/// <summary>
/// Configuration class for API authentication settings.
/// Loads API keys from environment variables for secure authentication.
/// </summary>
public class ApiAuthConfiguration
{
    /// <summary>
    /// List of valid API keys for authentication.
    /// </summary>
    public List<string> ApiKeys { get; set; } = new();

    /// <summary>
    /// Loads API authentication configuration from environment variables.
    /// Expected format: SNAPDOG_API_KEYS=key1,key2,key3
    /// </summary>
    /// <returns>Configured API authentication settings.</returns>
    public static ApiAuthConfiguration LoadFromEnvironment()
    {
        var config = new ApiAuthConfiguration();

        // Load API keys from environment variable
        var apiKeysEnv = Environment.GetEnvironmentVariable("SNAPDOG_API_KEYS");
        if (!string.IsNullOrWhiteSpace(apiKeysEnv))
        {
            config.ApiKeys = apiKeysEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(key => key.Trim())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .ToList();
        }

        // If no API keys are configured, add a default development key
        if (config.ApiKeys.Count == 0)
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (isDevelopment)
            {
                config.ApiKeys.Add("dev-api-key-12345");
                Console.WriteLine(
                    "WARNING: Using default development API key. Set SNAPDOG_API_KEYS environment variable for production."
                );
            }
        }

        return config;
    }

    /// <summary>
    /// Validates if the provided API key is valid.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <returns>True if the API key is valid, false otherwise.</returns>
    public bool IsValidApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        return ApiKeys.Contains(apiKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the number of configured API keys.
    /// </summary>
    public int KeyCount => ApiKeys.Count;

    /// <summary>
    /// Indicates whether API authentication is properly configured.
    /// </summary>
    public bool IsConfigured => ApiKeys.Count > 0;
}
