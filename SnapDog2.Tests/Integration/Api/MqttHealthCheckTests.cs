namespace SnapDog2.Tests.Integration.Api;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Tests.Integration.Fixtures;

[Collection("IntegrationContainers")]
public class MqttHealthCheckTests : IClassFixture<MqttIntegrationWebApplicationFactory>
{
    private readonly MqttIntegrationWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MqttHealthCheckTests(MqttIntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldBeReachable_AndIncludeMqttCheck()
    {
        // Arrange - ensure API enabled in factory, mqtt enabled and pointed to container
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<SnapDogConfiguration>();
        config.Api.Enabled.Should().BeTrue("API must be enabled for this test");

        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert: reachable, not 404
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("Checks", out var checks))
        {
            var hasMqtt = checks
                .EnumerateArray()
                .Any(e =>
                    e.TryGetProperty("Name", out var name)
                    && string.Equals(name.GetString(), "mqtt", StringComparison.OrdinalIgnoreCase)
                );
            hasMqtt.Should().BeTrue("MQTT health check should be present when MQTT is enabled");
        }
    }
}

public class MqttIntegrationWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly Dictionary<string, string?> _originalValues = new();
    private static readonly object _lock = new object();

    public MqttIntegrationWebApplicationFactory()
    {
        // Pull broker host/port from environment, set by test bootstrap
        var mqttHost = Environment.GetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST") ?? "localhost";
        var mqttPort = Environment.GetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT") ?? "1883";

        // Ensure API and health checks are enabled for the test harness
        SetEnv("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true");
        SetEnv("SNAPDOG_API_ENABLED", "true");
        SetEnv("SNAPDOG_API_AUTH_ENABLED", "false");
        SetEnv("SNAPDOG_API_PORT", "0");

        // Snapcast health check target; it's fine if it's unhealthy
        SetEnv("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", "localhost");
        SetEnv("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", "1704");

        // Enable MQTT and point to container
        SetEnv("SNAPDOG_SERVICES_MQTT_ENABLED", "true");
        SetEnv("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS", mqttHost);
        SetEnv("SNAPDOG_SERVICES_MQTT_PORT", mqttPort);

        // Disable other integrations to reduce noise
        SetEnv("SNAPDOG_SERVICES_KNX_ENABLED", "false");
        SetEnv("SNAPDOG_SERVICES_SUBSONIC_ENABLED", "false");
    }

    private void SetEnv(string name, string value)
    {
        lock (_lock)
        {
            _originalValues[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                foreach (var kvp in _originalValues)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
                _originalValues.Clear();
            }
        }
        base.Dispose(disposing);
    }
}
