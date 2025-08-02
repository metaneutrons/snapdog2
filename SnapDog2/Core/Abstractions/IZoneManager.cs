namespace SnapDog2.Core.Abstractions;

using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// Gets the state of a specific zone.
    /// </summary>
    /// <param name="zoneId">The zone ID.</param>
    /// <returns>The zone state if found.</returns>
    Task<Result<ZoneState>> GetZoneStateAsync(int zoneId);

    /// <summary>
    /// Gets the states of all zones.
    /// </summary>
    /// <returns>Collection of all zone states.</returns>
    Task<Result<List<ZoneState>>> GetAllZoneStatesAsync();

    /// <summary>
    /// Checks if a zone exists.
    /// </summary>
    /// <param name="zoneId">The zone ID to check.</param>
    /// <returns>True if the zone exists.</returns>
    Task<bool> ZoneExistsAsync(int zoneId);
}
