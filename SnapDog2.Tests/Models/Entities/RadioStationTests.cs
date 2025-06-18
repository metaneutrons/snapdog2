using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the RadioStation domain entity.
/// Tests station management, metadata, authentication, and business logic.
/// </summary>
public class RadioStationTests
{
    private readonly StreamUrl _validUrl = new("http://stream.example.com:8000/radio");
    private const string ValidId = "test-station";
    private const string ValidName = "Test Radio Station";
    private const AudioCodec ValidCodec = AudioCodec.MP3;
    private const string ValidDescription = "Test Description";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateRadioStation()
    {
        // Act
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);

        // Assert
        Assert.Equal(ValidId, station.Id);
        Assert.Equal(ValidName, station.Name);
        Assert.Equal(_validUrl, station.Url);
        Assert.Equal(ValidCodec, station.Codec);
        Assert.Null(station.Description);
        Assert.Null(station.Genre);
        Assert.Null(station.Country);
        Assert.Null(station.Language);
        Assert.Null(station.BitrateKbps);
        Assert.Null(station.SampleRateHz);
        Assert.Null(station.Channels);
        Assert.Null(station.Website);
        Assert.Null(station.LogoUrl);
        Assert.Null(station.Tags);
        Assert.True(station.IsEnabled);
        Assert.Equal(1, station.Priority);
        Assert.False(station.RequiresAuth);
        Assert.Null(station.Username);
        Assert.Null(station.Password);
        Assert.Equal(0, station.PlayCount);
        Assert.True(station.CreatedAt <= DateTime.UtcNow);
        Assert.Null(station.UpdatedAt);
        Assert.Null(station.LastPlayedAt);
        Assert.Null(station.LastCheckedAt);
        Assert.Null(station.IsOnline);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateRadioStationWithValues()
    {
        // Act
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidDescription);

        // Assert
        Assert.Equal(ValidDescription, station.Description);
        Assert.Equal(ValidId, station.Id);
        Assert.Equal(ValidName, station.Name);
        Assert.Equal(_validUrl, station.Url);
        Assert.Equal(ValidCodec, station.Codec);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => RadioStation.Create(invalidId!, ValidName, _validUrl, ValidCodec)
        );
        Assert.Contains("Radio station ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => RadioStation.Create(ValidId, invalidName!, _validUrl, ValidCodec)
        );
        Assert.Contains("Radio station name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithMetadata_ShouldUpdateMetadataFields()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        const string newDescription = "New Description";
        const string newGenre = "Jazz";
        const string newCountry = "USA";
        const string newLanguage = "English";

        // Act
        var updatedStation = station.WithMetadata(newDescription, newGenre, newCountry, newLanguage);

        // Assert
        Assert.Equal(newDescription, updatedStation.Description);
        Assert.Equal(newGenre, updatedStation.Genre);
        Assert.Equal(newCountry, updatedStation.Country);
        Assert.Equal(newLanguage, updatedStation.Language);
        Assert.NotNull(updatedStation.UpdatedAt);
        Assert.Equal(station.Id, updatedStation.Id);
        Assert.Equal(station.Name, updatedStation.Name);
    }

    [Fact]
    public void WithMetadata_WithNullValues_ShouldPreserveExistingValues()
    {
        // Arrange
        var station = RadioStation
            .Create(ValidId, ValidName, _validUrl, ValidCodec, ValidDescription)
            .WithMetadata(genre: "Rock", country: "Germany", language: "German");

        // Act - Pass null values to preserve existing
        var updatedStation = station.WithMetadata();

        // Assert
        Assert.Equal(ValidDescription, updatedStation.Description);
        Assert.Equal("Rock", updatedStation.Genre);
        Assert.Equal("Germany", updatedStation.Country);
        Assert.Equal("German", updatedStation.Language);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Fact]
    public void WithTechnicalInfo_ShouldUpdateTechnicalFields()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        const int bitrate = 320;
        const int sampleRate = 44100;
        const int channels = 2;

        // Act
        var updatedStation = station.WithTechnicalInfo(bitrate, sampleRate, channels);

        // Assert
        Assert.Equal(bitrate, updatedStation.BitrateKbps);
        Assert.Equal(sampleRate, updatedStation.SampleRateHz);
        Assert.Equal(channels, updatedStation.Channels);
        Assert.NotNull(updatedStation.UpdatedAt);
        Assert.Equal(station.Id, updatedStation.Id);
        Assert.Equal(station.Name, updatedStation.Name);
    }

    [Fact]
    public void WithTechnicalInfo_WithNullValues_ShouldPreserveExistingValues()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec).WithTechnicalInfo(256, 44100, 2);

        // Act - Pass null values to preserve existing
        var updatedStation = station.WithTechnicalInfo();

        // Assert
        Assert.Equal(256, updatedStation.BitrateKbps);
        Assert.Equal(44100, updatedStation.SampleRateHz);
        Assert.Equal(2, updatedStation.Channels);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Fact]
    public void WithUrl_ShouldUpdateUrl()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        var newUrl = new StreamUrl("https://newstream.example.com/radio");

        // Act
        var updatedStation = station.WithUrl(newUrl);

        // Assert
        Assert.Equal(newUrl, updatedStation.Url);
        Assert.NotNull(updatedStation.UpdatedAt);
        Assert.Equal(station.Id, updatedStation.Id);
        Assert.Equal(station.Name, updatedStation.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithEnabled_ShouldUpdateEnabledStatus(bool enabled)
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);

        // Act
        var updatedStation = station.WithEnabled(enabled);

        // Assert
        Assert.Equal(enabled, updatedStation.IsEnabled);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Fact]
    public void WithAuth_WithAuthRequired_ShouldSetAuthenticationSettings()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        const string username = "testuser";
        const string password = "testpass";

        // Act
        var updatedStation = station.WithAuth(true, username, password);

        // Assert
        Assert.True(updatedStation.RequiresAuth);
        Assert.Equal(username, updatedStation.Username);
        Assert.Equal(password, updatedStation.Password);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Fact]
    public void WithAuth_WithAuthNotRequired_ShouldClearAuthenticationSettings()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec).WithAuth(true, "user", "pass");

        // Act
        var updatedStation = station.WithAuth(false);

        // Assert
        Assert.False(updatedStation.RequiresAuth);
        Assert.Null(updatedStation.Username);
        Assert.Null(updatedStation.Password);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Fact]
    public void WithPlayIncrement_ShouldIncrementPlayCountAndUpdateTimes()
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        var originalPlayCount = station.PlayCount;

        // Act
        var updatedStation = station.WithPlayIncrement();

        // Assert
        Assert.Equal(originalPlayCount + 1, updatedStation.PlayCount);
        Assert.NotNull(updatedStation.LastPlayedAt);
        Assert.True(updatedStation.LastPlayedAt <= DateTime.UtcNow);
        Assert.NotNull(updatedStation.UpdatedAt);
        Assert.True(updatedStation.HasBeenPlayed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithOnlineStatus_ShouldUpdateOnlineStatusAndCheckTime(bool isOnline)
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);

        // Act
        var updatedStation = station.WithOnlineStatus(isOnline);

        // Assert
        Assert.Equal(isOnline, updatedStation.IsOnline);
        Assert.NotNull(updatedStation.LastCheckedAt);
        Assert.True(updatedStation.LastCheckedAt <= DateTime.UtcNow);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void WithPriority_ShouldUpdatePriority(int priority)
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);

        // Act
        var updatedStation = station.WithPriority(priority);

        // Assert
        Assert.Equal(priority, updatedStation.Priority);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    public void HasBeenPlayed_WithDifferentPlayCounts_ShouldReturnCorrectValue(int playCount, bool expectedResult)
    {
        // Arrange
        var station = new RadioStation
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = ValidCodec,
            PlayCount = playCount,
        };

        // Act & Assert
        Assert.Equal(expectedResult, station.HasBeenPlayed);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(6, true)]
    [InlineData(null, false)]
    public void IsStereo_WithDifferentChannelCounts_ShouldReturnCorrectValue(int? channels, bool expectedStereo)
    {
        // Arrange
        var station = new RadioStation
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = ValidCodec,
            Channels = channels,
        };

        // Act & Assert
        Assert.Equal(expectedStereo, station.IsStereo);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, null, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, null, false)]
    public void IsAvailable_WithDifferentStates_ShouldReturnCorrectValue(
        bool isEnabled,
        bool? isOnline,
        bool expectedAvailable
    )
    {
        // Arrange
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec).WithEnabled(isEnabled);

        if (isOnline.HasValue)
        {
            station = station.WithOnlineStatus(isOnline.Value);
        }

        // Act & Assert
        Assert.Equal(expectedAvailable, station.IsAvailable);
    }

    [Theory]
    [InlineData(null, null, null, false)]
    [InlineData("Rock", null, null, false)]
    [InlineData("Rock", "USA", null, false)]
    [InlineData("Rock", "USA", "English", true)]
    [InlineData(null, "USA", "English", false)]
    public void HasCompleteMetadata_WithDifferentMetadata_ShouldReturnCorrectValue(
        string? genre,
        string? country,
        string? language,
        bool expectedResult
    )
    {
        // Arrange
        var station = RadioStation
            .Create(ValidId, ValidName, _validUrl, ValidCodec)
            .WithMetadata(genre: genre, country: country, language: language);

        // Act & Assert
        Assert.Equal(expectedResult, station.HasCompleteMetadata);
    }

    [Theory]
    [InlineData("Test Station", null, null, "Test Station")]
    [InlineData("Test Station", "USA", null, "Test Station (USA)")]
    [InlineData("Test Station", "USA", "Rock", "Test Station (USA) - Rock")]
    [InlineData("Test Station", null, "Rock", "Test Station - Rock")]
    public void DisplayName_WithDifferentMetadata_ShouldReturnCorrectFormat(
        string name,
        string? country,
        string? genre,
        string expectedDisplayName
    )
    {
        // Arrange
        var station = RadioStation
            .Create(ValidId, name, _validUrl, ValidCodec)
            .WithMetadata(genre: genre, country: country);

        // Act & Assert
        Assert.Equal(expectedDisplayName, station.DisplayName);
    }

    [Theory]
    [InlineData(null, "Unknown")]
    [InlineData(128, "128 kbps")]
    [InlineData(320, "320 kbps")]
    [InlineData(1411, "1411 kbps")]
    public void FormattedBitrate_WithDifferentBitrates_ShouldReturnCorrectFormat(int? bitrate, string expectedFormat)
    {
        // Arrange
        var station = new RadioStation
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = ValidCodec,
            BitrateKbps = bitrate,
        };

        // Act & Assert
        Assert.Equal(expectedFormat, station.FormattedBitrate);
    }

    [Theory]
    [InlineData(AudioCodec.MP3, null, null, "MP3")]
    [InlineData(AudioCodec.FLAC, 1411, null, "FLAC, 1411 kbps")]
    [InlineData(AudioCodec.MP3, 320, 2, "MP3, 320 kbps, Stereo")]
    [InlineData(AudioCodec.AAC, 256, 1, "AAC, 256 kbps, Mono")]
    [InlineData(AudioCodec.OGG, 128, 6, "OGG, 128 kbps, Stereo")]
    public void QualityInfo_WithDifferentSettings_ShouldReturnCorrectFormat(
        AudioCodec codec,
        int? bitrate,
        int? channels,
        string expectedQualityInfo
    )
    {
        // Arrange
        var station = new RadioStation
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = codec,
            BitrateKbps = bitrate,
            Channels = channels,
        };

        // Act & Assert
        Assert.Equal(expectedQualityInfo, station.QualityInfo);
    }

    [Fact]
    public void RadioStation_Immutability_ShouldCreateNewInstancesOnUpdate()
    {
        // Arrange
        var originalStation = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);

        // Act
        var updatedStation = originalStation.WithMetadata("New Description");

        // Assert
        Assert.NotSame(originalStation, updatedStation);
        Assert.Null(originalStation.Description);
        Assert.Equal("New Description", updatedStation.Description);
        Assert.Null(originalStation.UpdatedAt);
        Assert.NotNull(updatedStation.UpdatedAt);
    }

    [Fact]
    public void RadioStation_WithComplexScenario_ShouldMaintainConsistency()
    {
        // Arrange
        var station = RadioStation.Create(
            "jazz-fm",
            "Jazz FM",
            new StreamUrl("http://jazz.example.com:8000/stream"),
            AudioCodec.AAC,
            "Best jazz music 24/7"
        );

        // Act - Simulate a complex scenario with multiple updates
        var configuredStation = station
            .WithMetadata(genre: "Jazz", country: "USA", language: "English")
            .WithTechnicalInfo(256, 44100, 2)
            .WithAuth(true, "jazzfm", "secret123")
            .WithPriority(5)
            .WithEnabled(true)
            .WithOnlineStatus(true)
            .WithPlayIncrement()
            .WithPlayIncrement();

        var finalStation = configuredStation.WithAuth(false).WithPriority(10);

        // Assert
        Assert.Equal("jazz-fm", finalStation.Id);
        Assert.Equal("Jazz FM", finalStation.Name);
        Assert.Equal("Best jazz music 24/7", finalStation.Description);
        Assert.Equal("Jazz", finalStation.Genre);
        Assert.Equal("USA", finalStation.Country);
        Assert.Equal("English", finalStation.Language);
        Assert.Equal(AudioCodec.AAC, finalStation.Codec);
        Assert.Equal(256, finalStation.BitrateKbps);
        Assert.Equal(44100, finalStation.SampleRateHz);
        Assert.Equal(2, finalStation.Channels);
        Assert.False(finalStation.RequiresAuth);
        Assert.Null(finalStation.Username);
        Assert.Null(finalStation.Password);
        Assert.Equal(10, finalStation.Priority);
        Assert.True(finalStation.IsEnabled);
        Assert.True(finalStation.IsOnline);
        Assert.Equal(2, finalStation.PlayCount);
        Assert.True(finalStation.HasBeenPlayed);
        Assert.True(finalStation.IsStereo);
        Assert.True(finalStation.IsAvailable);
        Assert.True(finalStation.HasCompleteMetadata);
        Assert.Equal("Jazz FM (USA) - Jazz", finalStation.DisplayName);
        Assert.Equal("256 kbps", finalStation.FormattedBitrate);
        Assert.Equal("AAC, 256 kbps, Stereo", finalStation.QualityInfo);
        Assert.NotNull(finalStation.LastPlayedAt);
        Assert.NotNull(finalStation.LastCheckedAt);
        Assert.NotNull(finalStation.UpdatedAt);
        Assert.True(finalStation.UpdatedAt >= station.CreatedAt);
    }

    [Theory]
    [InlineData(AudioCodec.MP3)]
    [InlineData(AudioCodec.FLAC)]
    [InlineData(AudioCodec.OGG)]
    [InlineData(AudioCodec.AAC)]
    [InlineData(AudioCodec.PCM)]
    public void Create_WithDifferentCodecs_ShouldCreateSuccessfully(AudioCodec codec)
    {
        // Act
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, codec);

        // Assert
        Assert.Equal(codec, station.Codec);
    }

    [Fact]
    public void RadioStation_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var station = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);

        // Assert
        Assert.True(station.IsEnabled);
        Assert.Equal(1, station.Priority);
        Assert.False(station.RequiresAuth);
        Assert.Equal(0, station.PlayCount);
        Assert.False(station.HasBeenPlayed);
        Assert.False(station.HasCompleteMetadata);
        Assert.Equal(ValidName, station.DisplayName);
        Assert.Equal("Unknown", station.FormattedBitrate);
        Assert.Equal("MP3", station.QualityInfo);
        Assert.True(station.IsAvailable); // Enabled and IsOnline is null (defaults to true)
    }

    [Fact]
    public void RadioStation_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var station1 = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        var station2 = RadioStation.Create(ValidId, ValidName, _validUrl, ValidCodec);
        var station3 = RadioStation.Create("different-id", ValidName, _validUrl, ValidCodec);

        // Act & Assert
        Assert.Equal(station1.Id, station2.Id);
        Assert.NotEqual(station1, station3);
        Assert.NotSame(station1, station2);
    }

    [Fact]
    public void RadioStation_WithCompleteRadioStationInfo_ShouldSetAllFields()
    {
        // Arrange & Act
        var station = new RadioStation
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = ValidCodec,
            Description = "Great music station",
            Genre = "Pop",
            Country = "Germany",
            Language = "German",
            BitrateKbps = 320,
            SampleRateHz = 44100,
            Channels = 2,
            Website = "https://example.com",
            LogoUrl = "https://example.com/logo.png",
            Tags = "pop,music,german",
            IsEnabled = true,
            Priority = 5,
            RequiresAuth = true,
            Username = "user",
            Password = "pass",
            PlayCount = 10,
            IsOnline = true,
        };

        // Assert
        Assert.Equal("Great music station", station.Description);
        Assert.Equal("Pop", station.Genre);
        Assert.Equal("Germany", station.Country);
        Assert.Equal("German", station.Language);
        Assert.Equal(320, station.BitrateKbps);
        Assert.Equal(44100, station.SampleRateHz);
        Assert.Equal(2, station.Channels);
        Assert.Equal("https://example.com", station.Website);
        Assert.Equal("https://example.com/logo.png", station.LogoUrl);
        Assert.Equal("pop,music,german", station.Tags);
        Assert.True(station.IsEnabled);
        Assert.Equal(5, station.Priority);
        Assert.True(station.RequiresAuth);
        Assert.Equal("user", station.Username);
        Assert.Equal("pass", station.Password);
        Assert.Equal(10, station.PlayCount);
        Assert.True(station.IsOnline);
        Assert.True(station.HasCompleteMetadata);
        Assert.True(station.IsStereo);
        Assert.True(station.IsAvailable);
        Assert.True(station.HasBeenPlayed);
    }
}
