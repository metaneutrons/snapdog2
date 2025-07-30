using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.State;
using Xunit;

namespace SnapDog2.Tests.State;

/// <summary>
/// Unit tests for the StateExtensions class.
/// Tests query methods, filtering, relationship queries, and statistics.
/// </summary>
public class StateExtensionsTests
{
    private readonly SnapDogState _testState;

    public StateExtensionsTests()
    {
        // Create a test state with sample data
        var stream1 = AudioStream
            .Create("stream-1", "Jazz Stream", new StreamUrl("http://jazz.example.com"), AudioCodec.FLAC, 1411)
            .WithStatus(StreamStatus.Playing);
        var stream2 = AudioStream
            .Create("stream-2", "Rock Stream", new StreamUrl("http://rock.example.com"), AudioCodec.MP3, 320)
            .WithStatus(StreamStatus.Stopped);

        var client1 = Client
            .Create("client-1", "Living Room", new MacAddress("AA:BB:CC:DD:EE:01"), new IpAddress("192.168.1.101"))
            .WithStatus(ClientStatus.Connected)
            .WithZone("zone-1");
        var client2 = Client
            .Create("client-2", "Kitchen", new MacAddress("AA:BB:CC:DD:EE:02"), new IpAddress("192.168.1.102"))
            .WithStatus(ClientStatus.Disconnected);

        var zone1 = Zone.Create("zone-1", "Living Room Zone")
            .WithAddedClient("client-1")
            .WithCurrentStream("stream-1")
            .WithEnabled(true);
        var zone2 = Zone.Create("zone-2", "Kitchen Zone").WithEnabled(true); // Zone2 is enabled but has no clients, so it won't be active

        var track1 = Track.Create("track-1", "Blue Note", "Miles Davis");
        var track2 = Track.Create("track-2", "Stairway to Heaven", "Led Zeppelin");

        var playlist1 = Playlist.Create("playlist-1", "Jazz Classics").WithAddedTrack("track-1");
        var playlist2 = Playlist.Create("playlist-2", "Rock Hits").WithAddedTrack("track-2");

        var radioStation1 = RadioStation
            .Create("station-1", "Jazz FM", new StreamUrl("http://jazzfm.example.com"), AudioCodec.AAC)
            .WithEnabled(true);
        var radioStation2 = RadioStation
            .Create("station-2", "Rock Radio", new StreamUrl("http://rockradio.example.com"), AudioCodec.MP3)
            .WithEnabled(false);

        _testState = SnapDogState
            .CreateEmpty()
            .WithAudioStream(stream1)
            .WithAudioStream(stream2)
            .WithClient(client1)
            .WithClient(client2)
            .WithZone(zone1)
            .WithZone(zone2)
            .WithTrack(track1)
            .WithTrack(track2)
            .WithPlaylist(playlist1)
            .WithPlaylist(playlist2)
            .WithRadioStation(radioStation1)
            .WithRadioStation(radioStation2)
            .WithSystemStatus(SystemStatus.Running);
    }

    [Fact]
    public void GetAudioStream_WithValidId_ShouldReturnStream()
    {
        // Act
        var stream = _testState.GetAudioStream("stream-1");

        // Assert
        Assert.NotNull(stream);
        Assert.Equal("stream-1", stream.Id);
        Assert.Equal("Jazz Stream", stream.Name);
    }

    [Fact]
    public void GetAudioStream_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var stream = _testState.GetAudioStream("non-existent");

        // Assert
        Assert.Null(stream);
    }

    [Fact]
    public void GetAudioStream_WithNullOrEmptyId_ShouldReturnNull()
    {
        // Act & Assert
        Assert.Null(_testState.GetAudioStream(null!));
        Assert.Null(_testState.GetAudioStream(""));
        Assert.Null(_testState.GetAudioStream("   "));
    }

    [Fact]
    public void GetClient_WithValidId_ShouldReturnClient()
    {
        // Act
        var client = _testState.GetClient("client-1");

        // Assert
        Assert.NotNull(client);
        Assert.Equal("client-1", client.Id);
        Assert.Equal("Living Room", client.Name);
    }

    [Fact]
    public void GetClient_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var client = _testState.GetClient("non-existent");

        // Assert
        Assert.Null(client);
    }

    [Fact]
    public void GetZone_WithValidId_ShouldReturnZone()
    {
        // Act
        var zone = _testState.GetZone("zone-1");

        // Assert
        Assert.NotNull(zone);
        Assert.Equal("zone-1", zone.Id);
        Assert.Equal("Living Room Zone", zone.Name);
    }

    [Fact]
    public void GetZone_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var zone = _testState.GetZone("non-existent");

        // Assert
        Assert.Null(zone);
    }

    [Fact]
    public void GetPlaylist_WithValidId_ShouldReturnPlaylist()
    {
        // Act
        var playlist = _testState.GetPlaylist("playlist-1");

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("playlist-1", playlist.Id);
        Assert.Equal("Jazz Classics", playlist.Name);
    }

    [Fact]
    public void GetRadioStation_WithValidId_ShouldReturnRadioStation()
    {
        // Act
        var station = _testState.GetRadioStation("station-1");

        // Assert
        Assert.NotNull(station);
        Assert.Equal("station-1", station.Id);
        Assert.Equal("Jazz FM", station.Name);
    }

    [Fact]
    public void GetTrack_WithValidId_ShouldReturnTrack()
    {
        // Act
        var track = _testState.GetTrack("track-1");

        // Assert
        Assert.NotNull(track);
        Assert.Equal("track-1", track.Id);
        Assert.Equal("Blue Note", track.Title);
    }

    [Fact]
    public void GetClientsInZone_WithValidZone_ShouldReturnClients()
    {
        // Act
        var clients = _testState.GetClientsInZone("zone-1").ToList();

        // Assert
        Assert.Single(clients);
        Assert.Equal("client-1", clients[0].Id);
        Assert.Equal("Living Room", clients[0].Name);
    }

    [Fact]
    public void GetClientsInZone_WithEmptyZone_ShouldReturnEmpty()
    {
        // Act
        var clients = _testState.GetClientsInZone("zone-2").ToList();

        // Assert
        Assert.Empty(clients);
    }

    [Fact]
    public void GetClientsInZone_WithInvalidZone_ShouldReturnEmpty()
    {
        // Act
        var clients = _testState.GetClientsInZone("non-existent").ToList();

        // Assert
        Assert.Empty(clients);
    }

    [Fact]
    public void GetClientsInZone_WithNullOrEmptyZoneId_ShouldReturnEmpty()
    {
        // Act & Assert
        Assert.Empty(_testState.GetClientsInZone(null!));
        Assert.Empty(_testState.GetClientsInZone(""));
        Assert.Empty(_testState.GetClientsInZone("   "));
    }

    [Fact]
    public void GetTracksInPlaylist_WithValidPlaylist_ShouldReturnTracks()
    {
        // Act
        var tracks = _testState.GetTracksInPlaylist("playlist-1").ToList();

        // Assert
        Assert.Single(tracks);
        Assert.Equal("track-1", tracks[0].Id);
        Assert.Equal("Blue Note", tracks[0].Title);
    }

    [Fact]
    public void GetTracksInPlaylist_WithEmptyPlaylist_ShouldReturnEmpty()
    {
        // Act
        var tracks = _testState.GetTracksInPlaylist("playlist-2").ToList();

        // Assert
        Assert.Single(tracks); // playlist-2 has track-2
        Assert.Equal("track-2", tracks[0].Id);
    }

    [Fact]
    public void GetTracksInPlaylist_WithInvalidPlaylist_ShouldReturnEmpty()
    {
        // Act
        var tracks = _testState.GetTracksInPlaylist("non-existent").ToList();

        // Assert
        Assert.Empty(tracks);
    }

    [Fact]
    public void GetConnectedClients_ShouldReturnOnlyConnectedClients()
    {
        // Act
        var connectedClients = _testState.GetConnectedClients().ToList();

        // Assert
        Assert.Single(connectedClients);
        Assert.Equal("client-1", connectedClients[0].Id);
        Assert.True(connectedClients[0].IsConnected);
    }

    [Fact]
    public void GetActiveZones_ShouldReturnOnlyActiveZones()
    {
        // Act
        var activeZones = _testState.GetActiveZones().ToList();

        // Assert
        Assert.Single(activeZones);
        Assert.Equal("zone-1", activeZones[0].Id);
        Assert.True(activeZones[0].IsActive);
    }

    [Fact]
    public void GetEnabledRadioStations_ShouldReturnOnlyEnabledStations()
    {
        // Act
        var enabledStations = _testState.GetEnabledRadioStations().ToList();

        // Assert
        Assert.Single(enabledStations);
        Assert.Equal("station-1", enabledStations[0].Id);
        Assert.True(enabledStations[0].IsEnabled);
    }

    [Fact]
    public void GetPlayingStreams_ShouldReturnOnlyPlayingStreams()
    {
        // Act
        var playingStreams = _testState.GetPlayingStreams().ToList();

        // Assert
        Assert.Single(playingStreams);
        Assert.Equal("stream-1", playingStreams[0].Id);
        Assert.True(playingStreams[0].IsPlaying);
    }

    [Fact]
    public void IsClientConnected_WithConnectedClient_ShouldReturnTrue()
    {
        // Act
        var isConnected = _testState.IsClientConnected("client-1");

        // Assert
        Assert.True(isConnected);
    }

    [Fact]
    public void IsClientConnected_WithDisconnectedClient_ShouldReturnFalse()
    {
        // Act
        var isConnected = _testState.IsClientConnected("client-2");

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void IsClientConnected_WithNonExistentClient_ShouldReturnFalse()
    {
        // Act
        var isConnected = _testState.IsClientConnected("non-existent");

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void HasConnectedClients_WithZoneWithConnectedClients_ShouldReturnTrue()
    {
        // Act
        var hasConnected = _testState.HasConnectedClients("zone-1");

        // Assert
        Assert.True(hasConnected);
    }

    [Fact]
    public void HasConnectedClients_WithZoneWithoutConnectedClients_ShouldReturnFalse()
    {
        // Act
        var hasConnected = _testState.HasConnectedClients("zone-2");

        // Assert
        Assert.False(hasConnected);
    }

    [Fact]
    public void GetCurrentStreamForZone_WithZoneHavingStream_ShouldReturnStream()
    {
        // Act
        var stream = _testState.GetCurrentStreamForZone("zone-1");

        // Assert
        Assert.NotNull(stream);
        Assert.Equal("stream-1", stream.Id);
        Assert.Equal("Jazz Stream", stream.Name);
    }

    [Fact]
    public void GetCurrentStreamForZone_WithZoneWithoutStream_ShouldReturnNull()
    {
        // Act
        var stream = _testState.GetCurrentStreamForZone("zone-2");

        // Assert
        Assert.Null(stream);
    }

    [Fact]
    public void GetCurrentStreamForZone_WithNonExistentZone_ShouldReturnNull()
    {
        // Act
        var stream = _testState.GetCurrentStreamForZone("non-existent");

        // Assert
        Assert.Null(stream);
    }

    [Fact]
    public void GetSummary_ShouldReturnCorrectStatistics()
    {
        // Act
        var summary = _testState.GetSummary();

        // Assert
        Assert.Equal(2, summary.TotalAudioStreams);
        Assert.Equal(1, summary.PlayingAudioStreams);
        Assert.Equal(2, summary.TotalClients);
        Assert.Equal(1, summary.ConnectedClients);
        Assert.Equal(2, summary.TotalZones);
        Assert.Equal(1, summary.ActiveZones);
        Assert.Equal(2, summary.TotalPlaylists);
        Assert.Equal(2, summary.TotalRadioStations);
        Assert.Equal(1, summary.EnabledRadioStations);
        Assert.Equal(2, summary.TotalTracks);
        Assert.Equal(SystemStatus.Running, summary.SystemStatus);
        Assert.True(summary.IsValid);
        Assert.True(summary.LastUpdated <= DateTime.UtcNow);
        Assert.True(summary.Version > 0);
    }

    [Fact]
    public void StateExtensions_WithEmptyState_ShouldHandleGracefully()
    {
        // Arrange
        var emptyState = SnapDogState.CreateEmpty();

        // Act & Assert
        Assert.Null(emptyState.GetAudioStream("any-id"));
        Assert.Null(emptyState.GetClient("any-id"));
        Assert.Null(emptyState.GetZone("any-id"));
        Assert.Null(emptyState.GetPlaylist("any-id"));
        Assert.Null(emptyState.GetRadioStation("any-id"));
        Assert.Null(emptyState.GetTrack("any-id"));

        Assert.Empty(emptyState.GetClientsInZone("any-zone"));
        Assert.Empty(emptyState.GetTracksInPlaylist("any-playlist"));
        Assert.Empty(emptyState.GetConnectedClients());
        Assert.Empty(emptyState.GetActiveZones());
        Assert.Empty(emptyState.GetEnabledRadioStations());
        Assert.Empty(emptyState.GetPlayingStreams());

        Assert.False(emptyState.IsClientConnected("any-client"));
        Assert.False(emptyState.HasConnectedClients("any-zone"));
        Assert.Null(emptyState.GetCurrentStreamForZone("any-zone"));

        var summary = emptyState.GetSummary();
        Assert.Equal(0, summary.TotalAudioStreams);
        Assert.Equal(0, summary.PlayingAudioStreams);
        Assert.Equal(0, summary.TotalClients);
        Assert.Equal(0, summary.ConnectedClients);
        Assert.Equal(0, summary.TotalZones);
        Assert.Equal(0, summary.ActiveZones);
        Assert.Equal(0, summary.TotalPlaylists);
        Assert.Equal(0, summary.TotalRadioStations);
        Assert.Equal(0, summary.EnabledRadioStations);
        Assert.Equal(0, summary.TotalTracks);
        Assert.Equal(SystemStatus.Stopped, summary.SystemStatus);
    }

    [Fact]
    public void StateExtensions_ComplexQueries_ShouldReturnCorrectResults()
    {
        // Test multiple interconnected queries
        var activeZones = _testState.GetActiveZones().ToList();
        Assert.Single(activeZones);

        var zone = activeZones[0];
        var clientsInZone = _testState.GetClientsInZone(zone.Id).ToList();
        Assert.Single(clientsInZone);

        var connectedClientsInZone = clientsInZone.Where(c => c.IsConnected).ToList();
        Assert.Single(connectedClientsInZone);

        var currentStream = _testState.GetCurrentStreamForZone(zone.Id);
        Assert.NotNull(currentStream);
        Assert.True(currentStream.IsPlaying);

        // Verify the relationships are consistent
        Assert.Equal(zone.CurrentStreamId, currentStream.Id);
        Assert.Contains(clientsInZone[0].Id, zone.ClientIds);
        Assert.Equal(zone.Id, clientsInZone[0].ZoneId);
    }

    [Fact]
    public void StateExtensions_FilteringOperations_ShouldWork()
    {
        // Test various filtering scenarios
        var allStreams = _testState.AudioStreams.Values;
        var playingStreams = _testState.GetPlayingStreams().ToList();
        var stoppedStreams = allStreams.Where(s => s.Status == StreamStatus.Stopped).ToList();

        Assert.Equal(2, allStreams.Count());
        Assert.Single(playingStreams);
        Assert.Single(stoppedStreams);

        var allClients = _testState.Clients.Values;
        var connectedClients = _testState.GetConnectedClients().ToList();
        var disconnectedClients = allClients.Where(c => !c.IsConnected).ToList();

        Assert.Equal(2, allClients.Count());
        Assert.Single(connectedClients);
        Assert.Single(disconnectedClients);
    }

    [Fact]
    public void StateExtensions_StateSummary_ShouldReflectChanges()
    {
        // Create a modified state
        var client3 = Client
            .Create("client-3", "Bedroom", new MacAddress("AA:BB:CC:DD:EE:03"), new IpAddress("192.168.1.103"))
            .WithStatus(ClientStatus.Connected);

        var modifiedState = _testState.WithClient(client3);
        var summary = modifiedState.GetSummary();

        // Verify the summary reflects the changes
        Assert.Equal(3, summary.TotalClients);
        Assert.Equal(2, summary.ConnectedClients); // client-1 and client-3
        Assert.True(summary.Version > _testState.Version);
    }
}
