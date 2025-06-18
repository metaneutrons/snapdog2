using System.Text.Json.Serialization;

namespace SnapDog2.Api.Models;

/// <summary>
/// Standard API response wrapper for consistent JSON responses.
/// Provides a uniform structure for all API endpoints with success/error handling.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The response data. Only populated when Success is true.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Error message. Only populated when Success is false.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details for debugging. Only populated when Success is false.
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }

    /// <summary>
    /// Timestamp of the response.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T> { Success = true, Data = data };
    }

    /// <summary>
    /// Creates an error response with a message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="details">Optional error details.</param>
    /// <returns>An error API response.</returns>
    public static ApiResponse<T> Fail(string error, object? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            Details = details,
        };
    }
}

/// <summary>
/// Standard API response wrapper without data payload.
/// Used for operations that don't return data (e.g., delete operations).
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data.
    /// </summary>
    /// <returns>A successful API response.</returns>
    public static ApiResponse Ok()
    {
        return new ApiResponse { Success = true };
    }

    /// <summary>
    /// Creates a successful response with a message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse Ok(string message)
    {
        return new ApiResponse { Success = true, Data = new { message } };
    }

    /// <summary>
    /// Creates an error response with a message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="details">Optional error details.</param>
    /// <returns>An error API response.</returns>
    public new static ApiResponse Fail(string error, object? details = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = error,
            Details = details,
        };
    }
}
