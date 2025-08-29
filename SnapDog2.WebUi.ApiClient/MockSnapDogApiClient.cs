namespace SnapDog2.WebUi.ApiClient;

public class MockSnapDogApiClient : ISnapDogApiClient
{
    private static readonly List<ZoneState> _zones = new()
    {
        new ZoneState
        {
            Index = 1,
            Name = "Living Room",
            IsPlaying = true,
            IsMuted = false,
            Volume = 75,
            CurrentTrack = new TrackInfo
            {
                Title = "Bohemian Rhapsody",
                Artist = "Queen",
                Album = "A Night at the Opera",
                Duration = TimeSpan.FromMinutes(6)
            },
            Clients = new List<ClientState>
            {
                new() { Index = 1, Name = "Living Room Speaker", IsConnected = true, ZoneIndex = 1 },
                new() { Index = 2, Name = "Kitchen Speaker", IsConnected = true, ZoneIndex = 1 }
            }
        },
        new ZoneState
        {
            Index = 2,
            Name = "Bedroom",
            IsPlaying = false,
            IsMuted = false,
            Volume = 50,
            CurrentTrack = null,
            Clients = new List<ClientState>
            {
                new() { Index = 3, Name = "Bedroom Speaker", IsConnected = false, ZoneIndex = 2 }
            }
        }
    };

    public Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_zones.ToArray());
    }

    public Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == zoneIndex);
        return Task.FromResult(zone ?? throw new ArgumentException($"Zone {zoneIndex} not found"));
    }

    public Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            zone.Volume = Math.Clamp(volume, 0, 100);
        }

        return Task.CompletedTask;
    }

    public Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            zone.IsMuted = !zone.IsMuted;
        }

        return Task.CompletedTask;
    }

    public Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            zone.IsPlaying = true;
        }

        return Task.CompletedTask;
    }

    public Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        var zone = _zones.FirstOrDefault(z => z.Index == zoneIndex);
        if (zone != null)
        {
            zone.IsPlaying = false;
        }

        return Task.CompletedTask;
    }

    public Task NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        // Mock implementation
        return Task.CompletedTask;
    }

    public Task PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        // Mock implementation
        return Task.CompletedTask;
    }

    public Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        var allClients = _zones.SelectMany(z => z.Clients).ToArray();
        return Task.FromResult(allClients);
    }

    public Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        // Find client and move to new zone
        foreach (var zone in _zones)
        {
            var client = zone.Clients.FirstOrDefault(c => c.Index == clientIndex);
            if (client != null)
            {
                zone.Clients.Remove(client);
                client.ZoneIndex = zoneIndex;

                var targetZone = _zones.FirstOrDefault(z => z.Index == zoneIndex);
                targetZone?.Clients.Add(client);
                break;
            }
        }
        return Task.CompletedTask;
    }
}
