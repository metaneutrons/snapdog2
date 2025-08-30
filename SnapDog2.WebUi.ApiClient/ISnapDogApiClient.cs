using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient;

public interface ISnapDogApiClient
{
    // Count endpoints only
    Task<int> GetZonesCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetClientsCountAsync(CancellationToken cancellationToken = default);

    // Zone control endpoints (no full state)
    Task<int> GetZoneVolumeAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
    Task<bool> GetZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task<int> GetZonePlaylistCountAsync(int zoneIndex, CancellationToken cancellationToken = default);

    // Client control endpoints (no full state)
    Task<int> GetClientZoneAsync(int clientIndex, CancellationToken cancellationToken = default);
    Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default);

    // SignalR events
    event Action<ZoneState>? ZoneStateUpdated;
    event Action<int, long, float>? TrackProgressUpdated;
    event Action<int, TrackInfo>? TrackChanged;

    Task StartRealtimeUpdatesAsync();
    Task StopRealtimeUpdatesAsync();
}
