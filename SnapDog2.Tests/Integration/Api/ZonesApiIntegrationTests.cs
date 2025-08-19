using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Containers;
using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration.Api;

/// <summary>
/// Integration tests for the Zones API endpoints.
/// Tests the new simplified API structure with repeat and shuffle endpoints.
/// </summary>
[Collection("Integration")]
public class ZonesApiIntegrationTests : IClassFixture<DockerComposeTestFixture>
{
    private readonly DockerComposeTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;

    public ZonesApiIntegrationTests(DockerComposeTestFixture fixture, ITestOutputHelper output)
    {
        this._fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        this._output = output ?? throw new ArgumentNullException(nameof(output));
        this._httpClient = this._fixture.HttpClient;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // TRACK REPEAT ENDPOINTS - /zones/{zoneIndex}/repeat/track
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTrackRepeat_Should_ReturnBooleanValue()
    {
        // Arrange
        const int zoneIndex = 1;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/zones/{zoneIndex}/repeat/track");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var isValidBoolean = bool.TryParse(content, out var repeatState);

        isValidBoolean.Should().BeTrue("Response should be a valid boolean");
        this._output.WriteLine($"✅ Track repeat state for zone {zoneIndex}: {repeatState}");
    }

    [Fact]
    public async Task SetTrackRepeat_Should_ReturnNewState()
    {
        // Arrange
        const int zoneIndex = 1;
        const bool newState = true;

        // Act
        var response = await this._httpClient.PutAsJsonAsync($"/api/v1/zones/{zoneIndex}/repeat/track", newState);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedState = await response.Content.ReadFromJsonAsync<bool>();
        returnedState.Should().Be(newState);

        this._output.WriteLine($"✅ Set track repeat for zone {zoneIndex} to {newState}, returned: {returnedState}");
    }

    [Fact(Timeout = 30000)] // 30 second timeout to prevent hanging
    public async Task ToggleTrackRepeat_Should_ReturnNewState()
    {
        this._output.WriteLine("🔍 Starting ToggleTrackRepeat test...");

        // Arrange
        const int zoneIndex = 1;

        this._output.WriteLine($"🔍 Getting current track repeat state for zone {zoneIndex}...");
        // Get current state
        var getCurrentResponse = await this._httpClient.GetAsync($"/api/v1/zones/{zoneIndex}/repeat/track");
        getCurrentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentState = await getCurrentResponse.Content.ReadFromJsonAsync<bool>();
        this._output.WriteLine($"🔍 Current track repeat state: {currentState}");

        this._output.WriteLine($"🔍 Toggling track repeat for zone {zoneIndex}...");
        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/repeat/track/toggle", null);

        this._output.WriteLine($"🔍 Toggle response status: {response.StatusCode}");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newState = await response.Content.ReadFromJsonAsync<bool>();
        newState.Should().Be(!currentState, "Toggle should flip the current state");

        this._output.WriteLine($"✅ Toggled track repeat for zone {zoneIndex}: {currentState} → {newState}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYLIST REPEAT ENDPOINTS - /zones/{zoneIndex}/repeat
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPlaylistRepeat_Should_ReturnBooleanValue()
    {
        // Arrange
        const int zoneIndex = 1;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/zones/{zoneIndex}/repeat");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var isValidBoolean = bool.TryParse(content, out var repeatState);

        isValidBoolean.Should().BeTrue("Response should be a valid boolean");
        this._output.WriteLine($"✅ Playlist repeat state for zone {zoneIndex}: {repeatState}");
    }

    [Fact]
    public async Task SetPlaylistRepeat_Should_ReturnNewState()
    {
        // Arrange
        const int zoneIndex = 1;
        const bool newState = true;

        // Act
        var response = await this._httpClient.PutAsJsonAsync($"/api/v1/zones/{zoneIndex}/repeat", newState);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedState = await response.Content.ReadFromJsonAsync<bool>();
        returnedState.Should().Be(newState);

        this._output.WriteLine($"✅ Set playlist repeat for zone {zoneIndex} to {newState}, returned: {returnedState}");
    }

    [Fact]
    public async Task TogglePlaylistRepeat_Should_ReturnNewState()
    {
        // Arrange
        const int zoneIndex = 1;

        // Get current state
        var getCurrentResponse = await this._httpClient.GetAsync($"/api/v1/zones/{zoneIndex}/repeat");
        getCurrentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentState = await getCurrentResponse.Content.ReadFromJsonAsync<bool>();

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/repeat/toggle", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newState = await response.Content.ReadFromJsonAsync<bool>();
        newState.Should().Be(!currentState, "Toggle should flip the current state");

        this._output.WriteLine($"✅ Toggled playlist repeat for zone {zoneIndex}: {currentState} → {newState}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PLAYLIST SHUFFLE ENDPOINTS - /zones/{zoneIndex}/shuffle
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPlaylistShuffle_Should_ReturnBooleanValue()
    {
        // Arrange
        const int zoneIndex = 1;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/zones/{zoneIndex}/shuffle");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var isValidBoolean = bool.TryParse(content, out var shuffleState);

        isValidBoolean.Should().BeTrue("Response should be a valid boolean");
        this._output.WriteLine($"✅ Playlist shuffle state for zone {zoneIndex}: {shuffleState}");
    }

    [Fact]
    public async Task SetPlaylistShuffle_Should_ReturnNewState()
    {
        // Arrange
        const int zoneIndex = 1;
        const bool newState = true;

        // Act
        var response = await this._httpClient.PutAsJsonAsync($"/api/v1/zones/{zoneIndex}/shuffle", newState);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedState = await response.Content.ReadFromJsonAsync<bool>();
        returnedState.Should().Be(newState);

        this._output.WriteLine($"✅ Set playlist shuffle for zone {zoneIndex} to {newState}, returned: {returnedState}");
    }

    [Fact]
    public async Task TogglePlaylistShuffle_Should_ReturnNewState()
    {
        // Arrange
        const int zoneIndex = 1;

        // Get current state
        var getCurrentResponse = await this._httpClient.GetAsync($"/api/v1/zones/{zoneIndex}/shuffle");
        getCurrentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentState = await getCurrentResponse.Content.ReadFromJsonAsync<bool>();

        // Act
        var response = await this._httpClient.PostAsync($"/api/v1/zones/{zoneIndex}/shuffle/toggle", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newState = await response.Content.ReadFromJsonAsync<bool>();
        newState.Should().Be(!currentState, "Toggle should flip the current state");

        this._output.WriteLine($"✅ Toggled playlist shuffle for zone {zoneIndex}: {currentState} → {newState}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ERROR HANDLING TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTrackRepeat_WithInvalidZone_Should_ReturnNotFound()
    {
        // Arrange
        const int invalidZoneIndex = 999;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/zones/{invalidZoneIndex}/repeat/track");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid zone {invalidZoneIndex} correctly returned 404 Not Found");
    }

    [Fact]
    public async Task GetPlaylistRepeat_WithInvalidZone_Should_ReturnNotFound()
    {
        // Arrange
        const int invalidZoneIndex = 999;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/zones/{invalidZoneIndex}/repeat");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid zone {invalidZoneIndex} correctly returned 404 Not Found");
    }

    [Fact]
    public async Task GetPlaylistShuffle_WithInvalidZone_Should_ReturnNotFound()
    {
        // Arrange
        const int invalidZoneIndex = 999;

        // Act
        var response = await this._httpClient.GetAsync($"/api/v1/zones/{invalidZoneIndex}/shuffle");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        this._output.WriteLine($"✅ Invalid zone {invalidZoneIndex} correctly returned 404 Not Found");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // API CONSISTENCY TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllRepeatAndShuffleEndpoints_Should_ReturnConsistentResponseFormat()
    {
        // Arrange
        const int zoneIndex = 1;
        var endpoints = new[]
        {
            $"/api/v1/zones/{zoneIndex}/repeat/track",
            $"/api/v1/zones/{zoneIndex}/repeat",
            $"/api/v1/zones/{zoneIndex}/shuffle",
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await this._httpClient.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Endpoint {endpoint} should return OK");

            var content = await response.Content.ReadAsStringAsync();
            var isValidBoolean = bool.TryParse(content, out var state);
            isValidBoolean.Should().BeTrue($"Endpoint {endpoint} should return a valid boolean");

            response
                .Content.Headers.ContentType?.MediaType.Should()
                .Be("application/json", $"Endpoint {endpoint} should return JSON content type");

            this._output.WriteLine($"✅ {endpoint} → {state} (valid boolean response)");
        }
    }
}
