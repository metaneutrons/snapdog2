namespace SnapDog2.Tests.Integration.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

public class TestcontainersFixture : IAsyncLifetime
{
    private IContainer? _mosquittoContainer;

    public string MqttHost { get; private set; } = "localhost";
    public int MqttPort { get; private set; } = 0;

    public async Task InitializeAsync()
    {
        // Allow using existing devcontainer broker if desired
        var useCompose = string.Equals(
            Environment.GetEnvironmentVariable("SNAPDOG_E2E_USE_COMPOSE"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
        var composeHost = Environment.GetEnvironmentVariable("SNAPDOG_E2E_MQTT_HOST") ?? "localhost";
        var composePortVar = Environment.GetEnvironmentVariable("SNAPDOG_E2E_MQTT_PORT");
        if (useCompose && int.TryParse(composePortVar, out var composePort))
        {
            MqttHost = composeHost;
            MqttPort = composePort;
            Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST", MqttHost);
            Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT", MqttPort.ToString());
            return;
        }

        // Start Mosquitto (MQTT) container for integration/e2e scenarios.
        _mosquittoContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithName($"snapdog-tests-mosquitto-{Guid.NewGuid():N}")
            .WithPortBinding(1883, true) // random host port
            .WithCommand("/usr/sbin/mosquitto", "-c", "/mosquitto-no-auth.conf", "-v")
            .WithResourceMapping(
                new FileInfo(
                    Path.Combine(AppContext.BaseDirectory, "Integration", "Fixtures", "mosquitto-no-auth.conf")
                ),
                "/"
            )
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();

        await _mosquittoContainer.StartAsync();

        var hostPort = _mosquittoContainer.GetMappedPublicPort(1883);
        MqttPort = hostPort;
        MqttHost = _mosquittoContainer.Hostname == "localhost" ? "localhost" : _mosquittoContainer.Hostname;
        // Testcontainers uses docker host; for simplicity we connect via localhost:hostPort
        MqttHost = "localhost";

        // Expose to app factory via environment variables
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_HOST", MqttHost);
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_MQTT_PORT", MqttPort.ToString());
    }

    public async Task DisposeAsync()
    {
        if (_mosquittoContainer is not null)
        {
            try
            {
                await _mosquittoContainer.StopAsync();
            }
            catch
            { /* ignore */
            }
            await _mosquittoContainer.DisposeAsync();
        }
    }
}

[CollectionDefinition("IntegrationContainers")]
public class IntegrationContainersCollection : ICollectionFixture<TestcontainersFixture> { }
