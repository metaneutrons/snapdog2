using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the AudioStream domain entity.
/// Tests stream creation, status changes, validation, and business logic.
/// </summary>
public class AudioStreamTests
{
    private readonly StreamUrl _validUrl = new("http://example.com/stream");
    private const string ValidId = "test-stream";
    private const string ValidName = "Test Stream";
    private const AudioCodec ValidCodec = AudioCodec.MP3;
    private const int ValidBitrate = 320;

    [Fact]
    public void Create_WithValidParameters_ShouldCreateAudioStream()
    {
        // Act
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);

        // Assert
        Assert.Equal(ValidId, stream.Id);
        Assert.Equal(ValidName, stream.Name);
        Assert.Equal(_validUrl, stream.Url);
        Assert.Equal(ValidCodec, stream.Codec);
        Assert.Equal(ValidBitrate, stream.BitrateKbps);
        Assert.Equal(StreamStatus.Stopped, stream.Status);
        Assert.True(stream.CreatedAt <= DateTime.UtcNow);
        Assert.Null(stream.UpdatedAt);
        Assert.Null(stream.SampleRateHz);
        Assert.Null(stream.Channels);
        Assert.Null(stream.Description);
        Assert.Null(stream.Tags);
    }

    [Fact]
    public void Create_WithCustomStatus_ShouldCreateWithSpecifiedStatus()
    {
        // Act
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate, StreamStatus.Playing);

        // Assert
        Assert.Equal(StreamStatus.Playing, stream.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => AudioStream.Create(invalidId!, ValidName, _validUrl, ValidCodec, ValidBitrate)
        );
        Assert.Contains("Stream ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => AudioStream.Create(ValidId, invalidName!, _validUrl, ValidCodec, ValidBitrate)
        );
        Assert.Contains("Stream name cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidBitrate_ShouldThrowArgumentException(int invalidBitrate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, invalidBitrate)
        );
        Assert.Contains("Bitrate must be greater than zero", exception.Message);
    }

    [Fact]
    public void WithStatus_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);
        var originalCreatedAt = stream.CreatedAt;

        // Act
        var updatedStream = stream.WithStatus(StreamStatus.Playing);

        // Assert
        Assert.Equal(StreamStatus.Playing, updatedStream.Status);
        Assert.NotNull(updatedStream.UpdatedAt);
        Assert.True(updatedStream.UpdatedAt > originalCreatedAt);
        Assert.Equal(originalCreatedAt, updatedStream.CreatedAt);
        Assert.Equal(stream.Id, updatedStream.Id);
        Assert.Equal(stream.Name, updatedStream.Name);
    }

    [Theory]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(320)]
    [InlineData(1411)]
    public void WithBitrate_WithValidBitrate_ShouldUpdateBitrate(int newBitrate)
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);

        // Act
        var updatedStream = stream.WithBitrate(newBitrate);

        // Assert
        Assert.Equal(newBitrate, updatedStream.BitrateKbps);
        Assert.NotNull(updatedStream.UpdatedAt);
        Assert.Equal(stream.Id, updatedStream.Id);
        Assert.Equal(stream.Name, updatedStream.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithBitrate_WithInvalidBitrate_ShouldThrowArgumentException(int invalidBitrate)
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => stream.WithBitrate(invalidBitrate));
        Assert.Contains("Bitrate must be greater than zero", exception.Message);
    }

    [Fact]
    public void WithUrl_ShouldUpdateUrlAndTimestamp()
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);
        var newUrl = new StreamUrl("https://newstream.example.com/audio");

        // Act
        var updatedStream = stream.WithUrl(newUrl);

        // Assert
        Assert.Equal(newUrl, updatedStream.Url);
        Assert.NotNull(updatedStream.UpdatedAt);
        Assert.Equal(stream.Id, updatedStream.Id);
        Assert.Equal(stream.Name, updatedStream.Name);
    }

    [Fact]
    public void IsPlaying_WhenStatusIsPlaying_ShouldReturnTrue()
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate, StreamStatus.Playing);

        // Act & Assert
        Assert.True(stream.IsPlaying);
        Assert.False(stream.IsStopped);
        Assert.False(stream.HasError);
    }

    [Fact]
    public void IsStopped_WhenStatusIsStopped_ShouldReturnTrue()
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate, StreamStatus.Stopped);

        // Act & Assert
        Assert.True(stream.IsStopped);
        Assert.False(stream.IsPlaying);
        Assert.False(stream.HasError);
    }

    [Fact]
    public void HasError_WhenStatusIsError_ShouldReturnTrue()
    {
        // Arrange
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate, StreamStatus.Error);

        // Act & Assert
        Assert.True(stream.HasError);
        Assert.False(stream.IsPlaying);
        Assert.False(stream.IsStopped);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(6, true)]
    [InlineData(null, false)]
    public void IsStereo_WithDifferentChannelCounts_ShouldReturnCorrectValue(int? channels, bool expectedStereo)
    {
        // Arrange
        var stream = new AudioStream
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = ValidCodec,
            BitrateKbps = ValidBitrate,
            Status = StreamStatus.Stopped,
            Channels = channels,
        };

        // Act & Assert
        Assert.Equal(expectedStereo, stream.IsStereo);
    }

    [Fact]
    public void AudioStream_WithAllProperties_ShouldInitializeCorrectly()
    {
        // Arrange
        var description = "Test Description";
        var tags = "rock,alternative";
        var sampleRate = 44100;
        var channels = 2;

        // Act
        var stream = new AudioStream
        {
            Id = ValidId,
            Name = ValidName,
            Url = _validUrl,
            Codec = AudioCodec.FLAC,
            BitrateKbps = 1411,
            Status = StreamStatus.Playing,
            SampleRateHz = sampleRate,
            Channels = channels,
            Description = description,
            Tags = tags,
        };

        // Assert
        Assert.Equal(ValidId, stream.Id);
        Assert.Equal(ValidName, stream.Name);
        Assert.Equal(_validUrl, stream.Url);
        Assert.Equal(AudioCodec.FLAC, stream.Codec);
        Assert.Equal(1411, stream.BitrateKbps);
        Assert.Equal(StreamStatus.Playing, stream.Status);
        Assert.Equal(sampleRate, stream.SampleRateHz);
        Assert.Equal(channels, stream.Channels);
        Assert.Equal(description, stream.Description);
        Assert.Equal(tags, stream.Tags);
        Assert.True(stream.IsStereo);
        Assert.True(stream.IsPlaying);
    }

    [Fact]
    public void AudioStream_Immutability_ShouldCreateNewInstancesOnUpdate()
    {
        // Arrange
        var originalStream = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);

        // Act
        var updatedStream = originalStream.WithStatus(StreamStatus.Playing);

        // Assert
        Assert.NotSame(originalStream, updatedStream);
        Assert.Equal(StreamStatus.Stopped, originalStream.Status);
        Assert.Equal(StreamStatus.Playing, updatedStream.Status);
        Assert.Null(originalStream.UpdatedAt);
        Assert.NotNull(updatedStream.UpdatedAt);
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
        var stream = AudioStream.Create(ValidId, ValidName, _validUrl, codec, ValidBitrate);

        // Assert
        Assert.Equal(codec, stream.Codec);
    }

    [Fact]
    public void AudioStream_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var stream1 = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);
        var stream2 = AudioStream.Create(ValidId, ValidName, _validUrl, ValidCodec, ValidBitrate);
        var stream3 = AudioStream.Create("different-id", ValidName, _validUrl, ValidCodec, ValidBitrate);

        // Act & Assert
        Assert.Equal(stream1.Id, stream2.Id);
        Assert.NotEqual(stream1, stream3);
        Assert.NotSame(stream1, stream2);
    }

    [Fact]
    public void AudioStream_WithComplexScenario_ShouldMaintainConsistency()
    {
        // Arrange
        var stream = AudioStream.Create(
            "classical-stream",
            "Classical Radio",
            new StreamUrl("https://classical.example.com/stream"),
            AudioCodec.FLAC,
            1411
        );

        // Act - Simulate a complex scenario with multiple updates
        var playingStream = stream
            .WithStatus(StreamStatus.Playing)
            .WithBitrate(320)
            .WithUrl(new StreamUrl("https://classical-backup.example.com/stream"));

        var stoppedStream = playingStream.WithStatus(StreamStatus.Stopped);

        // Assert
        Assert.Equal("classical-stream", stoppedStream.Id);
        Assert.Equal("Classical Radio", stoppedStream.Name);
        Assert.Equal(320, stoppedStream.BitrateKbps);
        Assert.Equal(StreamStatus.Stopped, stoppedStream.Status);
        Assert.Equal("https://classical-backup.example.com/stream", stoppedStream.Url.ToString());
        Assert.True(stoppedStream.IsStopped);
        Assert.False(stoppedStream.IsPlaying);
        Assert.NotNull(stoppedStream.UpdatedAt);
        Assert.True(stoppedStream.UpdatedAt >= stream.CreatedAt);
    }
}
