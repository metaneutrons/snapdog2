namespace SnapDog2.Api.Models;

using System.ComponentModel.DataAnnotations;

// ═══════════════════════════════════════════════════════════════════════════════
// COMPLEX REQUESTS - Only when multiple properties are truly needed
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Play media request - supports both direct URLs and playlist tracks.
/// Only remaining multi-property request object.
/// </summary>
public record PlayRequest(string? Url = null, int? Track = null);
