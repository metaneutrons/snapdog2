namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Service for managing audio zones.
/// </summary>
public interface IZoneManager
{
    /// <summary>
    /// Gets a zone by its ID.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <returns>The zone service if found.</returns>
    Task<Result<IZoneService>> GetZoneAsync(int zoneId);

    /// <summary>
    /// Gets all available zones.
    /// </summary>
    /// <returns>Collection of all zones.</returns>
    Task<Result<IEnumerable<IZoneService>>> GetAllZonesAsync();

    /// <summary>
    /// Checks if a zone exists.
    /// </summary>
    /// <param name="zoneId">The zone ID to check.</param>
    /// <returns>True if the zone exists.</returns>
    Task<bool> ZoneExistsAsync(int zoneId);
}
