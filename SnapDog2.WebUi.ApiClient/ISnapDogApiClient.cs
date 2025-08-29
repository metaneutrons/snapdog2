using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient;

public interface ISnapDogApiClient
{
    Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default);
    Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
    Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default);
    Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default);
}
