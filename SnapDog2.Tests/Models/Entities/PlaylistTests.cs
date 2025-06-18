using SnapDog2.Core.Models.Entities;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the Playlist domain entity.
/// Tests playlist operations, track management, shuffling, and business logic.
/// </summary>
public class PlaylistTests
{
    private const string ValidId = "test-playlist";
    private const string ValidName = "Test Playlist";
    private const string ValidDescription = "Test Description";
    private const string ValidOwner = "Test Owner";

    [Fact]
    public void Create_WithValidParameters_ShouldCreatePlaylist()
    {
        // Act
        var playlist = Playlist.Create(ValidId, ValidName);

        // Assert
        Assert.Equal(ValidId, playlist.Id);
        Assert.Equal(ValidName, playlist.Name);
        Assert.Null(playlist.Description);
        Assert.Null(playlist.Owner);
        Assert.Empty(playlist.TrackIds);
        Assert.True(playlist.IsPublic);
        Assert.False(playlist.IsSystem);
        Assert.Null(playlist.Tags);
        Assert.Null(playlist.CoverArtPath);
        Assert.Null(playlist.TotalDurationSeconds);
        Assert.Equal(0, playlist.PlayCount);
        Assert.True(playlist.CreatedAt <= DateTime.UtcNow);
        Assert.Null(playlist.UpdatedAt);
        Assert.Null(playlist.LastPlayedAt);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreatePlaylistWithValues()
    {
        // Act
        var playlist = Playlist.Create(ValidId, ValidName, ValidDescription, ValidOwner);

        // Assert
        Assert.Equal(ValidDescription, playlist.Description);
        Assert.Equal(ValidOwner, playlist.Owner);
        Assert.Equal(ValidId, playlist.Id);
        Assert.Equal(ValidName, playlist.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Playlist.Create(invalidId!, ValidName));
        Assert.Contains("Playlist ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Playlist.Create(ValidId, invalidName!));
        Assert.Contains("Playlist name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithAddedTrack_WithValidTrackId_ShouldAddTrack()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);
        const string trackId = "test-track";

        // Act
        var updatedPlaylist = playlist.WithAddedTrack(trackId);

        // Assert
        Assert.Contains(trackId, updatedPlaylist.TrackIds);
        Assert.Single(updatedPlaylist.TrackIds);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
        Assert.True(updatedPlaylist.HasTracks);
        Assert.Equal(1, updatedPlaylist.TrackCount);
        Assert.False(updatedPlaylist.IsEmpty);
        Assert.True(updatedPlaylist.ContainsTrack(trackId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithAddedTrack_WithInvalidTrackId_ShouldThrowArgumentException(string? invalidTrackId)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => playlist.WithAddedTrack(invalidTrackId!));
        Assert.Contains("Track ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithInsertedTrack_WithValidPosition_ShouldInsertTrack()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1").WithAddedTrack("track-3");
        const string trackId = "track-2";

        // Act
        var updatedPlaylist = playlist.WithInsertedTrack(trackId, 1);

        // Assert
        Assert.Equal(3, updatedPlaylist.TrackIds.Count);
        Assert.Equal("track-1", updatedPlaylist.TrackIds[0]);
        Assert.Equal("track-2", updatedPlaylist.TrackIds[1]);
        Assert.Equal("track-3", updatedPlaylist.TrackIds[2]);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithInsertedTrack_WithInvalidTrackId_ShouldThrowArgumentException(string? invalidTrackId)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => playlist.WithInsertedTrack(invalidTrackId!, 0));
        Assert.Contains("Track ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(10)]
    public void WithInsertedTrack_WithInvalidPosition_ShouldThrowArgumentException(int invalidPosition)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1").WithAddedTrack("track-2");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => playlist.WithInsertedTrack("new-track", invalidPosition)
        );
        Assert.Contains("Position must be between 0 and 2", exception.Message);
    }

    [Fact]
    public void WithRemovedTrack_WithExistingTrackId_ShouldRemoveTrack()
    {
        // Arrange
        const string trackId = "test-track";
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack(trackId);

        // Act
        var updatedPlaylist = playlist.WithRemovedTrack(trackId);

        // Assert
        Assert.DoesNotContain(trackId, updatedPlaylist.TrackIds);
        Assert.Empty(updatedPlaylist.TrackIds);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
        Assert.False(updatedPlaylist.HasTracks);
        Assert.Equal(0, updatedPlaylist.TrackCount);
        Assert.True(updatedPlaylist.IsEmpty);
        Assert.False(updatedPlaylist.ContainsTrack(trackId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithRemovedTrack_WithInvalidTrackId_ShouldThrowArgumentException(string? invalidTrackId)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => playlist.WithRemovedTrack(invalidTrackId!));
        Assert.Contains("Track ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithRemovedTrack_WithNonExistentTrackId_ShouldThrowArgumentException()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => playlist.WithRemovedTrack("non-existent"));
        Assert.Contains("Track 'non-existent' is not in playlist", exception.Message);
    }

    [Fact]
    public void WithRemovedTrackAt_WithValidPosition_ShouldRemoveTrack()
    {
        // Arrange
        var playlist = Playlist
            .Create(ValidId, ValidName)
            .WithAddedTrack("track-1")
            .WithAddedTrack("track-2")
            .WithAddedTrack("track-3");

        // Act
        var updatedPlaylist = playlist.WithRemovedTrackAt(1);

        // Assert
        Assert.Equal(2, updatedPlaylist.TrackIds.Count);
        Assert.Equal("track-1", updatedPlaylist.TrackIds[0]);
        Assert.Equal("track-3", updatedPlaylist.TrackIds[1]);
        Assert.DoesNotContain("track-2", updatedPlaylist.TrackIds);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(10)]
    public void WithRemovedTrackAt_WithInvalidPosition_ShouldThrowArgumentException(int invalidPosition)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1").WithAddedTrack("track-2");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => playlist.WithRemovedTrackAt(invalidPosition));
        Assert.Contains("Position must be between 0 and 1", exception.Message);
    }

    [Fact]
    public void WithTracks_WithValidTrackIds_ShouldSetTracks()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);
        var trackIds = new[] { "track-1", "track-2", "track-3" };

        // Act
        var updatedPlaylist = playlist.WithTracks(trackIds);

        // Assert
        Assert.Equal(3, updatedPlaylist.TrackIds.Count);
        Assert.Contains("track-1", updatedPlaylist.TrackIds);
        Assert.Contains("track-2", updatedPlaylist.TrackIds);
        Assert.Contains("track-3", updatedPlaylist.TrackIds);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
        Assert.True(updatedPlaylist.HasTracks);
        Assert.Equal(3, updatedPlaylist.TrackCount);
    }

    [Fact]
    public void WithTracks_WithEmptyList_ShouldClearTracks()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1").WithAddedTrack("track-2");

        // Act
        var updatedPlaylist = playlist.WithTracks(Array.Empty<string>());

        // Assert
        Assert.Empty(updatedPlaylist.TrackIds);
        Assert.False(updatedPlaylist.HasTracks);
        Assert.Equal(0, updatedPlaylist.TrackCount);
        Assert.True(updatedPlaylist.IsEmpty);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
    }

    [Fact]
    public void WithTracks_WithNullList_ShouldThrowArgumentNullException()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => playlist.WithTracks(null!));
    }

    [Fact]
    public void WithTracks_WithNullOrEmptyTrackIds_ShouldFilterThem()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);
        var trackIds = new[] { "track-1", "", "track-2", "   ", "track-3" };

        // Act
        var updatedPlaylist = playlist.WithTracks(trackIds);

        // Assert
        Assert.Equal(3, updatedPlaylist.TrackIds.Count);
        Assert.Contains("track-1", updatedPlaylist.TrackIds);
        Assert.Contains("track-2", updatedPlaylist.TrackIds);
        Assert.Contains("track-3", updatedPlaylist.TrackIds);
    }

    [Fact]
    public void WithMovedTrack_WithValidPositions_ShouldMoveTrack()
    {
        // Arrange
        var playlist = Playlist
            .Create(ValidId, ValidName)
            .WithAddedTrack("track-1")
            .WithAddedTrack("track-2")
            .WithAddedTrack("track-3")
            .WithAddedTrack("track-4");

        // Act - Move track from position 1 to position 3
        var updatedPlaylist = playlist.WithMovedTrack(1, 3);

        // Assert
        Assert.Equal("track-1", updatedPlaylist.TrackIds[0]);
        Assert.Equal("track-3", updatedPlaylist.TrackIds[1]);
        Assert.Equal("track-4", updatedPlaylist.TrackIds[2]);
        Assert.Equal("track-2", updatedPlaylist.TrackIds[3]);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(5, 1)]
    [InlineData(1, 5)]
    public void WithMovedTrack_WithInvalidPositions_ShouldThrowArgumentException(int fromPosition, int toPosition)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1").WithAddedTrack("track-2");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => playlist.WithMovedTrack(fromPosition, toPosition));
    }

    [Fact]
    public void WithMovedTrack_WithSamePosition_ShouldReturnSameInstance()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1").WithAddedTrack("track-2");

        // Act
        var updatedPlaylist = playlist.WithMovedTrack(1, 1);

        // Assert
        Assert.Same(playlist, updatedPlaylist);
    }

    [Fact]
    public void WithShuffledTracks_WithEmptyPlaylist_ShouldReturnSameInstance()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);

        // Act
        var shuffledPlaylist = playlist.WithShuffledTracks();

        // Assert
        Assert.Same(playlist, shuffledPlaylist);
    }

    [Fact]
    public void WithShuffledTracks_WithSingleTrack_ShouldReturnSameInstance()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1");

        // Act
        var shuffledPlaylist = playlist.WithShuffledTracks();

        // Assert
        Assert.Same(playlist, shuffledPlaylist);
    }

    [Fact]
    public void WithShuffledTracks_WithMultipleTracks_ShouldShuffleTracks()
    {
        // Arrange
        var playlist = Playlist
            .Create(ValidId, ValidName)
            .WithAddedTrack("track-1")
            .WithAddedTrack("track-2")
            .WithAddedTrack("track-3")
            .WithAddedTrack("track-4")
            .WithAddedTrack("track-5");

        var random = new Random(42); // Fixed seed for deterministic test

        // Act
        var shuffledPlaylist = playlist.WithShuffledTracks(random);

        // Assert
        Assert.Equal(5, shuffledPlaylist.TrackIds.Count);
        Assert.Contains("track-1", shuffledPlaylist.TrackIds);
        Assert.Contains("track-2", shuffledPlaylist.TrackIds);
        Assert.Contains("track-3", shuffledPlaylist.TrackIds);
        Assert.Contains("track-4", shuffledPlaylist.TrackIds);
        Assert.Contains("track-5", shuffledPlaylist.TrackIds);
        Assert.NotNull(shuffledPlaylist.UpdatedAt);

        // With a fixed seed, the shuffle should be deterministic and different from original
        Assert.NotEqual(playlist.TrackIds, shuffledPlaylist.TrackIds);
    }

    [Fact]
    public void WithPlayIncrement_ShouldIncrementPlayCountAndUpdateTimes()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);
        var originalPlayCount = playlist.PlayCount;

        // Act
        var updatedPlaylist = playlist.WithPlayIncrement();

        // Assert
        Assert.Equal(originalPlayCount + 1, updatedPlaylist.PlayCount);
        Assert.NotNull(updatedPlaylist.LastPlayedAt);
        Assert.True(updatedPlaylist.LastPlayedAt <= DateTime.UtcNow);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
        Assert.True(updatedPlaylist.HasBeenPlayed);
    }

    [Fact]
    public void WithTotalDuration_ShouldUpdateDuration()
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName);
        const int durationSeconds = 3600; // 1 hour

        // Act
        var updatedPlaylist = playlist.WithTotalDuration(durationSeconds);

        // Assert
        Assert.Equal(durationSeconds, updatedPlaylist.TotalDurationSeconds);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
        Assert.NotNull(updatedPlaylist.TotalDuration);
        Assert.Equal(TimeSpan.FromSeconds(durationSeconds), updatedPlaylist.TotalDuration);
    }

    [Theory]
    [InlineData(null, "--:--:--")]
    [InlineData(65, "01:05")]
    [InlineData(3661, "1:01:01")]
    [InlineData(59, "00:59")]
    public void FormattedTotalDuration_WithDifferentDurations_ShouldReturnCorrectFormat(
        int? durationSeconds,
        string expectedFormat
    )
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithTotalDuration(durationSeconds);

        // Act & Assert
        Assert.Equal(expectedFormat, playlist.FormattedTotalDuration);
    }

    [Theory]
    [InlineData("track-1", true)]
    [InlineData("track-2", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsTrack_WithDifferentTrackIds_ShouldReturnCorrectValue(string? trackId, bool expectedResult)
    {
        // Arrange
        var playlist = Playlist.Create(ValidId, ValidName).WithAddedTrack("track-1");

        // Act & Assert
        Assert.Equal(expectedResult, playlist.ContainsTrack(trackId!));
    }

    [Theory]
    [InlineData("track-1", 0)]
    [InlineData("track-2", 1)]
    [InlineData("track-3", 2)]
    [InlineData("non-existent", -1)]
    [InlineData("", -1)]
    [InlineData(null, -1)]
    public void GetTrackPosition_WithDifferentTrackIds_ShouldReturnCorrectPosition(
        string? trackId,
        int expectedPosition
    )
    {
        // Arrange
        var playlist = Playlist
            .Create(ValidId, ValidName)
            .WithAddedTrack("track-1")
            .WithAddedTrack("track-2")
            .WithAddedTrack("track-3");

        // Act & Assert
        Assert.Equal(expectedPosition, playlist.GetTrackPosition(trackId!));
    }

    [Fact]
    public void Playlist_Immutability_ShouldCreateNewInstancesOnUpdate()
    {
        // Arrange
        var originalPlaylist = Playlist.Create(ValidId, ValidName);

        // Act
        var updatedPlaylist = originalPlaylist.WithAddedTrack("test-track");

        // Assert
        Assert.NotSame(originalPlaylist, updatedPlaylist);
        Assert.Empty(originalPlaylist.TrackIds);
        Assert.Single(updatedPlaylist.TrackIds);
        Assert.Null(originalPlaylist.UpdatedAt);
        Assert.NotNull(updatedPlaylist.UpdatedAt);
    }

    [Fact]
    public void Playlist_WithComplexScenario_ShouldMaintainConsistency()
    {
        // Arrange
        var playlist = Playlist.Create("jazz-collection", "Jazz Collection", "Best jazz tracks", "Music Lover");

        // Act - Simulate a complex scenario with multiple updates
        var builtPlaylist = playlist
            .WithAddedTrack("miles-davis-1")
            .WithAddedTrack("john-coltrane-1")
            .WithAddedTrack("bill-evans-1")
            .WithInsertedTrack("charlie-parker-1", 1)
            .WithTotalDuration(7200) // 2 hours
            .WithPlayIncrement()
            .WithPlayIncrement();

        var reorderedPlaylist = builtPlaylist
            .WithMovedTrack(0, 3) // Move first track to end
            .WithRemovedTrack("bill-evans-1");

        // Assert
        Assert.Equal("jazz-collection", reorderedPlaylist.Id);
        Assert.Equal("Jazz Collection", reorderedPlaylist.Name);
        Assert.Equal("Best jazz tracks", reorderedPlaylist.Description);
        Assert.Equal("Music Lover", reorderedPlaylist.Owner);
        Assert.Equal(3, reorderedPlaylist.TrackCount);
        Assert.True(reorderedPlaylist.HasTracks);
        Assert.False(reorderedPlaylist.IsEmpty);
        Assert.Equal(2, reorderedPlaylist.PlayCount);
        Assert.True(reorderedPlaylist.HasBeenPlayed);
        Assert.Equal(7200, reorderedPlaylist.TotalDurationSeconds);
        Assert.Equal("2:00:00", reorderedPlaylist.FormattedTotalDuration);
        Assert.NotNull(reorderedPlaylist.LastPlayedAt);
        Assert.NotNull(reorderedPlaylist.UpdatedAt);
        Assert.True(reorderedPlaylist.UpdatedAt >= playlist.CreatedAt);

        // Check final track order
        Assert.Equal("charlie-parker-1", reorderedPlaylist.TrackIds[0]);
        Assert.Equal("john-coltrane-1", reorderedPlaylist.TrackIds[1]);
        Assert.Equal("miles-davis-1", reorderedPlaylist.TrackIds[2]);
    }

    [Fact]
    public void Playlist_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var playlist = Playlist.Create(ValidId, ValidName);

        // Assert
        Assert.True(playlist.IsPublic);
        Assert.False(playlist.IsSystem);
        Assert.Equal(0, playlist.PlayCount);
        Assert.False(playlist.HasBeenPlayed);
        Assert.True(playlist.IsEmpty);
        Assert.False(playlist.HasTracks);
        Assert.Equal(0, playlist.TrackCount);
        Assert.Null(playlist.TotalDuration);
        Assert.Equal("--:--:--", playlist.FormattedTotalDuration);
    }

    [Fact]
    public void Playlist_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var playlist1 = Playlist.Create(ValidId, ValidName);
        var playlist2 = Playlist.Create(ValidId, ValidName);
        var playlist3 = Playlist.Create("different-id", ValidName);

        // Act & Assert
        Assert.Equal(playlist1.Id, playlist2.Id);
        Assert.NotEqual(playlist1, playlist3);
        Assert.NotSame(playlist1, playlist2);
    }
}
