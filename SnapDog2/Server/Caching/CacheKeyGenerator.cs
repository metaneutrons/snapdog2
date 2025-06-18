using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SnapDog2.Server.Caching;

/// <summary>
/// Generates consistent cache keys for different types of requests and data.
/// Ensures cache keys are unique, predictable, and follow naming conventions.
/// </summary>
public static class CacheKeyGenerator
{
    private const string KeySeparator = ":";
    private const string ApplicationPrefix = "snapdog2";

    /// <summary>
    /// Generates a cache key for a MediatR request.
    /// </summary>
    /// <typeparam name="T">The request type.</typeparam>
    /// <param name="request">The request instance.</param>
    /// <param name="prefix">Optional prefix for the cache key.</param>
    /// <returns>A unique cache key for the request.</returns>
    public static string GenerateKey<T>(T request, string? prefix = null)
        where T : class
    {
        var requestType = typeof(T);
        var baseKey = GenerateBaseKey(requestType, prefix);

        // For simple requests without parameters, use just the base key
        if (IsSimpleRequest(request))
        {
            return baseKey;
        }

        // For complex requests, include a hash of the request data
        var requestHash = GenerateRequestHash(request);
        return $"{baseKey}{KeySeparator}{requestHash}";
    }

    /// <summary>
    /// Generates a cache key for a specific entity by ID.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="prefix">Optional prefix for the cache key.</param>
    /// <returns>A unique cache key for the entity.</returns>
    public static string GenerateEntityKey(string entityType, string id, string? prefix = null)
    {
        var key = $"{ApplicationPrefix}{KeySeparator}{entityType.ToLowerInvariant()}{KeySeparator}{id}";
        return prefix != null ? $"{prefix}{KeySeparator}{key}" : key;
    }

    /// <summary>
    /// Generates a cache key for a collection of entities with optional filters.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="filters">Optional filters applied to the collection.</param>
    /// <param name="prefix">Optional prefix for the cache key.</param>
    /// <returns>A unique cache key for the filtered entity collection.</returns>
    public static string GenerateCollectionKey(string entityType, object? filters = null, string? prefix = null)
    {
        var key = $"{ApplicationPrefix}{KeySeparator}{entityType.ToLowerInvariant()}{KeySeparator}collection";

        if (filters != null)
        {
            var filtersHash = GenerateRequestHash(filters);
            key = $"{key}{KeySeparator}{filtersHash}";
        }

        return prefix != null ? $"{prefix}{KeySeparator}{key}" : key;
    }

    /// <summary>
    /// Generates a cache key for system status or health information.
    /// </summary>
    /// <param name="statusType">The type of status information.</param>
    /// <param name="prefix">Optional prefix for the cache key.</param>
    /// <returns>A unique cache key for the status information.</returns>
    public static string GenerateSystemKey(string statusType, string? prefix = null)
    {
        var key = $"{ApplicationPrefix}{KeySeparator}system{KeySeparator}{statusType.ToLowerInvariant()}";
        return prefix != null ? $"{prefix}{KeySeparator}{key}" : key;
    }

    /// <summary>
    /// Generates a pattern for cache key matching (used for bulk operations).
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="prefix">Optional prefix for the pattern.</param>
    /// <returns>A pattern that matches all cache keys for the entity type.</returns>
    public static string GeneratePattern(string entityType, string? prefix = null)
    {
        var pattern = $"{ApplicationPrefix}{KeySeparator}{entityType.ToLowerInvariant()}*";
        return prefix != null ? $"{prefix}{KeySeparator}{pattern}" : pattern;
    }

    /// <summary>
    /// Generates a cache key for a custom operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="parameters">Optional parameters for the operation.</param>
    /// <param name="prefix">Optional prefix for the cache key.</param>
    /// <returns>A unique cache key for the custom operation.</returns>
    public static string GenerateCustomKey(string operation, object? parameters = null, string? prefix = null)
    {
        var key = $"{ApplicationPrefix}{KeySeparator}custom{KeySeparator}{operation.ToLowerInvariant()}";

        if (parameters != null)
        {
            var parametersHash = GenerateRequestHash(parameters);
            key = $"{key}{KeySeparator}{parametersHash}";
        }

        return prefix != null ? $"{prefix}{KeySeparator}{key}" : key;
    }

    /// <summary>
    /// Extracts the entity type from a cache key.
    /// </summary>
    /// <param name="cacheKey">The cache key to analyze.</param>
    /// <returns>The entity type if found; otherwise, null.</returns>
    public static string? ExtractEntityType(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            return null;
        }

        var parts = cacheKey.Split(KeySeparator);
        if (parts.Length >= 3 && parts[0] == ApplicationPrefix)
        {
            return parts[1];
        }

        return null;
    }

    /// <summary>
    /// Validates if a cache key follows the expected format.
    /// </summary>
    /// <param name="cacheKey">The cache key to validate.</param>
    /// <returns>True if the cache key is valid; otherwise, false.</returns>
    public static bool IsValidCacheKey(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            return false;
        }

        var parts = cacheKey.Split(KeySeparator);
        return parts.Length >= 2 && parts[0] == ApplicationPrefix;
    }

    /// <summary>
    /// Generates a base cache key for a request type.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="prefix">Optional prefix for the cache key.</param>
    /// <returns>A base cache key for the request type.</returns>
    private static string GenerateBaseKey(Type requestType, string? prefix = null)
    {
        var typeName = requestType.Name.ToLowerInvariant();

        // Remove common suffixes for cleaner keys
        typeName = typeName.Replace("query", "").Replace("command", "").Replace("request", "");

        var key = $"{ApplicationPrefix}{KeySeparator}{typeName}";
        return prefix != null ? $"{prefix}{KeySeparator}{key}" : key;
    }

    /// <summary>
    /// Determines if a request is simple (no parameters that affect caching).
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if the request is simple; otherwise, false.</returns>
    private static bool IsSimpleRequest(object request)
    {
        var type = request.GetType();
        var properties = type.GetProperties();

        // Consider a request simple if it has no public properties or only has default values
        return properties.Length == 0 || properties.All(p => IsDefaultValue(p.GetValue(request)));
    }

    /// <summary>
    /// Checks if a value is the default value for its type.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is the default value; otherwise, false.</returns>
    private static bool IsDefaultValue(object? value)
    {
        if (value == null)
        {
            return true;
        }

        var type = value.GetType();
        if (type.IsValueType)
        {
            return value.Equals(Activator.CreateInstance(type));
        }

        return false;
    }

    /// <summary>
    /// Generates a hash for request data to ensure cache key uniqueness.
    /// </summary>
    /// <param name="request">The request to hash.</param>
    /// <returns>A hash string representing the request data.</returns>
    private static string GenerateRequestHash(object request)
    {
        try
        {
            var json = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false }
            );

            var bytes = Encoding.UTF8.GetBytes(json);
            var hash = SHA256.HashData(bytes);

            // Convert to a shorter, URL-safe string (first 8 bytes)
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
        catch
        {
            // Fallback to object hash code if serialization fails
            return Math.Abs(request.GetHashCode()).ToString("x8");
        }
    }
}
