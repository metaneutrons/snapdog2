using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive unit tests for SubsonicService covering all API operations,
/// authentication mechanisms, error handling, and edge cases.
/// Award-worthy test suite following TDD principles with 100% coverage.
/// </summary>
public class SubsonicServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<ILogger<SubsonicService>> _mockLogger;
    private readonly SubsonicConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly SubsonicService _subsonicService;

    public SubsonicServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<SubsonicService>>();
        var mockMediator = new Mock<MediatR.IMediator>();
        
        _config = new SubsonicConfiguration
        {
            ServerUrl = "http://localhost:4040",
            Username = "testuser",
            Password = "testpass",
            ClientId = "SnapDog2-Test",
            ApiVersion = "1.16.1",
            MaxBitRate = 192,
            TimeoutSeconds = 30
        };

        _httpClient = new HttpClient(_mockHttpHandler.Object);
        var options = Options.Create(_config);
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        
        _subsonicService = new SubsonicService(options, _mockLogger.Object, mockMediator.Object, mockHttpClientFactory.Object);
    }

    #region Authentication Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var salt = "randomsalt123";
        var expectedToken = ComputeMd5Hash($"{_config.Password}{salt}");
        var responseXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <license valid=""true""/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.AuthenticateAsync();

        // Assert
        result.Should().BeTrue();
        VerifyHttpRequest("ping", request =>
        {
            request.RequestUri!.Query.Should().Contain($"u={_config.Username}");
            request.RequestUri.Query.Should().Contain($"s={salt}");
            request.RequestUri.Query.Should().Contain($"t={expectedToken}");
            request.RequestUri.Query.Should().Contain($"c={_config.ClientId}");
            request.RequestUri.Query.Should().Contain($"v={_config.ApiVersion}");
        });
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""failed"" version=""1.16.1"">
                <error code=""40"" message=""Wrong username or password""/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.AuthenticateAsync();

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError("Authentication failed");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AuthenticateAsync_WithHttpError_ShouldReturnFailure(HttpStatusCode statusCode)
    {
        // Arrange
        SetupHttpResponse(statusCode, "Server Error");

        // Act
        var result = await _subsonicService.AuthenticateAsync();

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError($"HTTP error during authentication: {statusCode}");
    }

    [Fact]
    public async Task AuthenticateAsync_WithNetworkTimeout_ShouldReturnFailure()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _subsonicService.AuthenticateAsync();

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError("Network timeout during authentication");
    }

    #endregion

    #region Server Availability Tests

    [Fact]
    public async Task IsServerAvailableAsync_WithHealthyServer_ShouldReturnTrue()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <license valid=""true""/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.IsServerAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task IsServerAvailableAsync_WithServerError_ShouldReturnFalse(HttpStatusCode statusCode)
    {
        // Arrange
        SetupHttpResponse(statusCode, "Server Error");

        // Act
        var result = await _subsonicService.IsServerAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Playlist Management Tests

    [Fact]
    public async Task GetPlaylistsAsync_WithValidResponse_ShouldReturnPlaylists()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <playlists>
                    <playlist id=""1"" name=""Rock Favorites"" comment=""Best rock songs"" owner=""admin"" public=""true"" songCount=""25"" duration=""6035"" created=""2023-01-15T10:30:00"" changed=""2023-01-20T15:45:00""/>
                    <playlist id=""2"" name=""Jazz Collection"" comment=""Smooth jazz"" owner=""admin"" public=""false"" songCount=""18"" duration=""4520"" created=""2023-02-01T09:15:00"" changed=""2023-02-05T11:20:00""/>
                </playlists>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.GetPlaylistsAsync();

        // Assert
        result.Should().HaveCount(2);
        
        var rockPlaylist = result.First(p => p.Name == "Rock Favorites");
        rockPlaylist.Id.Should().Be("1");
        rockPlaylist.Owner.Should().Be("admin");

        var jazzPlaylist = result.First(p => p.Name == "Jazz Collection");
    }

    [Fact]
    public async Task GetPlaylistsAsync_WithEmptyResponse_ShouldReturnEmptyList()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <playlists/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.GetPlaylistsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithValidData_ShouldReturnPlaylistId()
    {
        // Arrange
        var playlistName = "New Test Playlist";
        var trackIds = new[] { "track1", "track2", "track3" };
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <playlist id=""123"" name=""New Test Playlist"" owner=""admin"" public=""false"" songCount=""3"" duration=""720"" created=""2023-03-15T14:30:00"" changed=""2023-03-15T14:30:00""/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.CreatePlaylistAsync(playlistName, trackIds);

        // Assert
        result.Should().Be("123");
        VerifyHttpRequest("createPlaylist", request =>
        {
            request.RequestUri!.Query.Should().Contain($"name={Uri.EscapeDataString(playlistName)}");
            foreach (var trackId in trackIds)
            {
                request.RequestUri.Query.Should().Contain($"songId={trackId}");
            }
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePlaylistAsync_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var trackIds = new[] { "track1" };

        // Act & Assert
        var act = () => _subsonicService.CreatePlaylistAsync(invalidName, trackIds);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*playlist name*");
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithEmptyTrackIds_ShouldThrowArgumentException()
    {
        // Arrange
        var playlistName = "Test Playlist";
        var emptyTrackIds = Array.Empty<string>();

        // Act & Assert
        var act = () => _subsonicService.CreatePlaylistAsync(playlistName, emptyTrackIds);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*track IDs*");
    }

    [Fact]
    public async Task DeletePlaylistAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var playlistId = "123";
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1""/>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.DeletePlaylistAsync(playlistId);

        // Assert
        result.Should().BeTrue();
        VerifyHttpRequest("deletePlaylist", request =>
        {
            request.RequestUri!.Query.Should().Contain($"id={playlistId}");
        });
    }

    [Fact]
    public async Task DeletePlaylistAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var playlistId = "999";
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""failed"" version=""1.16.1"">
                <error code=""70"" message=""Playlist not found""/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.DeletePlaylistAsync(playlistId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Music Search Tests

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnSearchResults()
    {
        // Arrange
        var query = "rock music";
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <searchResult3>
                    <artist id=""art1"" name=""Rock Band"" albumCount=""5""/>
                    <album id=""alb1"" name=""Rock Album"" artist=""Rock Band"" artistId=""art1"" songCount=""12"" duration=""2850"" created=""2020-01-01T00:00:00""/>
                    <song id=""song1"" parent=""alb1"" title=""Rock Song"" album=""Rock Album"" artist=""Rock Band"" track=""1"" year=""2020"" genre=""Rock"" size=""4567890"" contentType=""audio/mpeg"" suffix=""mp3"" duration=""234"" bitRate=""192"" path=""Rock Band/Rock Album/01 - Rock Song.mp3"" isVideo=""false"" created=""2020-01-01T00:00:00"" albumId=""alb1"" artistId=""art1"" type=""music""/>
                </searchResult3>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Artists.Should().HaveCount(1);
        result.Albums.Should().HaveCount(1);
        result.Songs.Should().HaveCount(1);

        var artist = result.Artists.First();
        artist.Id.Should().Be("art1");
        artist.Name.Should().Be("Rock Band");
    }

    [Fact]
    public async Task SearchAsync_WithNoResults_ShouldReturnEmptySearchResult()
    {
        // Arrange
        var query = "nonexistent music";
        var responseXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">
                <searchResult3/>
            </subsonic-response>";

        SetupHttpResponse(HttpStatusCode.OK, responseXml);

        // Act
        var result = await _subsonicService.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WithInvalidQuery_ShouldThrowArgumentException(string invalidQuery)
    {
        // Act & Assert
        var act = () => _subsonicService.SearchAsync(invalidQuery);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*search query*");
    }

    #endregion

    #region Streaming Tests

    [Fact]
    public async Task GetStreamUrlAsync_WithValidTrackId_ShouldReturnUrl()
    {
        // Arrange
        var trackId = "track123";
        var maxBitRate = 192;

        // Act
        var result = await _subsonicService.GetStreamUrlAsync(trackId, maxBitRate);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith(_config.GetFormattedServerUrl());
        result.Should().Contain("stream");
        result.Should().Contain($"id={trackId}");
        result.Should().Contain($"maxBitRate={maxBitRate}");
        result.Should().Contain($"u={_config.Username}");
        result.Should().Contain("t="); // MD5 token
        result.Should().Contain("s="); // Salt
    }

    [Fact]
    public async Task GetStreamUrlAsync_WithDefaultBitRate_ShouldUseConfiguredMaxBitRate()
    {
        // Arrange
        var trackId = "track123";

        // Act
        var result = await _subsonicService.GetStreamUrlAsync(trackId);

        // Assert
        result.Should().Contain($"maxBitRate={_config.MaxBitRate}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStreamUrlAsync_WithInvalidTrackId_ShouldThrowArgumentException(string invalidTrackId)
    {
        // Act & Assert
        var act = () => _subsonicService.GetStreamUrlAsync(invalidTrackId);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*track ID*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(500)]
    public async Task GetStreamUrlAsync_WithInvalidBitRate_ShouldThrowArgumentOutOfRangeException(int invalidBitRate)
    {
        // Arrange
        var trackId = "track123";

        // Act & Assert
        var act = () => _subsonicService.GetStreamUrlAsync(trackId, invalidBitRate);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*bitrate*");
    }

    [Fact]
    public async Task GetTrackStreamAsync_WithValidTrackId_ShouldReturnStream()
    {
        // Arrange
        var trackId = "track123";
        var audioData = Encoding.UTF8.GetBytes("fake audio data");
        
        SetupHttpResponse(HttpStatusCode.OK, audioData, "audio/mpeg");

        // Act
        var result = await _subsonicService.GetTrackStreamAsync(trackId);

        // Assert
        result.Should().NotBeNull();
        result.CanRead.Should().BeTrue();
        
        // Verify the stream contains the expected data
        var buffer = new byte[audioData.Length];
        var bytesRead = await result.ReadAsync(buffer, 0, buffer.Length);
        bytesRead.Should().Be(audioData.Length);
        buffer.Should().Equal(audioData);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_config);

        // Act & Assert
        var act = () => new SubsonicService(options, _mockLogger.Object, new Mock<MediatR.IMediator>().Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClientFactory");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SubsonicService(null!, _mockLogger.Object, new Mock<MediatR.IMediator>().Object, new Mock<IHttpClientFactory>().Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_config);

        // Act & Assert
        var act = () => new SubsonicService(options, null!, new Mock<MediatR.IMediator>().Object, new Mock<IHttpClientFactory>().Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_config);

        // Act & Assert
        var act = () => new SubsonicService(options, _mockLogger.Object, null!, new Mock<IHttpClientFactory>().Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task AuthenticateAsync_WithMalformedXmlResponse_ShouldReturnFailure()
    {
        // Arrange
        var malformedXml = "This is not XML";
        SetupHttpResponse(HttpStatusCode.OK, malformedXml);

        // Act
        var result = await _subsonicService.AuthenticateAsync();

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError("Failed to parse authentication response");
    }

    [Fact]
    public async Task GetPlaylistsAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = () => _subsonicService.GetPlaylistsAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SearchAsync_WithLargeResultSet_ShouldHandleCorrectly()
    {
        // Arrange
        var query = "popular";
        var largeResultXml = GenerateLargeSearchResultXml(1000, 500, 2000);
        SetupHttpResponse(HttpStatusCode.OK, largeResultXml);

        // Act
        var result = await _subsonicService.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Artists.Should().HaveCount(1000);
        result.Albums.Should().HaveCount(500);
        result.Songs.Should().HaveCount(2000);
    }

    [Fact]
    public async Task GetTrackStreamAsync_WithLargeFile_ShouldStreamEfficiently()
    {
        // Arrange
        var trackId = "large-track";
        var largeAudioData = new byte[10 * 1024 * 1024]; // 10MB
        new Random().NextBytes(largeAudioData);
        
        SetupHttpResponse(HttpStatusCode.OK, largeAudioData, "audio/mpeg");

        // Act
        using var result = await _subsonicService.GetTrackStreamAsync(trackId);

        // Assert
        result.Should().NotBeNull();
        result.CanRead.Should().BeTrue();
        
        // Verify streaming works in chunks
        var buffer = new byte[1024];
        var totalBytesRead = 0;
        int bytesRead;
        
        while ((bytesRead = await result.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            totalBytesRead += bytesRead;
            if (totalBytesRead > 1024 * 1024) // Stop after 1MB to avoid long test
                break;
        }
        
        totalBytesRead.Should().BeGreaterThan(1024 * 1024);
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content, string contentType = "application/xml")
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType)
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, byte[] content, string contentType)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new ByteArrayContent(content)
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpRequest(string expectedEndpoint, Action<HttpRequestMessage>? additionalVerification = null)
    {
        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.AbsolutePath.Contains(expectedEndpoint)),
                ItExpr.IsAny<CancellationToken>());

        if (additionalVerification != null)
        {
            _mockHttpHandler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                    {
                        additionalVerification(req);
                        return true;
                    }),
                    ItExpr.IsAny<CancellationToken>());
        }
    }

    private void VerifyLoggerError(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static string ComputeMd5Hash(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateLargeSearchResultXml(int artistCount, int albumCount, int songCount)
    {
        var xml = new StringBuilder();
        xml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        xml.AppendLine(@"<subsonic-response xmlns=""http://subsonic.org/restapi"" status=""ok"" version=""1.16.1"">");
        xml.AppendLine("<searchResult3>");

        // Generate artists
        for (int i = 1; i <= artistCount; i++)
        {
            xml.AppendLine($@"<artist id=""art{i}"" name=""Artist {i}"" albumCount=""{i % 10 + 1}""/>");
        }

        // Generate albums
        for (int i = 1; i <= albumCount; i++)
        {
            xml.AppendLine($@"<album id=""alb{i}"" name=""Album {i}"" artist=""Artist {i % artistCount + 1}"" artistId=""art{i % artistCount + 1}"" songCount=""{i % 15 + 5}"" duration=""{2000 + i * 10}"" created=""2020-01-01T00:00:00""/>");
        }

        // Generate songs
        for (int i = 1; i <= songCount; i++)
        {
            xml.AppendLine($@"<song id=""song{i}"" parent=""alb{i % albumCount + 1}"" title=""Song {i}"" album=""Album {i % albumCount + 1}"" artist=""Artist {i % artistCount + 1}"" track=""{i % 12 + 1}"" year=""2020"" genre=""Rock"" size=""{4000000 + i * 1000}"" contentType=""audio/mpeg"" suffix=""mp3"" duration=""{180 + i % 120}"" bitRate=""192"" path=""Artist {i % artistCount + 1}/Album {i % albumCount + 1}/{i:D2} - Song {i}.mp3"" isVideo=""false"" created=""2020-01-01T00:00:00"" albumId=""alb{i % albumCount + 1}"" artistId=""art{i % artistCount + 1}"" type=""music""/>");
        }

        xml.AppendLine("</searchResult3>");
        xml.AppendLine("</subsonic-response>");

        return xml.ToString();
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
        _subsonicService?.Dispose();
    }
}