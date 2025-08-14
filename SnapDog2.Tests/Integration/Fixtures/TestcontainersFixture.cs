using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace SnapDog2.Tests.Integration.Fixtures;

public class TestcontainersFixture : IAsyncLifetime
{
    private IContainer? _mqttContainer;

    public string MqttHost { get; private set; } = "localhost";
    public int MqttPort { get; private set; }

    public async Task InitializeAsync()
    {
        // Start a Mosquitto broker for MQTT smoke tests
        const int containerMqttPort = 1883;
        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithPortBinding(0, containerMqttPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(containerMqttPort))
            .Build();

        await _mqttContainer.StartAsync();
        MqttPort = _mqttContainer.GetMappedPublicPort(containerMqttPort);
        MqttHost = "localhost";

        // Expose to other tests that rely on environment variables
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST", MqttHost);
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT", MqttPort.ToString());
    }

    public async Task DisposeAsync()
    {
        try { }
        finally
        {
            if (_mqttContainer is not null)
            {
                await _mqttContainer.StopAsync();
                await _mqttContainer.DisposeAsync();
            }
        }
    }
}

[CollectionDefinition("IntegrationContainers")]
public class IntegrationContainersCollection : ICollectionFixture<TestcontainersFixture> { }
