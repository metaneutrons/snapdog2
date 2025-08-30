namespace SnapDog2.WebUi.ApiClient;

/// <summary>
/// Business-focused API client interface for SnapDog operations.
/// </summary>
public interface ISnapDogApiClient
{
    // Zone Operations
    Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default);
    Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
    Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default);

    // Client Operations
    Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default);
    Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default);

    // Count Operations (using specialized endpoints)
    Task<int> GetZonesCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetClientsCountAsync(CancellationToken cancellationToken = default);
}

public record ZoneState(
    int Index,
    string Name,
    bool IsPlaying,
    bool IsMuted,
    int Volume,
    TrackInfo? CurrentTrack,
    int[] ClientIndices);

public record ClientState(
    int Index,
    string Name,
    bool IsConnected,
    int? ZoneIndex,
    bool IsMuted,
    int Volume);

public record TrackInfo(
    string Title,
    string Artist,
    string Album,
    TimeSpan Duration,
    TimeSpan Position);
