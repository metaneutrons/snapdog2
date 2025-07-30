using System.Collections.Immutable;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.State;
using Xunit;

namespace SnapDog2.Tests.State;

/// <summary>
/// Unit tests for the SnapDogState class.
/// Tests state immutability, validation, factory methods, and entity collections.
/// </summary>
public class SnapDogStateTests
{
    [Fact]
    public void CreateEmpty_ShouldCreateEmptyState()
    {
        // Act
        var state = SnapDogState.CreateEmpty();

        // Assert
        Assert.NotNull(state);
        Assert.Equal(1, state.Version);
        Assert.Equal(SystemStatus.Stopped, state.SystemStatus);
        Assert.Empty(state.AudioStreams);
        Assert.Empty(state.Clients);
        Assert.Empty(state.Zones);
        Assert.Empty(state.Playlists);
        Assert.Empty(state.RadioStations);
        Assert.Empty(state.Tracks);
        Assert.Empty(state.Metadata);
        Assert.True(state.LastUpdated <= DateTime.UtcNow);
        Assert.Equal(0, state.TotalEntityCount);
        Assert.True(state.IsStopped);
        Assert.False(state.IsRunning);
        Assert.False(state.HasError);
    }

    [Fact]
    public void CreateWithStatus_ShouldCreateStateWithSpecifiedStatus()
    {
        // Act
        var state = SnapDogState.CreateWithStatus(SystemStatus.Running);

        // Assert
        Assert.Equal(SystemStatus.Running, state.SystemStatus);
        Assert.True(state.IsRunning);
        Assert.False(state.IsStopped);
        Assert.False(state.HasError);
    }

    [Fact]
    public void Constructor_ShouldCreateStateWithDefaults()
    {
        // Act
        var state = new SnapDogState();

        // Assert
        Assert.Equal(1, state.Version);
        Assert.Equal(SystemStatus.Stopped, state.SystemStatus);
        Assert.Empty(state.AudioStreams);
        Assert.Empty(state.Clients);
        Assert.Empty(state.Zones);
        Assert.Empty(state.Playlists);
        Assert.Empty(state.RadioStations);
        Assert.Empty(state.Tracks);
        Assert.Empty(state.Metadata);
        Assert.True(state.LastUpdated <= DateTime.UtcNow);
    }

    [Fact]
    public void WithSystemStatus_ShouldReturnNewStateWithUpdatedStatus()
    {
        // Arrange
        var originalState = SnapDogState.CreateEmpty();
        var originalVersion = originalState.Version;

        // Act
        var newState = originalState.WithSystemStatus(SystemStatus.Starting);

        // Assert
        Assert.NotEqual(originalState, newState); // Immutability
        Assert.Equal(SystemStatus.Stopped, originalState.SystemStatus);
        Assert.Equal(SystemStatus.Starting, newState.SystemStatus);
        Assert.True(newState.Version > originalVersion);
        Assert.True(newState.LastUpdated >= originalState.LastUpdated);
    }

    [Fact]
    public void WithAudioStream_ShouldAddStreamToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var stream = AudioStream.Create(
            "stream-1",
            "Stream 1",
            new StreamUrl("http://example.com"),
            AudioCodec.MP3,
            320
        );
        var originalVersion = state.Version;

        // Act
        var newState = state.WithAudioStream(stream);

        // Assert
        Assert.NotEqual(state, newState);
        Assert.Empty(state.AudioStreams);
        Assert.Single(newState.AudioStreams);
        Assert.True(newState.AudioStreams.ContainsKey("stream-1"));
        Assert.Equal(stream, newState.AudioStreams["stream-1"]);
        Assert.True(newState.Version > originalVersion);
        Assert.Equal(1, newState.TotalEntityCount);
    }

    [Fact]
    public void WithAudioStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => state.WithAudioStream(null!));
    }

    [Fact]
    public void WithAudioStream_WithExistingStreamId_ShouldUpdateStream()
    {
        // Arrange
        var stream1 = AudioStream.Create(
            "stream-1",
            "Stream 1",
            new StreamUrl("http://example.com"),
            AudioCodec.MP3,
            320
        );
        var stream2 = AudioStream.Create(
            "stream-1",
            "Stream 1 Updated",
            new StreamUrl("http://updated.com"),
            AudioCodec.FLAC,
            1411
        );
        var state = SnapDogState.CreateEmpty().WithAudioStream(stream1);

        // Act
        var newState = state.WithAudioStream(stream2);

        // Assert
        Assert.Single(newState.AudioStreams);
        Assert.Equal("Stream 1 Updated", newState.AudioStreams["stream-1"].Name);
        Assert.Equal(AudioCodec.FLAC, newState.AudioStreams["stream-1"].Codec);
    }

    [Fact]
    public void WithoutAudioStream_ShouldRemoveStreamFromCollection()
    {
        // Arrange
        var stream = AudioStream.Create(
            "stream-1",
            "Stream 1",
            new StreamUrl("http://example.com"),
            AudioCodec.MP3,
            320
        );
        var state = SnapDogState.CreateEmpty().WithAudioStream(stream);

        // Act
        var newState = state.WithoutAudioStream("stream-1");

        // Assert
        Assert.Single(state.AudioStreams);
        Assert.Empty(newState.AudioStreams);
        Assert.Equal(0, newState.TotalEntityCount);
    }

    [Fact]
    public void WithoutAudioStream_WithInvalidId_ShouldThrowArgumentException()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => state.WithoutAudioStream(""));
        Assert.Throws<ArgumentException>(() => state.WithoutAudioStream(null!));
        Assert.Throws<ArgumentException>(() => state.WithoutAudioStream("   "));
    }

    [Fact]
    public void WithoutAudioStream_WithNonExistentId_ShouldReturnSameState()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();

        // Act
        var newState = state.WithoutAudioStream("non-existent");

        // Assert
        Assert.Equal(state, newState);
    }

    [Fact]
    public void WithClient_ShouldAddClientToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var client = Client.Create(
            "client-1",
            "Client 1",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act
        var newState = state.WithClient(client);

        // Assert
        Assert.Single(newState.Clients);
        Assert.True(newState.Clients.ContainsKey("client-1"));
        Assert.Equal(client, newState.Clients["client-1"]);
    }

    [Fact]
    public void WithClient_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => state.WithClient(null!));
    }

    [Fact]
    public void WithZone_ShouldAddZoneToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var zone = Zone.Create("zone-1", "Zone 1");

        // Act
        var newState = state.WithZone(zone);

        // Assert
        Assert.Single(newState.Zones);
        Assert.True(newState.Zones.ContainsKey("zone-1"));
        Assert.Equal(zone, newState.Zones["zone-1"]);
    }

    [Fact]
    public void WithPlaylist_ShouldAddPlaylistToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var playlist = Playlist.Create("playlist-1", "My Playlist");

        // Act
        var newState = state.WithPlaylist(playlist);

        // Assert
        Assert.Single(newState.Playlists);
        Assert.True(newState.Playlists.ContainsKey("playlist-1"));
        Assert.Equal(playlist, newState.Playlists["playlist-1"]);
    }

    [Fact]
    public void WithRadioStation_ShouldAddRadioStationToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var radioStation = RadioStation.Create(
            "station-1",
            "Radio Station",
            new StreamUrl("http://radio.example.com"),
            AudioCodec.MP3
        );

        // Act
        var newState = state.WithRadioStation(radioStation);

        // Assert
        Assert.Single(newState.RadioStations);
        Assert.True(newState.RadioStations.ContainsKey("station-1"));
        Assert.Equal(radioStation, newState.RadioStations["station-1"]);
    }

    [Fact]
    public void WithTrack_ShouldAddTrackToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var track = Track.Create("track-1", "Track Title", "Artist Name");

        // Act
        var newState = state.WithTrack(track);

        // Assert
        Assert.Single(newState.Tracks);
        Assert.True(newState.Tracks.ContainsKey("track-1"));
        Assert.Equal(track, newState.Tracks["track-1"]);
    }

    [Fact]
    public void WithMetadata_ShouldAddMetadataToCollection()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var key = "test-key";
        var value = "test-value";

        // Act
        var newState = state.WithMetadata(key, value);

        // Assert
        Assert.Empty(state.Metadata);
        Assert.Single(newState.Metadata);
        Assert.True(newState.Metadata.ContainsKey(key));
        Assert.Equal(value, newState.Metadata[key]);
    }

    [Fact]
    public void WithMetadata_WithInvalidKey_ShouldThrowArgumentException()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => state.WithMetadata("", "value"));
        Assert.Throws<ArgumentException>(() => state.WithMetadata(null!, "value"));
        Assert.Throws<ArgumentException>(() => state.WithMetadata("   ", "value"));
    }

    [Fact]
    public void TotalEntityCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var stream = AudioStream.Create("stream-1", "Stream", new StreamUrl("http://example.com"), AudioCodec.MP3, 320);
        var client = Client.Create(
            "client-1",
            "Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );
        var zone = Zone.Create("zone-1", "Zone");
        var playlist = Playlist.Create("playlist-1", "Playlist");
        var radioStation = RadioStation.Create(
            "station-1",
            "Station",
            new StreamUrl("http://radio.com"),
            AudioCodec.MP3
        );
        var track = Track.Create("track-1", "Track", "Artist");

        // Act
        var state = SnapDogState
            .CreateEmpty()
            .WithAudioStream(stream)
            .WithClient(client)
            .WithZone(zone)
            .WithPlaylist(playlist)
            .WithRadioStation(radioStation)
            .WithTrack(track);

        // Assert
        Assert.Equal(6, state.TotalEntityCount);
    }

    [Fact]
    public void IsValid_WithValidState_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(
            "client-1",
            "Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );
        var zone = Zone.Create("zone-1", "Zone").WithAddedClient("client-1");
        var clientWithZone = client.WithZone("zone-1");

        var state = SnapDogState.CreateEmpty().WithClient(clientWithZone).WithZone(zone);

        // Act & Assert
        Assert.True(state.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidClientZoneReference_ShouldReturnFalse()
    {
        // Arrange - Client references non-existent zone
        var client = Client
            .Create("client-1", "Client", new MacAddress("AA:BB:CC:DD:EE:FF"), new IpAddress("192.168.1.100"))
            .WithZone("non-existent-zone");

        var state = SnapDogState.CreateEmpty().WithClient(client);

        // Act & Assert
        Assert.False(state.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidZoneClientReference_ShouldReturnFalse()
    {
        // Arrange - Zone references non-existent client
        var zone = Zone.Create("zone-1", "Zone").WithAddedClient("non-existent-client");
        var state = SnapDogState.CreateEmpty().WithZone(zone);

        // Act & Assert
        Assert.False(state.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidStreamReference_ShouldReturnFalse()
    {
        // Arrange - Zone references non-existent stream
        var zone = Zone.Create("zone-1", "Zone").WithCurrentStream("non-existent-stream");
        var state = SnapDogState.CreateEmpty().WithZone(zone);

        // Act & Assert
        Assert.False(state.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidPlaylistTrackReference_ShouldReturnFalse()
    {
        // Arrange - Playlist references non-existent track
        var playlist = Playlist.Create("playlist-1", "Playlist").WithAddedTrack("non-existent-track");
        var state = SnapDogState.CreateEmpty().WithPlaylist(playlist);

        // Act & Assert
        Assert.False(state.IsValid());
    }

    [Fact]
    public void SystemStatusProperties_ShouldReflectCurrentStatus()
    {
        // Test Running status
        var runningState = SnapDogState.CreateWithStatus(SystemStatus.Running);
        Assert.True(runningState.IsRunning);
        Assert.False(runningState.IsStopped);
        Assert.False(runningState.HasError);

        // Test Stopped status
        var stoppedState = SnapDogState.CreateWithStatus(SystemStatus.Stopped);
        Assert.False(stoppedState.IsRunning);
        Assert.True(stoppedState.IsStopped);
        Assert.False(stoppedState.HasError);

        // Test Error status
        var errorState = SnapDogState.CreateWithStatus(SystemStatus.Error);
        Assert.False(errorState.IsRunning);
        Assert.False(errorState.IsStopped);
        Assert.True(errorState.HasError);

        // Test other statuses
        var maintenanceState = SnapDogState.CreateWithStatus(SystemStatus.Maintenance);
        Assert.False(maintenanceState.IsRunning);
        Assert.False(maintenanceState.IsStopped);
        Assert.False(maintenanceState.HasError);
    }

    [Fact]
    public void StateImmutability_ShouldMaintainOriginalStateUnchanged()
    {
        // Arrange
        var originalState = SnapDogState.CreateEmpty();
        var stream = AudioStream.Create("stream-1", "Stream", new StreamUrl("http://example.com"), AudioCodec.MP3, 320);

        // Act
        var newState = originalState
            .WithAudioStream(stream)
            .WithSystemStatus(SystemStatus.Running)
            .WithMetadata("key", "value");

        // Assert - Original state should be unchanged
        Assert.Equal(SystemStatus.Stopped, originalState.SystemStatus);
        Assert.Empty(originalState.AudioStreams);
        Assert.Empty(originalState.Metadata);
        Assert.Equal(1, originalState.Version);

        // New state should have changes
        Assert.Equal(SystemStatus.Running, newState.SystemStatus);
        Assert.Single(newState.AudioStreams);
        Assert.Single(newState.Metadata);
        Assert.True(newState.Version > originalState.Version);
    }

    [Fact]
    public void VersionIncrement_ShouldBeMonotonic()
    {
        // Arrange
        var state = SnapDogState.CreateEmpty();
        var versions = new List<long> { state.Version };

        // Act - Perform multiple state updates
        for (int i = 0; i < 5; i++)
        {
            state = state.WithMetadata($"key-{i}", i);
            versions.Add(state.Version);
        }

        // Assert - Each version should be greater than the previous
        for (int i = 1; i < versions.Count; i++)
        {
            Assert.True(
                versions[i] > versions[i - 1],
                $"Version {versions[i]} at index {i} should be greater than {versions[i - 1]} at index {i - 1}"
            );
        }
    }

    [Fact]
    public void ComplexStateOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var stream = AudioStream.Create(
            "stream-1",
            "Jazz Stream",
            new StreamUrl("http://jazz.example.com"),
            AudioCodec.FLAC,
            1411
        );
        var client = Client.Create(
            "client-1",
            "Living Room Speaker",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );
        var zone = Zone.Create("zone-1", "Living Room");
        var track = Track.Create("track-1", "Blue Note", "Miles Davis");
        var playlist = Playlist.Create("playlist-1", "Jazz Classics");

        // Act - Build complex state
        var finalState = SnapDogState
            .CreateEmpty()
            .WithAudioStream(stream)
            .WithClient(client.WithZone("zone-1"))
            .WithZone(zone.WithAddedClient("client-1").WithCurrentStream("stream-1"))
            .WithTrack(track)
            .WithPlaylist(playlist.WithAddedTrack("track-1"))
            .WithSystemStatus(SystemStatus.Running)
            .WithMetadata("session-id", Guid.NewGuid().ToString());

        // Assert - Verify consistency
        Assert.Equal(5, finalState.TotalEntityCount);
        Assert.True(finalState.IsValid());
        Assert.Equal(SystemStatus.Running, finalState.SystemStatus);
        Assert.Single(finalState.Metadata);

        // Verify relationships
        var savedZone = finalState.Zones["zone-1"];
        var savedClient = finalState.Clients["client-1"];
        var savedPlaylist = finalState.Playlists["playlist-1"];

        Assert.Equal("stream-1", savedZone.CurrentStreamId);
        Assert.Contains("client-1", savedZone.ClientIds);
        Assert.Equal("zone-1", savedClient.ZoneId);
        Assert.Contains("track-1", savedPlaylist.TrackIds);
    }
}
