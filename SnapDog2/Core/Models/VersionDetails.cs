namespace SnapDog2.Core.Models;

/// <summary>
/// Represents software version information.
/// </summary>
public record VersionDetails
{
    /// <summary>
    /// Gets the application version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; init; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; init; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the version info was generated.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the UTC build date.
    /// </summary>
    public DateTime? BuildDateUtc { get; init; }

    /// <summary>
    /// Gets the Git commit hash.
    /// </summary>
    public string? GitCommit { get; init; }

    /// <summary>
    /// Gets the Git branch name.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Gets the build configuration (Debug, Release).
    /// </summary>
    public string? BuildConfiguration { get; init; }
}
