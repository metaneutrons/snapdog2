using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient;

public class SnapDogApiClient : ISnapDogApiClient
{
    private readonly IGeneratedSnapDogClient _client;

    public SnapDogApiClient(IGeneratedSnapDogClient client)
    {
        _client = client;
    }

    public async Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _client.ZonesAsync(page: 1, size: 100, cancellationToken);
        return result.Items?.ToArray() ?? Array.Empty<ZoneState>();
    }

    public async Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        return await _client.Zones2Async(zoneIndex, cancellationToken);
    }

    public async Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        await _client.VolumePUT2Async(zoneIndex, volume, cancellationToken);
    }

    public async Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _client.Toggle2Async(zoneIndex, cancellationToken);
    }

    public async Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _client.PlayAsync(zoneIndex, cancellationToken);
    }

    public async Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _client.PauseAsync(zoneIndex, cancellationToken);
    }

    public async Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _client.NextAsync(zoneIndex, cancellationToken);
    }

    public async Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _client.PreviousAsync(zoneIndex, cancellationToken);
    }

    public async Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _client.ClientsAllAsync(cancellationToken);
        return result?.ToArray() ?? Array.Empty<ClientState>();
    }

    public async Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _client.ZonePUTAsync(clientIndex, zoneIndex, cancellationToken);
    }
}
