namespace SnapDog2.Tests.Api;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if Status property exists
        if (healthResponse.TryGetProperty("Status", out var statusProperty))
        {
            statusProperty.GetString().Should().NotBeNullOrEmpty();
        }

        // Check if TotalDuration property exists
        if (healthResponse.TryGetProperty("TotalDuration", out var durationProperty))
        {
            durationProperty.GetDouble().Should().BeGreaterThanOrEqualTo(0);
        }

        // Check if Checks property exists
        if (healthResponse.TryGetProperty("Checks", out var checksProperty))
        {
            checksProperty.EnumerateArray().Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task GetReady_ShouldReturnReadyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var readyResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if Status property exists
        if (readyResponse.TryGetProperty("Status", out var statusProperty))
        {
            statusProperty.GetString().Should().BeOneOf("Ready", "Not Ready");
        }
    }

    [Fact]
    public async Task GetLive_ShouldReturnLiveStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var liveResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if Status property exists
        if (liveResponse.TryGetProperty("Status", out var statusProperty))
        {
            statusProperty.GetString().Should().BeOneOf("Live", "Not Live");
        }
    }
}
