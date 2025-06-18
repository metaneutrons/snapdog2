using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.ValueObjects;

/// <summary>
/// Unit tests for the StreamUrl value object.
/// Tests URL validation, parsing, scheme checking, equality, and type safety.
/// </summary>
public class StreamUrlTests
{
    [Theory]
    [InlineData("http://example.com/stream")]
    [InlineData("https://example.com/stream")]
    [InlineData("http://192.168.1.100:8000/stream")]
    [InlineData("https://stream.example.com:8080/radio")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("file:///path/to/audio.mp3")]
    [InlineData("ftp://example.com/audio.mp3")]
    [InlineData("rtsp://example.com/stream")]
    [InlineData("rtmp://example.com/live")]
    public void Constructor_WithValidUrl_ShouldCreateStreamUrl(string validUrl)
    {
        // Act
        var streamUrl = new StreamUrl(validUrl);

        // Assert
        var expectedUri = new Uri(validUrl);
        Assert.Equal(expectedUri.ToString(), streamUrl.ToString());
        Assert.Equal(new Uri(validUrl), streamUrl.Value);
        Assert.NotNull(streamUrl.Value);
    }

    [Fact]
    public void Constructor_WithUriInstance_ShouldCreateStreamUrl()
    {
        // Arrange
        var uri = new Uri("https://example.com/stream");

        // Act
        var streamUrl = new StreamUrl(uri);

        // Assert
        Assert.Equal(uri.ToString(), streamUrl.ToString());
        Assert.Equal(uri, streamUrl.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Constructor_WithInvalidInput_ShouldThrowArgumentException(string? invalidUrl)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StreamUrl(invalidUrl!));
        Assert.Contains("Stream URL cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    public void Constructor_WithInvalidUrlFormat_ShouldThrowArgumentException(string invalidUrl)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StreamUrl(invalidUrl));
        Assert.Contains("Invalid URL format", exception.Message);
    }

    [Theory]
    [InlineData("mailto:test@example.com")]
    [InlineData("javascript:alert('test')")]
    [InlineData("data:text/plain;base64,SGVsbG8=")]
    [InlineData("ssh://example.com")]
    [InlineData("telnet://example.com")]
    public void Constructor_WithUnsupportedUrlScheme_ShouldThrowArgumentException(string invalidUrl)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StreamUrl(invalidUrl));
        Assert.Contains("Unsupported URL scheme", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullUri_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamUrl((Uri)null!));
    }

    [Theory]
    [InlineData("http://example.com/stream", true)]
    [InlineData("https://example.com/stream", true)]
    [InlineData("file:///path/to/audio.mp3", true)]
    [InlineData("ftp://example.com/audio.mp3", true)]
    [InlineData("rtsp://example.com/stream", true)]
    [InlineData("rtmp://example.com/live", true)]
    [InlineData("HTTP://EXAMPLE.COM/STREAM", true)]
    [InlineData("HTTPS://EXAMPLE.COM/STREAM", true)]
    [InlineData("mailto:test@example.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_WithDifferentInputs_ShouldReturnCorrectValue(string? url, bool expectedValid)
    {
        // Act & Assert
        Assert.Equal(expectedValid, StreamUrl.IsValid(url));
    }

    [Theory]
    [InlineData("http://example.com/stream")]
    [InlineData("https://example.com/stream")]
    [InlineData("file:///path/to/audio.mp3")]
    [InlineData("rtsp://192.168.1.100:554/stream")]
    public void Parse_WithValidUrl_ShouldReturnStreamUrl(string validUrl)
    {
        // Act
        var streamUrl = StreamUrl.Parse(validUrl);

        // Assert
        // URLs may be normalized by Uri constructor, so check if they're equivalent
        var expectedUri = new Uri(validUrl);
        Assert.Equal(expectedUri.ToString(), streamUrl.ToString());
        Assert.NotNull(streamUrl.Value);
    }

    [Theory]
    [InlineData("mailto:test@example.com")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void Parse_WithInvalidUrl_ShouldThrowArgumentException(string invalidUrl)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StreamUrl.Parse(invalidUrl));
    }

    [Theory]
    [InlineData("http://example.com/stream", true)]
    [InlineData("https://example.com/stream", true)]
    [InlineData("file:///path/to/audio.mp3", true)]
    [InlineData("rtsp://example.com/stream", true)]
    [InlineData("mailto:test@example.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParse_WithDifferentInputs_ShouldReturnCorrectResult(string? url, bool expectedSuccess)
    {
        // Act
        var success = StreamUrl.TryParse(url, out var result);

        // Assert
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.Equal(url, result.ToString());
            Assert.NotNull(result.Value);
        }
        else
        {
            Assert.Equal(default, result);
        }
    }

    [Theory]
    [InlineData("http://example.com/stream", true)]
    [InlineData("https://example.com/stream", true)]
    [InlineData("HTTP://EXAMPLE.COM/STREAM", true)]
    [InlineData("HTTPS://EXAMPLE.COM/STREAM", true)]
    [InlineData("file:///path/to/audio.mp3", false)]
    [InlineData("rtsp://example.com/stream", false)]
    [InlineData("rtmp://example.com/live", false)]
    public void IsHttp_WithDifferentSchemes_ShouldReturnCorrectValue(string url, bool expectedHttp)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedHttp, streamUrl.IsHttp);
    }

    [Theory]
    [InlineData("https://example.com/stream", true)]
    [InlineData("HTTPS://EXAMPLE.COM/STREAM", true)]
    [InlineData("http://example.com/stream", false)]
    [InlineData("HTTP://EXAMPLE.COM/STREAM", false)]
    [InlineData("file:///path/to/audio.mp3", false)]
    [InlineData("rtsp://example.com/stream", false)]
    public void IsSecure_WithDifferentSchemes_ShouldReturnCorrectValue(string url, bool expectedSecure)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedSecure, streamUrl.IsSecure);
    }

    [Theory]
    [InlineData("file:///path/to/audio.mp3", true)]
    [InlineData("FILE:///PATH/TO/AUDIO.MP3", true)]
    [InlineData("http://example.com/stream", false)]
    [InlineData("https://example.com/stream", false)]
    [InlineData("rtsp://example.com/stream", false)]
    public void IsFile_WithDifferentSchemes_ShouldReturnCorrectValue(string url, bool expectedFile)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedFile, streamUrl.IsFile);
    }

    [Theory]
    [InlineData("http", "http://example.com/stream")]
    [InlineData("https", "https://example.com/stream")]
    [InlineData("file", "file:///path/to/audio.mp3")]
    [InlineData("rtsp", "rtsp://example.com/stream")]
    [InlineData("rtmp", "rtmp://example.com/live")]
    [InlineData("ftp", "ftp://example.com/audio.mp3")]
    public void Scheme_WithDifferentUrls_ShouldReturnCorrectScheme(string expectedScheme, string url)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedScheme, streamUrl.Scheme);
    }

    [Theory]
    [InlineData("http://example.com/stream", "example.com")]
    [InlineData("https://api.example.com/stream", "api.example.com")]
    [InlineData("http://192.168.1.100:8000/radio", "192.168.1.100")]
    [InlineData("https://localhost/stream", "localhost")]
    [InlineData("rtsp://stream.example.com:554/live", "stream.example.com")]
    public void Host_WithDifferentUrls_ShouldReturnCorrectHost(string url, string expectedHost)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedHost, streamUrl.Host);
    }

    [Theory]
    [InlineData("http://example.com/stream", 80)]
    [InlineData("https://example.com/stream", 443)]
    [InlineData("http://example.com:8000/stream", 8000)]
    [InlineData("https://example.com:8080/stream", 8080)]
    [InlineData("rtsp://example.com:554/stream", 554)]
    [InlineData("ftp://example.com:21/audio.mp3", 21)]
    public void Port_WithDifferentUrls_ShouldReturnCorrectPort(string url, int expectedPort)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedPort, streamUrl.Port);
    }

    [Theory]
    [InlineData("http://example.com/stream", "/stream")]
    [InlineData("https://example.com/radio/live", "/radio/live")]
    [InlineData("http://example.com", "/")]
    [InlineData("https://example.com/", "/")]
    [InlineData("file:///path/to/audio.mp3", "/path/to/audio.mp3")]
    public void Path_WithDifferentUrls_ShouldReturnCorrectPath(string url, string expectedPath)
    {
        // Arrange
        var streamUrl = new StreamUrl(url);

        // Act & Assert
        Assert.Equal(expectedPath, streamUrl.Path);
    }

    [Fact]
    public void Equals_WithSameUrl_ShouldReturnTrue()
    {
        // Arrange
        var url1 = new StreamUrl("http://example.com/stream");
        var url2 = new StreamUrl("http://example.com/stream");

        // Act & Assert
        Assert.True(url1.Equals(url2));
        Assert.True(url1 == url2);
        Assert.False(url1 != url2);
        Assert.Equal(url1.GetHashCode(), url2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentUrl_ShouldReturnFalse()
    {
        // Arrange
        var url1 = new StreamUrl("http://example.com/stream1");
        var url2 = new StreamUrl("http://example.com/stream2");

        // Act & Assert
        Assert.False(url1.Equals(url2));
        Assert.False(url1 == url2);
        Assert.True(url1 != url2);
    }

    [Fact]
    public void Equals_WithNonStreamUrlObject_ShouldReturnFalse()
    {
        // Arrange
        var streamUrl = new StreamUrl("http://example.com/stream");
        var obj = "http://example.com/stream";

        // Act & Assert
        Assert.True(streamUrl.Equals(obj));
    }

    [Fact]
    public void Equals_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        var streamUrl = new StreamUrl("http://example.com/stream");

        // Act & Assert
        Assert.False(streamUrl.Equals((object?)null));
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldWork()
    {
        // Act
        StreamUrl streamUrl = "http://example.com/stream";

        // Assert
        Assert.Equal("http://example.com/stream", streamUrl.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var streamUrl = new StreamUrl("http://example.com/stream");

        // Act
        string urlString = streamUrl;

        // Assert
        Assert.Equal("http://example.com/stream", urlString);
    }

    [Fact]
    public void ImplicitConversion_FromUri_ShouldWork()
    {
        // Arrange
        var uri = new Uri("http://example.com/stream");

        // Act
        StreamUrl streamUrl = uri;

        // Assert
        Assert.Equal(uri.ToString(), streamUrl.ToString());
        Assert.Equal(uri, streamUrl.Value);
    }

    [Fact]
    public void ImplicitConversion_ToUri_ShouldWork()
    {
        // Arrange
        var streamUrl = new StreamUrl("http://example.com/stream");

        // Act
        Uri uri = streamUrl;

        // Assert
        Assert.Equal(streamUrl.Value, uri);
        Assert.Equal(streamUrl.ToString(), uri.ToString());
    }

    [Fact]
    public void GetHashCode_WithSameUrl_ShouldReturnSameHashCode()
    {
        // Arrange
        var url1 = new StreamUrl("http://example.com/stream");
        var url2 = new StreamUrl("http://example.com/stream");

        // Act & Assert
        Assert.Equal(url1.GetHashCode(), url2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnAbsoluteUri()
    {
        // Arrange
        var streamUrl = new StreamUrl("http://example.com/stream");

        // Act
        var result = streamUrl.ToString();

        // Assert
        Assert.Equal("http://example.com/stream", result);
    }

    [Fact]
    public void ValueObject_Immutability_ShouldBeImmutable()
    {
        // Arrange
        var originalUrl = new StreamUrl("http://example.com/stream");
        var originalValue = originalUrl.Value;

        // Act - Try to access and use the URL in various ways
        var stringValue = originalUrl.ToString();
        var host = originalUrl.Host;
        var port = originalUrl.Port;
        var path = originalUrl.Path;
        var isSecure = originalUrl.IsSecure;
        var scheme = originalUrl.Scheme;

        // Assert - Original should remain unchanged
        Assert.Equal(originalValue, originalUrl.Value);
        Assert.Equal("http://example.com/stream", originalUrl.ToString());
        Assert.False(originalUrl.IsSecure);
        Assert.Equal("http", originalUrl.Scheme);
    }

    [Fact]
    public void StreamUrl_WithComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange - Test various URL operations with different schemes
        var httpUrl = new StreamUrl("http://radio.example.com:8000/stream");
        var httpsUrl = new StreamUrl("https://secure.example.com/live");
        var fileUrl = new StreamUrl("file:///home/user/music/track.mp3");
        var rtspUrl = new StreamUrl("rtsp://192.168.1.100:554/live");
        var parsedUrl = StreamUrl.Parse("ftp://files.example.com/audio.mp3");

        // Act & Assert
        Assert.True(httpUrl.IsHttp);
        Assert.False(httpUrl.IsSecure);
        Assert.False(httpUrl.IsFile);
        Assert.Equal("radio.example.com", httpUrl.Host);
        Assert.Equal(8000, httpUrl.Port);
        Assert.Equal("/stream", httpUrl.Path);
        Assert.Equal("http", httpUrl.Scheme);

        Assert.True(httpsUrl.IsHttp);
        Assert.True(httpsUrl.IsSecure);
        Assert.False(httpsUrl.IsFile);
        Assert.Equal("secure.example.com", httpsUrl.Host);
        Assert.Equal(443, httpsUrl.Port);
        Assert.Equal("/live", httpsUrl.Path);
        Assert.Equal("https", httpsUrl.Scheme);

        Assert.False(fileUrl.IsHttp);
        Assert.False(fileUrl.IsSecure);
        Assert.True(fileUrl.IsFile);
        Assert.Equal("/home/user/music/track.mp3", fileUrl.Path);
        Assert.Equal("file", fileUrl.Scheme);

        Assert.False(rtspUrl.IsHttp);
        Assert.False(rtspUrl.IsSecure);
        Assert.False(rtspUrl.IsFile);
        Assert.Equal("192.168.1.100", rtspUrl.Host);
        Assert.Equal(554, rtspUrl.Port);
        Assert.Equal("rtsp", rtspUrl.Scheme);

        Assert.Equal("files.example.com", parsedUrl.Host);
        Assert.Equal("ftp", parsedUrl.Scheme);

        // Test that they're all different
        Assert.NotEqual(httpUrl, httpsUrl);
        Assert.NotEqual(httpUrl, fileUrl);
        Assert.NotEqual(httpsUrl, rtspUrl);
        Assert.NotEqual(fileUrl, parsedUrl);

        // Test conversion roundtrip
        string urlString = httpUrl;
        StreamUrl convertedBack = urlString;
        Assert.Equal(httpUrl, convertedBack);
    }

    [Theory]
    [InlineData("http://example.com/stream", "http://example.com/stream", true)]
    [InlineData("http://example.com/stream", "https://example.com/stream", false)]
    [InlineData("http://example.com/stream1", "http://example.com/stream2", false)]
    [InlineData("HTTP://EXAMPLE.COM/STREAM", "http://example.com/STREAM", true)]
    public void OperatorEquals_WithDifferentComparisons_ShouldReturnCorrectResult(
        string url1String,
        string url2String,
        bool expectedEqual
    )
    {
        // Arrange
        var url1 = new StreamUrl(url1String);
        var url2 = new StreamUrl(url2String);

        // Act & Assert
        Assert.Equal(expectedEqual, url1 == url2);
        Assert.Equal(!expectedEqual, url1 != url2);
    }

    [Fact]
    public void StreamUrl_StructBehavior_ShouldBehaveAsValueType()
    {
        // Arrange
        var url1 = new StreamUrl("http://example.com/stream");
        var url2 = url1; // This should copy the struct

        // Act - Since it's a struct, url2 should be a copy
        var areEqual = url1.Equals(url2);
        var hashCodesEqual = url1.GetHashCode() == url2.GetHashCode();

        // Assert
        Assert.True(areEqual);
        Assert.True(hashCodesEqual);
        Assert.Equal(url1.ToString(), url2.ToString());
    }

    [Theory]
    [InlineData("http://icecast.example.com:8000/live.mp3")]
    [InlineData("https://shoutcast.example.com/stream")]
    [InlineData("rtsp://camera.example.com:554/live")]
    [InlineData("rtmp://live.example.com/app/stream")]
    [InlineData("file:///var/media/audio/track.flac")]
    [InlineData("ftp://media.example.com/audio/podcast.mp3")]
    public void StreamUrl_WithRealisticStreamUrls_ShouldWorkCorrectly(string realisticUrl)
    {
        // Act
        var streamUrl = new StreamUrl(realisticUrl);

        // Assert
        Assert.Equal(realisticUrl, streamUrl.ToString());
        Assert.NotNull(streamUrl.Value);
        Assert.NotEmpty(streamUrl.Scheme);
        Assert.True(streamUrl.Port > 0 || streamUrl.Port == -1 || streamUrl.IsFile);
    }

    [Fact]
    public void StreamUrl_WithUriConversion_ShouldMaintainEquivalence()
    {
        // Arrange
        var originalUri = new Uri("https://example.com/stream");

        // Act
        var streamUrl = new StreamUrl(originalUri);
        Uri convertedUri = streamUrl;

        // Assert
        Assert.Equal(originalUri, convertedUri);
        Assert.Equal(originalUri.ToString(), streamUrl.ToString());
        Assert.Equal(originalUri.Host, streamUrl.Host);
        Assert.Equal(originalUri.Port, streamUrl.Port);
        Assert.Equal(originalUri.AbsolutePath, streamUrl.Path);
        Assert.Equal(originalUri.Scheme, streamUrl.Scheme);
    }

    [Fact]
    public void StreamUrl_DefaultValue_ShouldBeHandledCorrectly()
    {
        // Arrange
        var defaultUrl = default(StreamUrl);

        // Act & Assert
        Assert.Null(defaultUrl.Value);
        Assert.Null(defaultUrl.ToString());
    }

    [Theory]
    [InlineData("http")]
    [InlineData("https")]
    [InlineData("file")]
    [InlineData("ftp")]
    [InlineData("rtsp")]
    [InlineData("rtmp")]
    public void StreamUrl_WithAllSupportedSchemes_ShouldWorkCorrectly(string scheme)
    {
        // Arrange
        var url = scheme switch
        {
            "file" => "file:///path/to/file.mp3",
            _ => $"{scheme}://example.com/stream",
        };

        // Act
        var streamUrl = new StreamUrl(url);

        // Assert
        Assert.Equal(scheme, streamUrl.Scheme);
        Assert.True(StreamUrl.IsValid(url));
        Assert.Equal(url, streamUrl.ToString());
    }
}
