namespace SnapDog2.Api.Models;

using System.Diagnostics;

/// <summary>
/// Standard API response wrapper for all endpoints.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response data. Null on error.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the error information. Null on success.
    /// </summary>
    public ApiError? Error { get; set; }

    /// <summary>
    /// Gets or sets the request ID for tracing.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="requestId">Optional request ID.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> CreateSuccess(T data, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Error = null,
            RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional error details.</param>
    /// <param name="requestId">Optional request ID.</param>
    /// <returns>An error API response.</returns>
    public static ApiResponse<T> CreateError(
        string code,
        string message,
        object? details = null,
        string? requestId = null
    )
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details,
            },
            RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        };
    }
}

/// <summary>
/// Non-generic API response for operations without return data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data.
    /// </summary>
    /// <param name="requestId">Optional request ID.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse CreateSuccess(string? requestId = null)
    {
        return new ApiResponse
        {
            Success = true,
            Data = null,
            Error = null,
            RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        };
    }

    /// <summary>
    /// Creates an error response without data.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional error details.</param>
    /// <param name="requestId">Optional request ID.</param>
    /// <returns>An error API response.</returns>
    public static new ApiResponse CreateError(
        string code,
        string message,
        object? details = null,
        string? requestId = null
    )
    {
        return new ApiResponse
        {
            Success = false,
            Data = null,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details,
            },
            RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        };
    }
}

/// <summary>
/// API error details.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error details.
    /// </summary>
    public object? Details { get; set; }
}
