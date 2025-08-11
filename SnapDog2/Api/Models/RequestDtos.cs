namespace SnapDog2.Api.Models;

using System.ComponentModel.DataAnnotations;

// ═══════════════════════════════════════════════════════════════════════════════
// ULTRA-MODERN SIMPLIFIED REQUEST DTOS - Absolute minimum complexity
// ═══════════════════════════════════════════════════════════════════════════════

// ═══════════════════════════════════════════════════════════════════════════════
// COMPLEX REQUESTS - Only when multiple properties are truly needed
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Play media request - supports both direct URLs and playlist tracks.
/// Only remaining multi-property request object.
/// </summary>
public record PlayRequest(string? Url = null, int? Track = null);

// ═══════════════════════════════════════════════════════════════════════════════
// ELIMINATED ALL OTHER REQUEST OBJECTS - Use direct parameter binding:
// ═══════════════════════════════════════════════════════════════════════════════
// 
// ❌ REMOVED: PlaylistRequest      → Use: int playlistIndex (1-based)
// ❌ REMOVED: VolumeSetRequest     → Use: int volume (0-100)
// ❌ REMOVED: MuteSetRequest       → Use: bool muted
// ❌ REMOVED: ModeSetRequest       → Use: bool enabled  
// ❌ REMOVED: SetTrackRequest      → Use: int track (1-based)
// ❌ REMOVED: StepRequest          → Use: int step = 5
// ❌ REMOVED: LatencySetRequest    → Use: int latency (ms)
// ❌ REMOVED: AssignZoneRequest    → Use: int zoneId
// ❌ REMOVED: ZoneAssignmentRequest → Use: int zoneId
// ❌ REMOVED: RenameRequest        → Use: string name
//
// This eliminates 10 unnecessary DTOs and makes the API even more intuitive!
