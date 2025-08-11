namespace SnapDog2.Api.Models;

using SnapDog2.Core.Models;

// ═══════════════════════════════════════════════════════════════════════════════
// ULTRA-MODERN RESPONSE DESIGN - Minimal, intuitive, and type-safe
// ═══════════════════════════════════════════════════════════════════════════════

// ═══════════════════════════════════════════════════════════════════════════════
// PRIMITIVE RESPONSES - Direct values for maximum simplicity
// ═══════════════════════════════════════════════════════════════════════════════
// 
// 🎯 PHILOSOPHY: Return the actual value, not a wrapper object
// 
// ✅ GET /zones/1/volume        → 75 (int)
// ✅ GET /zones/1/mute          → false (bool)  
// ✅ GET /zones/1/track         → 3 (int)
// ✅ GET /zones/1/track/repeat  → true (bool)
//
// This eliminates ALL single-property response wrappers!

// ═══════════════════════════════════════════════════════════════════════════════
// COLLECTION RESPONSES - Only when structure adds value
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Paginated collection with metadata.
/// </summary>
public record Page<T>(T[] Items, int Total, int PageSize = 20, int PageNumber = 1)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => PageNumber < TotalPages;
    public bool HasPrevious => PageNumber > 1;
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

// ═══════════════════════════════════════════════════════════════════════════════
// ELIMINATED WRAPPER RESPONSES - Return primitives directly:
// ═══════════════════════════════════════════════════════════════════════════════
//
// ❌ REMOVED: VolumeResponse           → Return: int (0-100)
// ❌ REMOVED: MuteResponse             → Return: bool
// ❌ REMOVED: TrackIndexResponse       → Return: int (1-based)
// ❌ REMOVED: TrackRepeatResponse      → Return: bool
// ❌ REMOVED: PlaylistRepeatResponse   → Return: bool
// ❌ REMOVED: PlaylistShuffleResponse  → Return: bool
// ❌ REMOVED: LatencyResponse          → Return: int (milliseconds)
// ❌ REMOVED: ZoneAssignmentResponse   → Return: int? (zone index)
//
// This eliminates 8 unnecessary wrapper objects and makes responses cleaner!
