namespace SnapDog2.Core.Models;

/// <summary>
/// Information about a media source (Subsonic, radio stations, etc.).
/// </summary>
public class MediaSourceInfo
{
    /// <summary>
    /// Unique identifier for the media source.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the media source.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of media source (subsonic, radio, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the media source is currently available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Optional description of the media source.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional URL or endpoint for the media source.
    /// </summary>
    public string? Url { get; set; }
}
