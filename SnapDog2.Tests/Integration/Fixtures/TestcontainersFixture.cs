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
        this._mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithPortBinding(0, containerMqttPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(containerMqttPort))
            .Build();

        await this._mqttContainer.StartAsync();
        this.MqttPort = this._mqttContainer.GetMappedPublicPort(containerMqttPort);
        this.MqttHost = "localhost";

        // Expose to other tests that rely on environment variables
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST", this.MqttHost);
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT", this.MqttPort.ToString());
    }

    public async Task DisposeAsync()
    {
        try { }
        finally
        {
            if (this._mqttContainer is not null)
            {
                await this._mqttContainer.StopAsync();
                await this._mqttContainer.DisposeAsync();
            }
        }
    }
}

[CollectionDefinition("IntegrationContainers")]
public class IntegrationContainersCollection : ICollectionFixture<TestcontainersFixture> { }
