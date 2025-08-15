namespace SnapDog2.Api.Models;

using System.ComponentModel.DataAnnotations;

// ═══════════════════════════════════════════════════════════════════════════════
// ZERO REQUEST DTOS - ULTIMATE SIMPLIFICATION ACHIEVED!
// ═══════════════════════════════════════════════════════════════════════════════

// 🎉 NO REQUEST OBJECTS NEEDED!
//
// All API operations now use direct parameter binding:
// - Primitives: int, bool, string
// - Path parameters: /zones/{zoneIndex}/play/track/{trackIndex}
// - Query parameters: ?page=1&size=20
// - Body parameters: [FromBody] int volume, [FromBody] string url
//
// This represents the ultimate in API simplification!

// ═══════════════════════════════════════════════════════════════════════════════
// ELIMINATED ALL REQUEST OBJECTS - 100% reduction achieved:
// ═══════════════════════════════════════════════════════════════════════════════
//
// ❌ REMOVED: PlayRequest          → Use: separate endpoints /play, /play/track/{index}, /play/url
// ❌ REMOVED: PlaylistRequest      → Use: int playlistIndex (1-based)
// ❌ REMOVED: VolumeSetRequest     → Use: int volume (0-100)
// ❌ REMOVED: MuteSetRequest       → Use: bool muted
// ❌ REMOVED: ModeSetRequest       → Use: bool enabled
// ❌ REMOVED: SetTrackRequest      → Use: int track (1-based)
// ❌ REMOVED: StepRequest          → Use: int step = 5
// ❌ REMOVED: LatencySetRequest    → Use: int latency (ms)
// ❌ REMOVED: AssignZoneRequest    → Use: int zoneIndex
// ❌ REMOVED: ZoneAssignmentRequest → Use: int zoneIndex
// ❌ REMOVED: RenameRequest        → Use: string name
//
// TOTAL ELIMINATION: 11 request DTOs → 0 (100% reduction!)
// This is the pinnacle of modern API design!
