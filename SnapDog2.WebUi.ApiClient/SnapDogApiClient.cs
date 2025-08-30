using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SnapDog2.WebUi.ApiClient;

/// <summary>
/// API client implementation with functional mock data.
/// </summary>
public partial class SnapDogApiClient : ISnapDogApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SnapDogApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Mock state that actually changes
    private static readonly List<ZoneState> _mockZones = new()
    {
        new(1, "Living Room", false, false, 75,
            new TrackInfo("Sample Track", "Sample Artist", "Sample Album", TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(45)),
            new[] { 1, 2 }),
        new(2, "Kitchen", true, false, 60,
            new TrackInfo("Another Track", "Another Artist", "Another Album", TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1)),
            new[] { 3 }),
        new(3, "Bedroom", false, true, 0, null, Array.Empty<int>())
    };

    private static readonly List<ClientState> _mockClients = new()
    {
        new(1, "Living Room Speaker", true, 1, false, 75),
        new(2, "Living Room Subwoofer", true, 1, false, 80),
        new(3, "Kitchen Speaker", true, 2, false, 60),
        new(4, "Bedroom Speaker", false, null, true, 0)
    };

    public SnapDogApiClient(HttpClient httpClient, ILogger<SnapDogApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        LogFetchingZones();
        LogRetrievedZones(_mockZones.Count);
        return _mockZones.ToArray();
    }

    public async Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        var zone = _mockZones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone == null)
        {
            throw new InvalidOperationException($"Zone {zoneIndex} not found");
        }

        return zone;
    }

    public async Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        LogFetchingClients();
        LogRetrievedClients(_mockClients.Count);
        return _mockClients.ToArray();
    }

    public async Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogPlayingZone(zoneIndex);
        var zone = _mockZones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            var index = _mockZones.IndexOf(zone);
            _mockZones[index] = zone with { IsPlaying = true };
        }
    }

    public async Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogPausingZone(zoneIndex);
        var zone = _mockZones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            var index = _mockZones.IndexOf(zone);
            _mockZones[index] = zone with { IsPlaying = false };
        }
    }

    public async Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogTogglingZoneMute(zoneIndex);
        var zone = _mockZones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            var index = _mockZones.IndexOf(zone);
            _mockZones[index] = zone with { IsMuted = !zone.IsMuted };
        }
    }

    public async Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogSettingZoneVolume(zoneIndex, volume);
        var zone = _mockZones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            var index = _mockZones.IndexOf(zone);
            _mockZones[index] = zone with { Volume = volume };
        }
    }

    public async Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogNextTrack(zoneIndex);
    }

    public async Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogPreviousTrack(zoneIndex);
    }

    public async Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        LogAssigningClient(clientIndex, zoneIndex);
        var client = _mockClients.FirstOrDefault(c => c.Index == clientIndex);
        if (client != null)
        {
            var index = _mockClients.IndexOf(client);
            _mockClients[index] = client with { ZoneIndex = zoneIndex };
        }
    }

    public async Task<int> GetZonesCountAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return _mockZones.Count;
    }

    public async Task<int> GetClientsCountAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return _mockClients.Count;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching all zones")]
    private partial void LogFetchingZones();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Retrieved {ZoneCount} zones")]
    private partial void LogRetrievedZones(int ZoneCount);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Fetching all clients")]
    private partial void LogFetchingClients();

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Retrieved {ClientCount} clients")]
    private partial void LogRetrievedClients(int ClientCount);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClient(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "Setting zone {ZoneIndex} volume to {Volume}")]
    private partial void LogSettingZoneVolume(int ZoneIndex, int Volume);

    [LoggerMessage(EventId = 17, Level = LogLevel.Debug, Message = "Toggling mute for zone {ZoneIndex}")]
    private partial void LogTogglingZoneMute(int ZoneIndex);

    [LoggerMessage(EventId = 18, Level = LogLevel.Debug, Message = "Playing zone {ZoneIndex}")]
    private partial void LogPlayingZone(int ZoneIndex);

    [LoggerMessage(EventId = 19, Level = LogLevel.Debug, Message = "Pausing zone {ZoneIndex}")]
    private partial void LogPausingZone(int ZoneIndex);

    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Next track for zone {ZoneIndex}")]
    private partial void LogNextTrack(int ZoneIndex);

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "Previous track for zone {ZoneIndex}")]
    private partial void LogPreviousTrack(int ZoneIndex);
}
