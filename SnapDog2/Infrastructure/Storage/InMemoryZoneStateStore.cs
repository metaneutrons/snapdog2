namespace SnapDog2.Infrastructure.Storage;

using System.Collections.Concurrent;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

/// <summary>
/// In-memory implementation of zone state storage.
/// Thread-safe and persists state across HTTP requests.
/// </summary>
public class InMemoryZoneStateStore : IZoneStateStore
{
    private readonly ConcurrentDictionary<int, ZoneState> _zoneStates = new();

    public ZoneState? GetZoneState(int zoneIndex)
    {
        return _zoneStates.TryGetValue(zoneIndex, out var state) ? state : null;
    }

    public void SetZoneState(int zoneIndex, ZoneState state)
    {
        _zoneStates.AddOrUpdate(zoneIndex, state, (_, _) => state);
    }

    public Dictionary<int, ZoneState> GetAllZoneStates()
    {
        return _zoneStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void InitializeZoneState(int zoneIndex, ZoneState defaultState)
    {
        _zoneStates.TryAdd(zoneIndex, defaultState);
    }
}
