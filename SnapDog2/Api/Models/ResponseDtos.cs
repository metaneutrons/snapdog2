namespace SnapDog2.Api.Models;

using SnapDog2.Core.Models;

/// <summary>
/// Basic zone information for list endpoints.
/// </summary>
public record ZoneInfo(int Id, string Name, string PlaybackStatus);

/// <summary>
/// Basic client information for list endpoints.
/// </summary>
public record ClientInfo(int Id, string Name, bool Connected, int? ZoneId);

/// <summary>
/// Media source information.
/// </summary>
public record MediaSourceInfo(string Id, string Type, string Name);

/// <summary>
/// Playlist with tracks for detailed endpoints.
/// </summary>
public record PlaylistWithTracks
{
    /// <summary>
    /// Gets or sets the playlist information.
    /// </summary>
    public required PlaylistInfo Info { get; set; }

    /// <summary>
    /// Gets or sets the tracks in the playlist.
    /// </summary>
    public required List<TrackInfo> Tracks { get; set; }
}

/// <summary>
/// Volume response.
/// </summary>
public record VolumeResponse(int Volume);

/// <summary>
/// Mute response.
/// </summary>
public record MuteResponse(bool IsMuted);

/// <summary>
/// Latency response.
/// </summary>
public record LatencyResponse(int Latency);

/// <summary>
/// Zone assignment response.
/// </summary>
public record ZoneAssignmentResponse(int? ZoneId);

/// <summary>
/// Name response.
/// </summary>
public record NameResponse(string Name);

/// <summary>
/// Track repeat response.
/// </summary>
public record TrackRepeatResponse(bool TrackRepeat);

/// <summary>
/// Playlist repeat response.
/// </summary>
public record PlaylistRepeatResponse(bool PlaylistRepeat);

/// <summary>
/// Playlist shuffle response.
/// </summary>
public record PlaylistShuffleResponse(bool PlaylistShuffle);

/// <summary>
/// Paginated response wrapper.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public record PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public required List<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public required PaginationMetadata Pagination { get; set; }
}

/// <summary>
/// Pagination metadata.
/// </summary>
public record PaginationMetadata
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public required int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public required int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public required int TotalPages { get; set; }
}
