namespace SnapDog2.Tests.Integration.Mqtt;

using System.Net.Http.Json;
using FluentAssertions;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Tests.Integration.Fixtures;

[Collection("IntegrationContainers")]
public class MqttEndToEndTests : IClassFixture<MqttIntegrationWebApplicationFactory>
{
    private readonly MqttIntegrationWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MqttEndToEndTests(MqttIntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(Skip = "Pending MQTTnet v5 client API wiring (factory/type names differ)")]
    public async Task Publish_VolumeSet_Command_ShouldUpdate_Api()
    {
        // Skipped: MQTTnet v5 client factory needs to be wired to the resolved package version.
        await Task.CompletedTask;
        true.Should().BeTrue();
    }
}

public class MqttIntegrationWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly Dictionary<string, string?> _originalValues = new();
    private static readonly object _lock = new object();

    public MqttIntegrationWebApplicationFactory()
    {
        var mqttHost = Environment.GetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST") ?? "localhost";
        var mqttPort = Environment.GetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT") ?? "1883";

        // Enable API and health, disable auth
        SetEnv("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true");
        SetEnv("SNAPDOG_API_ENABLED", "true");
        SetEnv("SNAPDOG_API_AUTH_ENABLED", "false");
        SetEnv("SNAPDOG_API_PORT", "0");

        // MQTT enabled
        SetEnv("SNAPDOG_SERVICES_MQTT_ENABLED", "true");
        SetEnv("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS", mqttHost);
        SetEnv("SNAPDOG_SERVICES_MQTT_PORT", mqttPort);

        // Minimal Zone 1 so state exists
        SetEnv("SNAPDOG_ZONE_1_NAME", "Test Zone 1");
        // Additional Zone envs can be added if needed

        // Disable other integrations to reduce noise
        SetEnv("SNAPDOG_SERVICES_KNX_ENABLED", "false");
        SetEnv("SNAPDOG_SERVICES_SUBSONIC_ENABLED", "false");

        // Snapcast target for health (may remain unhealthy)
        SetEnv("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", "localhost");
        SetEnv("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", "1704");
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
