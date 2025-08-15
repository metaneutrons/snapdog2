namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Interface for persisting zone states across requests.
/// </summary>
public interface IZoneStateStore
{
    /// <summary>
    /// Gets the current state for a zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <returns>Zone state or null if not found</returns>
    ZoneState? GetZoneState(int zoneIndex);

    /// <summary>
    /// Sets the current state for a zone.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="state">Zone state to store</param>
    void SetZoneState(int zoneIndex, ZoneState state);

    /// <summary>
    /// Gets all zone states.
    /// </summary>
    /// <returns>Dictionary of zone states by zone index</returns>
    Dictionary<int, ZoneState> GetAllZoneStates();

    /// <summary>
    /// Initializes default state for a zone if it doesn't exist.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based)</param>
    /// <param name="defaultState">Default state to use</param>
    void InitializeZoneState(int zoneIndex, ZoneState defaultState);
}
