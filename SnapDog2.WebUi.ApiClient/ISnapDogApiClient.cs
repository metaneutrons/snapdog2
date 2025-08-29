using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient;

public interface ISnapDogApiClient
{
    Task<PublishableZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default);
    Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default);
    Task<SystemStatusApiResponse> GetSystemStatusAsync(CancellationToken cancellationToken = default);
    Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default);

    // Media Controls
    Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task StopZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task<int> NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task<int> PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task<bool> ToggleMuteAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task<int> SetVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
}
