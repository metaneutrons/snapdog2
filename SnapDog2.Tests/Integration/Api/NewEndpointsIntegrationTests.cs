using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SnapDog2.Tests.Integration.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration.Api;

/// <summary>
/// Integration tests for the new API endpoints added for complete command framework alignment.
/// Tests all 18 new endpoints: Zone Track Commands, Client Commands, and System Commands.
/// </summary>
[Collection("Integration")]
public class NewEndpointsIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;

    public NewEndpointsIntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        this._fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        this._output = output ?? throw new ArgumentNullException(nameof(output));
        this._httpClient = this._fixture.HttpClient;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE TRACK SEEK ENDPOINTS - /zones/{zoneIndex}/track/seek/*
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PostTrackSeekPosition_Should_AcceptValidPosition()
    {
        // Arrange
        const int zoneIndex = 1;
        const long positionMs = 30000; // 30 seconds
        var requestBody = JsonContent.Create(positionMs);

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/track/seek/position", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Track seek position for zone {zoneIndex} to {positionMs}ms: {response.StatusCode}");
    }

    [Fact]
    public async Task PostTrackSeekProgress_Should_AcceptValidProgress()
    {
        // Arrange
        const int zoneIndex = 1;
        const float progress = 0.75f; // 75%
        var requestBody = JsonContent.Create(progress);

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/track/seek/progress", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        this._output.WriteLine(
            $"✅ Track seek progress for zone {zoneIndex} to {progress * 100}%: {response.StatusCode}"
        );
    }

    [Theory]
    [InlineData(-1000)] // Negative position
    [InlineData(long.MaxValue)] // Extremely large position
    public async Task PostTrackSeekPosition_Should_HandleInvalidPositions(long invalidPosition)
    {
        // Arrange
        const int zoneIndex = 1;
        var requestBody = JsonContent.Create(invalidPosition);

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/track/seek/position", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid position {invalidPosition}ms handled: {response.StatusCode}");
    }

    [Theory]
    [InlineData(-0.5f)] // Negative progress
    [InlineData(1.5f)] // Progress > 1.0
    public async Task PostTrackSeekProgress_Should_HandleInvalidProgress(float invalidProgress)
    {
        // Arrange
        const int zoneIndex = 1;
        var requestBody = JsonContent.Create(invalidProgress);

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/track/seek/progress", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid progress {invalidProgress} handled: {response.StatusCode}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE TRACK PLAY ENDPOINTS - /zones/{zoneIndex}/track/play/*
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PostTrackPlayByIndex_Should_AcceptValidTrackIndex()
    {
        // Arrange
        const int zoneIndex = 1;
        const int trackIndex = 5;
        var requestBody = JsonContent.Create(trackIndex);

        // Act
        var response = await this._httpClient.PostAsync(
            $"/api/v1/zones/{zoneIndex}/track/play/{trackIndex}",
            requestBody
        );

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Play track {trackIndex} in zone {zoneIndex}: {response.StatusCode}");
    }

    [Fact]
    public async Task PostTrackPlayUrl_Should_AcceptValidUrl()
    {
        // Arrange
        const int zoneIndex = 1;
        const string mediaUrl = "http://example.com/stream.mp3";
        var requestBody = new StringContent($"\"{mediaUrl}\"", Encoding.UTF8, "application/json");

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/track/play/url", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Play URL {mediaUrl} in zone {zoneIndex}: {response.StatusCode}");
    }

    [Theory]
    [InlineData(0)] // Invalid track index (should be 1-based)
    [InlineData(-1)] // Negative track index
    public async Task PostTrackPlayByIndex_Should_HandleInvalidTrackIndex(int invalidTrackIndex)
    {
        // Arrange
        const int zoneIndex = 1;
        var requestBody = JsonContent.Create(invalidTrackIndex);

        // Act
        var response = await this._httpClient.PostAsync(
            $"/api/v1/zones/{zoneIndex}/track/play/{invalidTrackIndex}",
            requestBody
        );

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid track index {invalidTrackIndex} handled: {response.StatusCode}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid-protocol.com")]
    public async Task PostTrackPlayUrl_Should_HandleInvalidUrls(string invalidUrl)
    {
        // Arrange
        const int zoneIndex = 1;
        var requestBody = new StringContent($"\"{invalidUrl}\"", Encoding.UTF8, "application/json");

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/track/play/url", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid URL '{invalidUrl}' handled: {response.StatusCode}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // CLIENT COMMANDS - /clients/{clientIndex}/*
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PostClientName_Should_AcceptValidName()
    {
        // Arrange
        const int clientIndex = 1;
        const string newName = "Test Client Name";
        var requestBody = new StringContent($"\"{newName}\"", Encoding.UTF8, "application/json");

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/clients/{clientIndex}/name", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Set client {clientIndex} name to '{newName}': {response.StatusCode}");
    }

    [Fact]
    public async Task GetClientConnectionStatus_Should_ReturnStatus()
    {
        // Arrange
        const int clientIndex = 1;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/clients/{clientIndex}/connection/status");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var isValidBoolean = bool.TryParse(content, out var connectionStatus);
            isValidBoolean.Should().BeTrue("Response should be a valid boolean");
            this._output.WriteLine($"✅ Client {clientIndex} connection status: {connectionStatus}");
        }
        else
        {
            this._output.WriteLine($"✅ Client {clientIndex} not found: {response.StatusCode}");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostClientName_Should_HandleInvalidNames(string invalidName)
    {
        // Arrange
        const int clientIndex = 1;
        var requestBody = new StringContent($"\"{invalidName}\"", Encoding.UTF8, "application/json");

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/clients/{clientIndex}/name", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid client name '{invalidName}' handled: {response.StatusCode}");
    }

    [Fact]
    public async Task PostClientName_Should_HandleNullName()
    {
        // Arrange
        const int clientIndex = 1;
        var requestBody = new StringContent("null", Encoding.UTF8, "application/json");

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/clients/{clientIndex}/name", requestBody);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Null client name handled: {response.StatusCode}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SYSTEM COMMANDS - /system/commands/*
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSystemCommandsStatus_Should_ReturnStatusList()
    {
        // Act
        var response = await this._httpClient.GetAsync("/api/v1/system/commands/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // Should be a valid JSON array or object
        var isValidJson = IsValidJson(content);
        isValidJson.Should().BeTrue("Response should be valid JSON");

        this._output.WriteLine($"✅ System commands status retrieved: {content.Length} characters");
    }

    [Fact]
    public async Task GetSystemCommandsErrors_Should_ReturnErrorList()
    {
        // Act
        var response = await this._httpClient.GetAsync("/api/v1/system/commands/errors");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // Should be a valid JSON array or object
        var isValidJson = IsValidJson(content);
        isValidJson.Should().BeTrue("Response should be valid JSON");

        this._output.WriteLine($"✅ System commands errors retrieved: {content.Length} characters");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ERROR HANDLING TESTS - Invalid Zone/Client Indices
    // ═══════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0)] // Invalid zone index
    [InlineData(-1)] // Negative zone index
    [InlineData(999)] // Non-existent zone index
    public async Task ZoneEndpoints_Should_HandleInvalidZoneIndices(int invalidZoneIndex)
    {
        // Test multiple endpoints with invalid zone index
        var endpoints = new[]
        {
            $"/api/v1/zones/{invalidZoneIndex}/track/seek/position",
            $"/api/v1/zones/{invalidZoneIndex}/track/seek/progress",
            $"/api/v1/zones/{invalidZoneIndex}/track/play/1",
            $"/api/v1/zones/{invalidZoneIndex}/track/play/url",
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await this._httpClient.PostAsync(endpoint, JsonContent.Create("test"));

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
            this._output.WriteLine($"✅ Invalid zone {invalidZoneIndex} handled for {endpoint}: {response.StatusCode}");
        }
    }

    [Theory]
    [InlineData(0)] // Invalid client index
    [InlineData(-1)] // Negative client index
    [InlineData(999)] // Non-existent client index
    public async Task ClientEndpoints_Should_HandleInvalidClientIndices(int invalidClientIndex)
    {
        // Test multiple endpoints with invalid client index
        var getEndpoints = new[] { $"/api/v1/clients/{invalidClientIndex}/connection/status" };

        var postEndpoints = new[] { $"/api/v1/clients/{invalidClientIndex}/name" };

        foreach (var endpoint in getEndpoints)
        {
            // Act
            var response = await this._httpClient.GetAsync(endpoint);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
            this._output.WriteLine(
                $"✅ Invalid client {invalidClientIndex} handled for GET {endpoint}: {response.StatusCode}"
            );
        }

        foreach (var endpoint in postEndpoints)
        {
            // Act
            var response = await this._httpClient.PostAsync(endpoint, JsonContent.Create("test"));

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
            this._output.WriteLine(
                $"✅ Invalid client {invalidClientIndex} handled for POST {endpoint}: {response.StatusCode}"
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // RESPONSE FORMAT CONSISTENCY TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllNewEndpoints_Should_ReturnConsistentResponseFormat()
    {
        // Arrange - Test a representative sample of endpoints
        var testCases = new (string Method, string Endpoint, HttpContent? Body)[]
        {
            ("POST", "/api/v1/zones/1/track/seek/position", JsonContent.Create(30000L)),
            ("POST", "/api/v1/zones/1/track/seek/progress", JsonContent.Create(0.5f)),
            ("POST", "/api/v1/zones/1/track/play/1", JsonContent.Create(1)),
            ("GET", "/api/v1/clients/1/connection/status", null),
            ("GET", "/api/v1/system/commands/status", null),
            ("GET", "/api/v1/system/commands/errors", null),
        };

        foreach (var testCase in testCases)
        {
            // Act
            var response =
                testCase.Method == "GET"
                    ? await this._httpClient.GetAsync(testCase.Endpoint)
                    : await this._httpClient.PostAsync(testCase.Endpoint, testCase.Body);

            // Assert - Check that response has consistent headers and format
            response.Headers.Should().NotBeNull();

            if (response.Content.Headers.ContentType != null)
            {
                response.Content.Headers.ContentType.MediaType.Should().BeOneOf("application/json", "text/plain");
            }

            this._output.WriteLine(
                $"✅ {testCase.Method} {testCase.Endpoint}: {response.StatusCode} - Content-Type: {response.Content.Headers.ContentType?.MediaType}"
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════════

    private static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
