namespace SnapDog2.Core.Models;

/// <summary>
/// Represents detailed error information for system errors.
/// </summary>
public record ErrorDetails
{
    /// <summary>
    /// Gets the UTC timestamp when the error occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the error level (0=Debug, 1=Info, 2=Warning, 3=Error, 4=Critical).
    /// </summary>
    public required int Level { get; init; }

    /// <summary>
    /// Gets the error code identifier.
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets additional context information about the error.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Gets the component or service where the error occurred.
    /// </summary>
    public string? Component { get; init; }
}
