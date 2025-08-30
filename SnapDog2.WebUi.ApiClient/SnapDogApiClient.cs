using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient;

public class SnapDogApiClient : ISnapDogApiClient, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private HubConnection? _hubConnection;

    public SnapDogApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // SignalR events
    public event Action<ZoneState>? ZoneStateUpdated;
    public event Action<int, long, float>? TrackProgressUpdated;
    public event Action<int, TrackInfo>? TrackChanged;

    // Count endpoints
    public async Task<int> GetZonesCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync("/api/v1/zones/count", cancellationToken);
        return int.Parse(response);
    }

    public async Task<int> GetClientsCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync("/api/v1/clients/count", cancellationToken);
        return int.Parse(response);
    }

    // Zone control endpoints
    public async Task<int> GetZoneVolumeAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync($"/api/v1/zones/{zoneIndex}/volume", cancellationToken);
        return int.Parse(response);
    }

    public async Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        await _httpClient.PutAsync($"/api/v1/zones/{zoneIndex}/volume",
            new StringContent(volume.ToString(), System.Text.Encoding.UTF8, "application/json"), cancellationToken);
    }

    public async Task<bool> GetZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync($"/api/v1/zones/{zoneIndex}/mute", cancellationToken);
        return bool.Parse(response);
    }

    public async Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/mute/toggle", null, cancellationToken);
    }

    public async Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/play", null, cancellationToken);
    }

    public async Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/pause", null, cancellationToken);
    }

    public async Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/next", null, cancellationToken);
    }

    public async Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/previous", null, cancellationToken);
    }

    public async Task<int> GetZonePlaylistCountAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync($"/api/v1/zones/{zoneIndex}/playlist/count", cancellationToken);
        return int.Parse(response);
    }

    // Client control endpoints
    public async Task<int> GetClientZoneAsync(int clientIndex, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStringAsync($"/api/v1/clients/{clientIndex}/zone", cancellationToken);
        return int.Parse(response);
    }

    public async Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        await _httpClient.PutAsync($"/api/v1/clients/{clientIndex}/zone",
            new StringContent(zoneIndex.ToString(), System.Text.Encoding.UTF8, "application/json"), cancellationToken);
    }

    public async Task StartRealtimeUpdatesAsync()
    {
        if (_hubConnection != null)
        {
            return;
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5555/zonehub")
            .Build();

        _hubConnection.On<ZoneState>("ZoneStateUpdated", (zoneState) =>
        {
            ZoneStateUpdated?.Invoke(zoneState);
        });

        _hubConnection.On<int, long, float>("TrackProgressUpdated", (zoneIndex, position, progress) =>
        {
            TrackProgressUpdated?.Invoke(zoneIndex, position, progress);
        });

        _hubConnection.On<int, TrackInfo>("TrackChanged", (zoneIndex, trackInfo) =>
        {
            TrackChanged?.Invoke(zoneIndex, trackInfo);
        });

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("JoinAllZones");
    }

    public async Task StopRealtimeUpdatesAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopRealtimeUpdatesAsync();
    }
}
