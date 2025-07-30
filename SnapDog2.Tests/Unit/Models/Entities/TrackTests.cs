using SnapDog2.Core.Models.Entities;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the Track domain entity.
/// Tests track metadata, duration calculations, validation, and business logic.
/// </summary>
public class TrackTests
{
    private const string ValidId = "test-track";
    private const string ValidTitle = "Test Track";
    private const string ValidArtist = "Test Artist";
    private const string ValidAlbum = "Test Album";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateTrack()
    {
        // Act
        var track = Track.Create(ValidId, ValidTitle);

        // Assert
        Assert.Equal(ValidId, track.Id);
        Assert.Equal(ValidTitle, track.Title);
        Assert.Null(track.Artist);
        Assert.Null(track.Album);
        Assert.Null(track.Genre);
        Assert.Null(track.Year);
        Assert.Null(track.TrackNumber);
        Assert.Null(track.TotalTracks);
        Assert.Null(track.DurationSeconds);
        Assert.Null(track.FilePath);
        Assert.Null(track.FileSizeBytes);
        Assert.Null(track.BitrateKbps);
        Assert.Null(track.SampleRateHz);
        Assert.Null(track.Channels);
        Assert.Null(track.Format);
        Assert.Empty(track.Tags);
        Assert.Equal(0, track.PlayCount);
        Assert.True(track.CreatedAt <= DateTime.UtcNow);
        Assert.Null(track.UpdatedAt);
        Assert.Null(track.LastPlayedAt);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateTrackWithValues()
    {
        // Act
        var track = Track.Create(ValidId, ValidTitle, ValidArtist, ValidAlbum);

        // Assert
        Assert.Equal(ValidArtist, track.Artist);
        Assert.Equal(ValidAlbum, track.Album);
        Assert.Equal(ValidId, track.Id);
        Assert.Equal(ValidTitle, track.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Track.Create(invalidId!, ValidTitle));
        Assert.Contains("Track ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Track.Create(ValidId, invalidTitle!));
        Assert.Contains("Track title cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithMetadata_ShouldUpdateMetadataFields()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);
        const string newArtist = "New Artist";
        const string newAlbum = "New Album";
        const string newGenre = "Rock";
        const int newYear = 2023;

        // Act
        var updatedTrack = track.WithMetadata(newArtist, newAlbum, newGenre, newYear);

        // Assert
        Assert.Equal(newArtist, updatedTrack.Artist);
        Assert.Equal(newAlbum, updatedTrack.Album);
        Assert.Equal(newGenre, updatedTrack.Genre);
        Assert.Equal(newYear, updatedTrack.Year);
        Assert.NotNull(updatedTrack.UpdatedAt);
        Assert.Equal(track.Id, updatedTrack.Id);
        Assert.Equal(track.Title, updatedTrack.Title);
    }

    [Fact]
    public void WithMetadata_WithNullValues_ShouldPreserveExistingValues()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle, ValidArtist, ValidAlbum).WithMetadata(genre: "Jazz", year: 2020);

        // Act - Pass null values to preserve existing
        var updatedTrack = track.WithMetadata();

        // Assert
        Assert.Equal(ValidArtist, updatedTrack.Artist);
        Assert.Equal(ValidAlbum, updatedTrack.Album);
        Assert.Equal("Jazz", updatedTrack.Genre);
        Assert.Equal(2020, updatedTrack.Year);
        Assert.NotNull(updatedTrack.UpdatedAt);
    }

    [Fact]
    public void WithTechnicalInfo_ShouldUpdateTechnicalFields()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);
        const int duration = 240; // 4 minutes
        const int bitrate = 320;
        const int sampleRate = 44100;
        const int channels = 2;
        const string format = "MP3";

        // Act
        var updatedTrack = track.WithTechnicalInfo(duration, bitrate, sampleRate, channels, format);

        // Assert
        Assert.Equal(duration, updatedTrack.DurationSeconds);
        Assert.Equal(bitrate, updatedTrack.BitrateKbps);
        Assert.Equal(sampleRate, updatedTrack.SampleRateHz);
        Assert.Equal(channels, updatedTrack.Channels);
        Assert.Equal(format, updatedTrack.Format);
        Assert.NotNull(updatedTrack.UpdatedAt);
        Assert.Equal(track.Id, updatedTrack.Id);
        Assert.Equal(track.Title, updatedTrack.Title);
    }

    [Fact]
    public void WithTechnicalInfo_WithNullValues_ShouldPreserveExistingValues()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle).WithTechnicalInfo(180, 256, 44100, 2, "FLAC");

        // Act - Pass null values to preserve existing
        var updatedTrack = track.WithTechnicalInfo();

        // Assert
        Assert.Equal(180, updatedTrack.DurationSeconds);
        Assert.Equal(256, updatedTrack.BitrateKbps);
        Assert.Equal(44100, updatedTrack.SampleRateHz);
        Assert.Equal(2, updatedTrack.Channels);
        Assert.Equal("FLAC", updatedTrack.Format);
        Assert.NotNull(updatedTrack.UpdatedAt);
    }

    [Fact]
    public void WithTag_WithValidKeyAndValue_ShouldAddTag()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);
        const string key = "mood";
        const string value = "energetic";

        // Act
        var updatedTrack = track.WithTag(key, value);

        // Assert
        Assert.Single(updatedTrack.Tags);
        Assert.True(updatedTrack.Tags.ContainsKey(key));
        Assert.Equal(value, updatedTrack.Tags[key]);
        Assert.NotNull(updatedTrack.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithTag_WithInvalidKey_ShouldThrowArgumentException(string? invalidKey)
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => track.WithTag(invalidKey!, "value"));
        Assert.Contains("Tag key cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithTag_WithNullValue_ShouldAddEmptyStringValue()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);
        const string key = "test";

        // Act
        var updatedTrack = track.WithTag(key, null!);

        // Assert
        Assert.Single(updatedTrack.Tags);
        Assert.True(updatedTrack.Tags.ContainsKey(key));
        Assert.Equal(string.Empty, updatedTrack.Tags[key]);
    }

    [Fact]
    public void WithoutTag_WithExistingKey_ShouldRemoveTag()
    {
        // Arrange
        const string key = "mood";
        var track = Track.Create(ValidId, ValidTitle).WithTag(key, "energetic");

        // Act
        var updatedTrack = track.WithoutTag(key);

        // Assert
        Assert.Empty(updatedTrack.Tags);
        Assert.False(updatedTrack.Tags.ContainsKey(key));
        Assert.NotNull(updatedTrack.UpdatedAt);
    }

    [Fact]
    public void WithoutTag_WithNonExistentKey_ShouldReturnSameInstance()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);

        // Act
        var updatedTrack = track.WithoutTag("non-existent");

        // Assert
        Assert.Same(track, updatedTrack);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithoutTag_WithInvalidKey_ShouldReturnSameInstance(string? invalidKey)
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);

        // Act
        var updatedTrack = track.WithoutTag(invalidKey!);

        // Assert
        Assert.Same(track, updatedTrack);
    }

    [Fact]
    public void WithPlayIncrement_ShouldIncrementPlayCountAndUpdateTimes()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);
        var originalPlayCount = track.PlayCount;

        // Act
        var updatedTrack = track.WithPlayIncrement();

        // Assert
        Assert.Equal(originalPlayCount + 1, updatedTrack.PlayCount);
        Assert.NotNull(updatedTrack.LastPlayedAt);
        Assert.True(updatedTrack.LastPlayedAt <= DateTime.UtcNow);
        Assert.NotNull(updatedTrack.UpdatedAt);
        Assert.True(updatedTrack.HasBeenPlayed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(65)]
    [InlineData(3661)]
    [InlineData(59)]
    public void Duration_WithDifferentDurationSeconds_ShouldReturnCorrectTimeSpan(int? durationSeconds)
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle).WithTechnicalInfo(durationSeconds: durationSeconds);

        // Act & Assert
        if (durationSeconds.HasValue)
        {
            Assert.NotNull(track.Duration);
            Assert.Equal(TimeSpan.FromSeconds(durationSeconds.Value), track.Duration);
        }
        else
        {
            Assert.Null(track.Duration);
        }
    }

    [Theory]
    [InlineData(null, "--:--")]
    [InlineData(65, "1:05")]
    [InlineData(3661, "1:01:01")]
    [InlineData(59, "0:59")]
    public void FormattedDuration_WithDifferentDurations_ShouldReturnCorrectFormat(
        int? durationSeconds,
        string expectedFormat
    )
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle).WithTechnicalInfo(durationSeconds: durationSeconds);

        // Act & Assert
        Assert.Equal(expectedFormat, track.FormattedDuration);
    }

    [Theory]
    [InlineData(null, "Unknown")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(2621440L, "2.5 MB")]
    public void FormattedFileSize_WithDifferentFileSizes_ShouldReturnCorrectFormat(
        long? fileSizeBytes,
        string expectedFormat
    )
    {
        // Arrange
        var track = new Track
        {
            Id = ValidId,
            Title = ValidTitle,
            FileSizeBytes = fileSizeBytes,
        };

        // Act & Assert
        Assert.Equal(expectedFormat, track.FormattedFileSize);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    public void HasBeenPlayed_WithDifferentPlayCounts_ShouldReturnCorrectValue(int playCount, bool expectedResult)
    {
        // Arrange
        var track = new Track
        {
            Id = ValidId,
            Title = ValidTitle,
            PlayCount = playCount,
        };

        // Act & Assert
        Assert.Equal(expectedResult, track.HasBeenPlayed);
    }

    [Theory]
    [InlineData(null, null, null, false)]
    [InlineData("Artist", null, null, false)]
    [InlineData("Artist", "Album", null, false)]
    [InlineData("Artist", "Album", "Genre", true)]
    [InlineData(null, "Album", "Genre", false)]
    public void HasCompleteMetadata_WithDifferentMetadata_ShouldReturnCorrectValue(
        string? artist,
        string? album,
        string? genre,
        bool expectedResult
    )
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle).WithMetadata(artist, album, genre);

        // Act & Assert
        Assert.Equal(expectedResult, track.HasCompleteMetadata);
    }

    [Theory]
    [InlineData(null, "Test Track")]
    [InlineData("", "Test Track")]
    [InlineData("   ", "Test Track")]
    [InlineData("Test Artist", "Test Artist - Test Track")]
    public void DisplayName_WithDifferentArtists_ShouldReturnCorrectFormat(string? artist, string expectedDisplayName)
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle).WithMetadata(artist: artist);

        // Act & Assert
        Assert.Equal(expectedDisplayName, track.DisplayName);
    }

    [Fact]
    public void Track_Immutability_ShouldCreateNewInstancesOnUpdate()
    {
        // Arrange
        var originalTrack = Track.Create(ValidId, ValidTitle);

        // Act
        var updatedTrack = originalTrack.WithMetadata(ValidArtist);

        // Assert
        Assert.NotSame(originalTrack, updatedTrack);
        Assert.Null(originalTrack.Artist);
        Assert.Equal(ValidArtist, updatedTrack.Artist);
        Assert.Null(originalTrack.UpdatedAt);
        Assert.NotNull(updatedTrack.UpdatedAt);
    }

    [Fact]
    public void Track_WithComplexScenario_ShouldMaintainConsistency()
    {
        // Arrange
        var track = Track.Create("bohemian-rhapsody", "Bohemian Rhapsody", "Queen", "A Night at the Opera");

        // Act - Simulate a complex scenario with multiple updates
        var enrichedTrack = track
            .WithMetadata(genre: "Rock", year: 1975)
            .WithTechnicalInfo(355, 320, 44100, 2, "MP3")
            .WithTag("mood", "epic")
            .WithTag("tempo", "variable")
            .WithTag("instruments", "piano,vocals,guitar,drums")
            .WithPlayIncrement()
            .WithPlayIncrement()
            .WithPlayIncrement();

        var finalTrack = enrichedTrack.WithoutTag("tempo").WithTag("classic", "true");

        // Assert
        Assert.Equal("bohemian-rhapsody", finalTrack.Id);
        Assert.Equal("Bohemian Rhapsody", finalTrack.Title);
        Assert.Equal("Queen", finalTrack.Artist);
        Assert.Equal("A Night at the Opera", finalTrack.Album);
        Assert.Equal("Rock", finalTrack.Genre);
        Assert.Equal(1975, finalTrack.Year);
        Assert.Equal(355, finalTrack.DurationSeconds);
        Assert.Equal("5:55", finalTrack.FormattedDuration);
        Assert.Equal(320, finalTrack.BitrateKbps);
        Assert.Equal(44100, finalTrack.SampleRateHz);
        Assert.Equal(2, finalTrack.Channels);
        Assert.Equal("MP3", finalTrack.Format);
        Assert.Equal(3, finalTrack.PlayCount);
        Assert.True(finalTrack.HasBeenPlayed);
        Assert.True(finalTrack.HasCompleteMetadata);
        Assert.Equal("Queen - Bohemian Rhapsody", finalTrack.DisplayName);
        Assert.NotNull(finalTrack.LastPlayedAt);
        Assert.NotNull(finalTrack.UpdatedAt);
        Assert.True(finalTrack.UpdatedAt >= track.CreatedAt);

        // Check tags
        Assert.Equal(3, finalTrack.Tags.Count);
        Assert.True(finalTrack.Tags.ContainsKey("mood"));
        Assert.Equal("epic", finalTrack.Tags["mood"]);
        Assert.True(finalTrack.Tags.ContainsKey("classic"));
        Assert.Equal("true", finalTrack.Tags["classic"]);
        Assert.False(finalTrack.Tags.ContainsKey("tempo"));
    }

    [Fact]
    public void Track_WithMultipleTags_ShouldHandleCorrectly()
    {
        // Arrange
        var track = Track.Create(ValidId, ValidTitle);

        // Act
        var taggedTrack = track
            .WithTag("genre", "rock")
            .WithTag("mood", "energetic")
            .WithTag("tempo", "fast")
            .WithTag("language", "english")
            .WithTag("explicit", "false");

        // Assert
        Assert.Equal(5, taggedTrack.Tags.Count);
        Assert.Equal("rock", taggedTrack.Tags["genre"]);
        Assert.Equal("energetic", taggedTrack.Tags["mood"]);
        Assert.Equal("fast", taggedTrack.Tags["tempo"]);
        Assert.Equal("english", taggedTrack.Tags["language"]);
        Assert.Equal("false", taggedTrack.Tags["explicit"]);
    }

    [Fact]
    public void Track_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var track = Track.Create(ValidId, ValidTitle);

        // Assert
        Assert.Equal(0, track.PlayCount);
        Assert.False(track.HasBeenPlayed);
        Assert.False(track.HasCompleteMetadata);
        Assert.Equal(ValidTitle, track.DisplayName);
        Assert.Equal("--:--", track.FormattedDuration);
        Assert.Equal("Unknown", track.FormattedFileSize);
        Assert.Null(track.Duration);
        Assert.Empty(track.Tags);
    }

    [Fact]
    public void Track_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var track1 = Track.Create(ValidId, ValidTitle);
        var track2 = Track.Create(ValidId, ValidTitle);
        var track3 = Track.Create("different-id", ValidTitle);

        // Act & Assert
        Assert.Equal(track1.Id, track2.Id);
        Assert.NotEqual(track1, track3);
        Assert.NotSame(track1, track2);
    }

    [Fact]
    public void Track_WithCompleteAlbumInfo_ShouldSetAllFields()
    {
        // Arrange & Act
        var track = new Track
        {
            Id = ValidId,
            Title = ValidTitle,
            Artist = ValidArtist,
            Album = ValidAlbum,
            AlbumArtist = "Various Artists",
            TrackNumber = 5,
            TotalTracks = 12,
            Year = 2023,
            Genre = "Rock",
            Composer = "John Doe",
            Conductor = "Jane Smith",
            Label = "Test Records",
            ISRC = "USRC12345678",
            MusicBrainzTrackId = "12345678-1234-1234-1234-123456789012",
            MusicBrainzRecordingId = "87654321-4321-4321-4321-210987654321",
        };

        // Assert
        Assert.Equal("Various Artists", track.AlbumArtist);
        Assert.Equal(5, track.TrackNumber);
        Assert.Equal(12, track.TotalTracks);
        Assert.Equal(2023, track.Year);
        Assert.Equal("Rock", track.Genre);
        Assert.Equal("John Doe", track.Composer);
        Assert.Equal("Jane Smith", track.Conductor);
        Assert.Equal("Test Records", track.Label);
        Assert.Equal("USRC12345678", track.ISRC);
        Assert.Equal("12345678-1234-1234-1234-123456789012", track.MusicBrainzTrackId);
        Assert.Equal("87654321-4321-4321-4321-210987654321", track.MusicBrainzRecordingId);
    }
}
