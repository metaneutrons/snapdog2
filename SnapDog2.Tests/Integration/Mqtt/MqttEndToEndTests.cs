namespace SnapDog2.Tests.Integration.Mqtt;

using System.Net.Http.Json;
using FluentAssertions;
using MQTTnet;
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
        // Arrange
        var mqttHost = Environment.GetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST") ?? "localhost";
        var mqttPort = int.Parse(Environment.GetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT") ?? "1883");

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder().WithTcpServer(mqttHost, mqttPort).Build();
        await client.ConnectAsync(options, CancellationToken.None);

        // Topic per MqttCommandMapper: snapdog/zone/{id}/control/set
        var topic = "snapdog/zone/1/control/set";
        var payload = "volume 50";
        var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).Build();

        // Act
        await client.PublishAsync(message, CancellationToken.None);

        // Assert via API (poll a few times)
        ApiResponse<ZoneState>? api;
        var success = false;
        for (int i = 0; i < 20 && !success; i++)
        {
            var resp = await _client.GetAsync("/api/v1/zones/1", CancellationToken.None);
            if (resp.IsSuccessStatusCode)
            {
                api = await resp.Content.ReadFromJsonAsync<ApiResponse<ZoneState>>();
                if (api?.Data is not null)
                {
                    // Assuming ZoneState contains Volume or similar numeric field; fallback to checking PlaybackState if not available
                    // Here we check a hypothetical Volume field in ZoneState (adjust once exact shape is confirmed)
                    var volumeProp = api.Data.GetType().GetProperty("Volume");
                    if (volumeProp != null)
                    {
                        var vol = Convert.ToInt32(volumeProp.GetValue(api.Data) ?? 0);
                        if (vol == 50)
                        {
                            success = true;
                            break;
                        }
                    }
                }
            }
            await Task.Delay(250);
        }

        success.Should().BeTrue("API zone 1 volume should reflect the MQTT command 'volume 50'");

        await client.DisconnectAsync(CancellationToken.None);
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
