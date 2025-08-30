using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace SnapDog2.WebUi.ApiClient;

/// <summary>
/// API client implementation with resilience and logging.
/// </summary>
public partial class SnapDogApiClient : ISnapDogApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SnapDogApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SnapDogApiClient(HttpClient httpClient, ILogger<SnapDogApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        LogFetchingZones();
        
        // For now, return mock data until API is fully implemented
        var mockZones = new[]
        {
            new ZoneState(1, "Living Room", false, false, 75, 
                new TrackInfo("Sample Track", "Sample Artist", "Sample Album", TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(45)),
                new[] { 1, 2 }),
            new ZoneState(2, "Kitchen", true, false, 60, 
                new TrackInfo("Another Track", "Another Artist", "Another Album", TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1)),
                new[] { 3 }),
            new ZoneState(3, "Bedroom", false, true, 0, null, Array.Empty<int>())
        };

        LogRetrievedZones(mockZones.Length);
        return mockZones;
    }

    public async Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogFetchingZone(zoneIndex);
        
        var zones = await GetAllZonesAsync(cancellationToken);
        var zone = zones.FirstOrDefault(z => z.Index == zoneIndex);
        
        if (zone == null)
        {
            LogZoneNotFound(zoneIndex);
            throw new InvalidOperationException($"Zone {zoneIndex} not found");
        }

        return zone;
    }

    public async Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        LogFetchingClients();
        
        // Mock data for now
        var mockClients = new[]
        {
            new ClientState(1, "Living Room Speaker", true, 1, false, 75),
            new ClientState(2, "Living Room Subwoofer", true, 1, false, 80),
            new ClientState(3, "Kitchen Speaker", true, 2, false, 60),
            new ClientState(4, "Bedroom Speaker", false, null, true, 0)
        };

        LogRetrievedClients(mockClients.Length);
        return mockClients;
    }

    public async Task<int> GetZonesCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogFetchingZonesCount();
            var response = await _httpClient.GetAsync("zones/count", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var count = await response.Content.ReadFromJsonAsync<int>(_jsonOptions, cancellationToken);
            LogRetrievedZonesCount(count);
            return count;
        }
        catch (Exception ex)
        {
            LogErrorFetchingZonesCount(ex.Message);
            // Fallback to mock data
            return 3;
        }
    }

    public async Task<int> GetClientsCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogFetchingClientsCount();
            var response = await _httpClient.GetAsync("clients/count", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var count = await response.Content.ReadFromJsonAsync<int>(_jsonOptions, cancellationToken);
            LogRetrievedClientsCount(count);
            return count;
        }
        catch (Exception ex)
        {
            LogErrorFetchingClientsCount(ex.Message);
            // Fallback to mock data
            return 4;
        }
    }

    public async Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (clientIndex < 1) throw new ArgumentException("Client index must be >= 1", nameof(clientIndex));
        if (zoneIndex < 1) throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));

        LogAssigningClient(clientIndex, zoneIndex);
        
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"clients/{clientIndex}/zone", zoneIndex, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            LogClientAssigned(clientIndex, zoneIndex);
        }
        catch (Exception ex)
        {
            LogErrorAssigningClient(clientIndex, zoneIndex, ex.Message);
            throw;
        }
    }

    public async Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        LogSettingZoneVolume(zoneIndex, volume);
        // Mock implementation for now
        await Task.Delay(100, cancellationToken);
    }

    public async Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogTogglingZoneMute(zoneIndex);
        // Mock implementation for now
        await Task.Delay(100, cancellationToken);
    }

    public async Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogPlayingZone(zoneIndex);
        // Mock implementation for now
        await Task.Delay(100, cancellationToken);
    }

    public async Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogPausingZone(zoneIndex);
        // Mock implementation for now
        await Task.Delay(100, cancellationToken);
    }

    public async Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogNextTrack(zoneIndex);
        // Mock implementation for now
        await Task.Delay(100, cancellationToken);
    }

    public async Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        LogPreviousTrack(zoneIndex);
        // Mock implementation for now
        await Task.Delay(100, cancellationToken);
    }

    // LoggerMessage methods
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching all zones")]
    private partial void LogFetchingZones();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Retrieved {ZoneCount} zones")]
    private partial void LogRetrievedZones(int ZoneCount);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Fetching zone {ZoneIndex}")]
    private partial void LogFetchingZone(int ZoneIndex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Zone {ZoneIndex} not found")]
    private partial void LogZoneNotFound(int ZoneIndex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Fetching all clients")]
    private partial void LogFetchingClients();

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Retrieved {ClientCount} clients")]
    private partial void LogRetrievedClients(int ClientCount);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Fetching zones count")]
    private partial void LogFetchingZonesCount();

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Retrieved zones count: {Count}")]
    private partial void LogRetrievedZonesCount(int Count);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Error fetching zones count: {Error}")]
    private partial void LogErrorFetchingZonesCount(string Error);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Fetching clients count")]
    private partial void LogFetchingClientsCount();

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Retrieved clients count: {Count}")]
    private partial void LogRetrievedClientsCount(int Count);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error fetching clients count: {Error}")]
    private partial void LogErrorFetchingClientsCount(string Error);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClient(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Successfully assigned client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogClientAssigned(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error assigning client {ClientIndex} to zone {ZoneIndex}: {Error}")]
    private partial void LogErrorAssigningClient(int ClientIndex, int ZoneIndex, string Error);

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
