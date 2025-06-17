using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Demo;

/// <summary>
/// Demonstrates all domain entities and their capabilities.
/// Shows creation, manipulation, and business logic of Zone, Client, AudioStream, Playlist, Track, and RadioStation entities.
/// </summary>
public class DomainEntitiesDemo
{
    private readonly ILogger<DomainEntitiesDemo> _logger;

    public DomainEntitiesDemo(ILogger<DomainEntitiesDemo> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the complete domain entities demonstration.
    /// </summary>
    public async Task RunDemoAsync()
    {
        _logger.LogInformation("=== Domain Entities Demo ===");

        try
        {
            // Demonstrate each entity type
            await DemonstrateZoneEntityAsync();
            await DemonstrateClientEntityAsync();
            await DemonstrateAudioStreamEntityAsync();
            await DemonstratePlaylistEntityAsync();
            await DemonstrateTrackEntityAsync();
            await DemonstrateRadioStationEntityAsync();
            await DemonstrateValueObjectsAsync();
            await DemonstrateMultiRoomScenarioAsync();

            _logger.LogInformation("Domain entities demo completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Domain entities demo failed");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates Zone entity creation and manipulation.
    /// </summary>
    private async Task DemonstrateZoneEntityAsync()
    {
        _logger.LogInformation("--- Zone Entity Demo ---");

        // Create a zone
        var zone = Zone.Create("living-room", "Living Room", "Main entertainment area");
        _logger.LogInformation("Created zone: {ZoneName} (ID: {ZoneId})", zone.Name, zone.Id);

        // Add clients to zone
        var zoneWithClients = zone.WithAddedClient("client-1").WithAddedClient("client-2");
        _logger.LogInformation("Added clients to zone: {ClientCount} clients", zoneWithClients.ClientCount);

        // Update zone settings
        var zoneWithSettings = zoneWithClients
            .WithVolumeSettings(defaultVolume: 60, minVolume: 0, maxVolume: 90)
            .WithCurrentStream("stream-classical");
        _logger.LogInformation(
            "Updated zone settings: Default volume {Volume}, Current stream: {Stream}",
            zoneWithSettings.DefaultVolume,
            zoneWithSettings.CurrentStreamId
        );

        // Demonstrate zone properties
        _logger.LogInformation(
            "Zone is active: {IsActive}, Has clients: {HasClients}, Has stream: {HasCurrentStream}",
            zoneWithSettings.IsActive,
            zoneWithSettings.HasClients,
            zoneWithSettings.HasCurrentStream
        );

        await Task.Delay(100); // Simulate async operation
    }

    /// <summary>
    /// Demonstrates Client entity creation and manipulation.
    /// </summary>
    private async Task DemonstrateClientEntityAsync()
    {
        _logger.LogInformation("--- Client Entity Demo ---");

        // Create clients with different configurations
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        var client = Client.Create(
            "client-1",
            "Living Room Speaker",
            macAddress,
            ipAddress,
            ClientStatus.Connected,
            75
        );
        _logger.LogInformation(
            "Created client: {ClientName} ({MAC}) at {IP}",
            client.Name,
            client.MacAddress,
            client.IpAddress
        );

        // Demonstrate client operations
        var clientWithZone = client.WithZone("living-room");
        var clientMuted = clientWithZone.WithMute(true);
        var clientVolumeChanged = clientMuted.WithVolume(50).WithMute(false);

        _logger.LogInformation(
            "Client status: Connected: {Connected}, Volume: {Volume}, Muted: {Muted}, Zone: {Zone}",
            clientVolumeChanged.IsConnected,
            clientVolumeChanged.Volume,
            clientVolumeChanged.IsMuted,
            clientVolumeChanged.ZoneId
        );

        // Show effective volume calculation
        _logger.LogInformation(
            "Effective volume: {EffectiveVolume}, Is silent: {IsSilent}",
            clientVolumeChanged.EffectiveVolume,
            clientVolumeChanged.IsSilent
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates AudioStream entity creation and manipulation.
    /// </summary>
    private async Task DemonstrateAudioStreamEntityAsync()
    {
        _logger.LogInformation("--- AudioStream Entity Demo ---");

        var streamUrl = new StreamUrl("http://radio.example.com/classical");
        var stream = AudioStream.Create("stream-classical", "Classical Music", streamUrl, AudioCodec.FLAC, 1411);

        _logger.LogInformation(
            "Created audio stream: {Name} ({Codec}, {Bitrate} kbps)",
            stream.Name,
            stream.Codec,
            stream.BitrateKbps
        );

        // Update stream status
        var playingStream = stream.WithStatus(StreamStatus.Playing);
        var stoppedStream = playingStream.WithStatus(StreamStatus.Stopped);

        _logger.LogInformation("Stream status progression: Created -> Playing -> Stopped");
        _logger.LogInformation(
            "Current status: {Status}, Is playing: {IsPlaying}, Has error: {HasError}",
            stoppedStream.Status,
            stoppedStream.IsPlaying,
            stoppedStream.HasError
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates Playlist entity creation and manipulation.
    /// </summary>
    private async Task DemonstratePlaylistEntityAsync()
    {
        _logger.LogInformation("--- Playlist Entity Demo ---");

        var playlist = Playlist.Create("jazz-classics", "Jazz Classics", "Best jazz tracks of all time", "admin");
        _logger.LogInformation("Created playlist: {Name} by {Owner}", playlist.Name, playlist.Owner);

        // Add tracks to playlist
        var playlistWithTracks = playlist.WithAddedTrack("track-1").WithAddedTrack("track-2").WithAddedTrack("track-3");

        _logger.LogInformation("Added tracks to playlist: {TrackCount} tracks", playlistWithTracks.TrackCount);

        // Demonstrate playlist operations
        var reorderedPlaylist = playlistWithTracks.WithMovedTrack(0, 2);
        var shuffledPlaylist = reorderedPlaylist.WithShuffledTracks();

        _logger.LogInformation("Playlist operations: Reordered and shuffled");
        _logger.LogInformation(
            "Has tracks: {HasTracks}, Is empty: {IsEmpty}, Play count: {PlayCount}",
            shuffledPlaylist.HasTracks,
            shuffledPlaylist.IsEmpty,
            shuffledPlaylist.PlayCount
        );

        // Simulate playing
        var playedPlaylist = shuffledPlaylist.WithPlayIncrement();
        _logger.LogInformation(
            "Playlist played: Play count now {PlayCount}, Last played: {LastPlayed}",
            playedPlaylist.PlayCount,
            playedPlaylist.LastPlayedAt
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates Track entity creation and manipulation.
    /// </summary>
    private async Task DemonstrateTrackEntityAsync()
    {
        _logger.LogInformation("--- Track Entity Demo ---");

        var track = Track.Create("track-1", "Take Five", "Dave Brubeck Quartet", "Time Out");
        _logger.LogInformation("Created track: {DisplayName} from {Album}", track.DisplayName, track.Album);

        // Add metadata and technical info
        var enrichedTrack = track
            .WithMetadata(genre: "Jazz", year: 1959)
            .WithTechnicalInfo(durationSeconds: 324, bitrateKbps: 320, sampleRateHz: 44100, channels: 2, format: "MP3")
            .WithTag("mood", "relaxing")
            .WithTag("tempo", "moderate");

        _logger.LogInformation(
            "Track metadata: Genre: {Genre}, Year: {Year}, Duration: {Duration}",
            enrichedTrack.Genre,
            enrichedTrack.Year,
            enrichedTrack.FormattedDuration
        );

        _logger.LogInformation(
            "Technical info: {Format}, {Bitrate} kbps, {SampleRate} Hz, {Channels} channels",
            enrichedTrack.Format,
            enrichedTrack.BitrateKbps,
            enrichedTrack.SampleRateHz,
            enrichedTrack.Channels
        );

        // Simulate playing
        var playedTrack = enrichedTrack.WithPlayIncrement();
        _logger.LogInformation(
            "Track played: Play count {PlayCount}, Has been played: {HasBeenPlayed}",
            playedTrack.PlayCount,
            playedTrack.HasBeenPlayed
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates RadioStation entity creation and manipulation.
    /// </summary>
    private async Task DemonstrateRadioStationEntityAsync()
    {
        _logger.LogInformation("--- RadioStation Entity Demo ---");

        var stationUrl = new StreamUrl("http://stream.example.com/classical");
        var station = RadioStation.Create(
            "classical-fm",
            "Classical FM",
            stationUrl,
            AudioCodec.MP3,
            "Best classical music 24/7"
        );

        _logger.LogInformation("Created radio station: {Name} - {Description}", station.Name, station.Description);

        // Add metadata and technical info
        var enrichedStation = station
            .WithMetadata(genre: "Classical", country: "USA", language: "English")
            .WithTechnicalInfo(bitrateKbps: 128, sampleRateHz: 44100, channels: 2)
            .WithOnlineStatus(true);

        _logger.LogInformation(
            "Station info: {QualityInfo}, Available: {IsAvailable}",
            enrichedStation.QualityInfo,
            enrichedStation.IsAvailable
        );

        _logger.LogInformation("Display name: {DisplayName}", enrichedStation.DisplayName);

        // Simulate playing
        var playedStation = enrichedStation.WithPlayIncrement();
        _logger.LogInformation(
            "Station played: Play count {PlayCount}, Last played: {LastPlayed}",
            playedStation.PlayCount,
            playedStation.LastPlayedAt
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates value objects (MacAddress, IpAddress, StreamUrl).
    /// </summary>
    private async Task DemonstrateValueObjectsAsync()
    {
        _logger.LogInformation("--- Value Objects Demo ---");

        // MacAddress demonstrations
        var mac1 = new MacAddress("aa:bb:cc:dd:ee:ff");
        var mac2 = new MacAddress("AA-BB-CC-DD-EE-FF"); // Different format, same value
        _logger.LogInformation("MAC addresses: {Mac1} == {Mac2}: {AreEqual}", mac1, mac2, mac1 == mac2);

        // IpAddress demonstrations
        var ip1 = new IpAddress("192.168.1.100");
        var ip2 = new IpAddress("127.0.0.1");
        _logger.LogInformation(
            "IP addresses: {Ip1} (IsLoopback: {IsLoopback1}), {Ip2} (IsLoopback: {IsLoopback2})",
            ip1,
            ip1.IsLoopback,
            ip2,
            ip2.IsLoopback
        );

        // StreamUrl demonstrations
        var url1 = new StreamUrl("http://stream.example.com/radio");
        var url2 = new StreamUrl("https://secure.stream.com/music");
        _logger.LogInformation(
            "Stream URLs: {Url1} (IsSecure: {IsSecure1}), {Url2} (IsSecure: {IsSecure2})",
            url1,
            url1.IsSecure,
            url2,
            url2.IsSecure
        );

        // Validation demonstrations
        _logger.LogInformation(
            "MAC validation: Valid format 'AA:BB:CC:DD:EE:FF': {IsValid}",
            MacAddress.IsValid("AA:BB:CC:DD:EE:FF")
        );
        _logger.LogInformation(
            "IP validation: Valid format '192.168.1.1': {IsValid}",
            IpAddress.IsValid("192.168.1.1")
        );
        _logger.LogInformation(
            "URL validation: Valid format 'http://example.com': {IsValid}",
            StreamUrl.IsValid("http://example.com")
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates a realistic multi-room audio scenario.
    /// </summary>
    private async Task DemonstrateMultiRoomScenarioAsync()
    {
        _logger.LogInformation("--- Multi-Room Audio Scenario Demo ---");

        // Create zones
        var livingRoom = Zone.Create("living-room", "Living Room").WithVolumeSettings(65, 0, 100);
        var kitchen = Zone.Create("kitchen", "Kitchen").WithVolumeSettings(50, 10, 80);
        var bedroom = Zone.Create("bedroom", "Bedroom").WithVolumeSettings(40, 0, 60);

        _logger.LogInformation(
            "Created zones: {LivingRoom}, {Kitchen}, {Bedroom}",
            livingRoom.Name,
            kitchen.Name,
            bedroom.Name
        );

        // Create clients
        var clients = new[]
        {
            Client.Create(
                "lr-speaker",
                "Living Room Speaker",
                "AA:BB:CC:DD:EE:01",
                "192.168.1.101",
                ClientStatus.Connected,
                65
            ),
            Client.Create(
                "kitchen-speaker",
                "Kitchen Speaker",
                "AA:BB:CC:DD:EE:02",
                "192.168.1.102",
                ClientStatus.Connected,
                50
            ),
            Client.Create(
                "bedroom-speaker",
                "Bedroom Speaker",
                "AA:BB:CC:DD:EE:03",
                "192.168.1.103",
                ClientStatus.Connected,
                40
            ),
            Client.Create(
                "bathroom-speaker",
                "Bathroom Speaker",
                "AA:BB:CC:DD:EE:04",
                "192.168.1.104",
                ClientStatus.Disconnected,
                30
            ),
        };

        _logger.LogInformation("Created {ClientCount} clients with various connection states", clients.Length);

        // Assign clients to zones
        var livingRoomWithClient = livingRoom.WithAddedClient("lr-speaker");
        var kitchenWithClient = kitchen.WithAddedClient("kitchen-speaker");
        var bedroomWithClient = bedroom.WithAddedClient("bedroom-speaker");

        // Create audio streams
        var jazzStream = AudioStream.Create(
            "jazz-stream",
            "Smooth Jazz",
            "http://jazz.example.com",
            AudioCodec.MP3,
            320
        );
        var classicalStream = AudioStream.Create(
            "classical-stream",
            "Classical Music",
            "http://classical.example.com",
            AudioCodec.FLAC,
            1411
        );

        _logger.LogInformation("Created audio streams: {Jazz} and {Classical}", jazzStream.Name, classicalStream.Name);

        // Assign streams to zones
        var livingRoomPlaying = livingRoomWithClient.WithCurrentStream("jazz-stream");
        var kitchenPlaying = kitchenWithClient.WithCurrentStream("classical-stream");

        _logger.LogInformation("Zone assignments:");
        _logger.LogInformation(
            "  - {Zone}: {Stream} ({ClientCount} clients)",
            livingRoomPlaying.Name,
            livingRoomPlaying.CurrentStreamId,
            livingRoomPlaying.ClientCount
        );
        _logger.LogInformation(
            "  - {Zone}: {Stream} ({ClientCount} clients)",
            kitchenPlaying.Name,
            kitchenPlaying.CurrentStreamId,
            kitchenPlaying.ClientCount
        );
        _logger.LogInformation(
            "  - {Zone}: No stream ({ClientCount} clients)",
            bedroomWithClient.Name,
            bedroomWithClient.ClientCount
        );

        // Simulate volume changes
        var clientVolumeUp = clients[0].WithVolume(80);
        var clientMuted = clients[1].WithMute(true);

        _logger.LogInformation("Client operations:");
        _logger.LogInformation("  - {Client}: Volume changed to {Volume}", clientVolumeUp.Name, clientVolumeUp.Volume);
        _logger.LogInformation(
            "  - {Client}: Muted (effective volume: {EffectiveVolume})",
            clientMuted.Name,
            clientMuted.EffectiveVolume
        );

        await Task.Delay(200);
        _logger.LogInformation("Multi-room scenario completed");
    }
}
