namespace SnapDog2.Api.Models;

using SnapDog2.Core.Models;

/// <summary>
/// Paginated collection with metadata.
/// </summary>
public record Page<T>(T[] Items, int Total, int PageSize = 20, int PageNumber = 1)
{
    public int TotalPages => (int)Math.Ceiling((double)this.Total / this.PageSize);
    public bool HasNext => this.PageNumber < this.TotalPages;
    public bool HasPrevious => this.PageNumber > 1;
}

/// <summary>
/// Zone summary for listings.
/// </summary>
public record Zone(string Name, int Index, bool Active, string Status);

/// <summary>
/// Client summary for listings.
/// </summary>
public record Client(int Id, string Name, bool Connected, int? Zone = null);

/// <summary>
/// Playlist with tracks for detailed endpoints.
/// </summary>
public record PlaylistWithTracks(PlaylistInfo Info, List<TrackInfo> Tracks);
