using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using SnapDog2.Tests.Fixtures.Shared;

namespace SnapDog2.Tests.Fixtures.Containers;

/// <summary>
/// Enterprise-grade Testcontainers fixture providing isolated container infrastructure
/// for integration testing with real services (MQTT, Snapcast, KNX).
/// </summary>
public class TestcontainersFixture : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private INetwork? _testNetwork;
    private IContainer? _mqttContainer;
    private IContainer? _snapcastServerContainer;
    private IContainer? _livingRoomClientContainer;
    private IContainer? _kitchenClientContainer;
    private IContainer? _bedroomClientContainer;

    // Connection information
    public string MqttHost { get; private set; } = "127.0.0.1";
    public int MqttPort { get; private set; }
    public string SnapcastHost { get; private set; } = "127.0.0.1";
    public int SnapcastJsonRpcPort { get; private set; }
    public int SnapcastHttpPort { get; private set; }

    public TestcontainersFixture(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _output.WriteLine("🚀 Starting integration test fixture initialization...");

        try
        {
            await CreateTestNetworkAsync();
            await StartMqttBrokerAsync();
            await StartSnapcastServerAsync();
            await StartSnapcastClientsAsync();

            _output.WriteLine("✅ Integration test fixture initialized successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to initialize test fixture: {ex.Message}");
            throw;
        }
    }

    private async Task CreateTestNetworkAsync()
    {
        _testNetwork = new NetworkBuilder()
            .WithName($"snapdog-test-{Guid.NewGuid():N}"[..12])
            .WithDriver(NetworkDriver.Bridge)
            .Build();

        await _testNetwork.CreateAsync();
        _output.WriteLine("✅ Test network created");
    }

    private async Task StartMqttBrokerAsync()
    {
        _output.WriteLine("🐳 Starting MQTT broker container...");

        const int containerMqttPort = 1883;

        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithName($"mqtt-broker-{Guid.NewGuid():N}"[..8])
            .WithNetwork(_testNetwork!)
            .WithNetworkAliases("mqtt-broker")
            .WithPortBinding(0, containerMqttPort)
            .WithBindMount(
                Path.Combine(Directory.GetCurrentDirectory(), "Integration", "Fixtures", "mosquitto-no-auth.conf"),
                "/mosquitto/config/mosquitto.conf"
            )
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(containerMqttPort)
                    .UntilMessageIsLogged("mosquitto version .* running")
            )
            .WithStartupCallback(
                (container, ct) =>
                {
                    MqttPort = container.GetMappedPublicPort(containerMqttPort);
                    return Task.CompletedTask;
                }
            )
            .Build();

        await _mqttContainer.StartAsync();
        _output.WriteLine($"✅ MQTT broker started at {MqttHost}:{MqttPort}");
    }

    private async Task StartSnapcastServerAsync()
    {
        _output.WriteLine("🐳 Starting Snapcast server container...");

        const int jsonRpcPort = 1705;
        const int httpPort = 1780;

        _snapcastServerContainer = new ContainerBuilder()
            .WithImage("badaix/snapserver:latest")
            .WithName($"snapcast-server-{Guid.NewGuid():N}"[..8])
            .WithNetwork(_testNetwork!)
            .WithNetworkAliases("snapcast-server")
            .WithPortBinding(0, jsonRpcPort)
            .WithPortBinding(0, httpPort)
            .WithCommand(
                "--tcp.enabled=true",
                "--http.enabled=true",
                $"--http.port={httpPort}",
                $"--tcp.port={jsonRpcPort}",
                "--source=pipe:///tmp/snapfifo?name=default"
            )
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilPortIsAvailable(jsonRpcPort).UntilMessageIsLogged("Server listening")
            )
            .WithStartupCallback(
                (container, ct) =>
                {
                    SnapcastJsonRpcPort = container.GetMappedPublicPort(jsonRpcPort);
                    SnapcastHttpPort = container.GetMappedPublicPort(httpPort);
                    return Task.CompletedTask;
                }
            )
            .Build();

        await _snapcastServerContainer.StartAsync();
        _output.WriteLine(
            $"✅ Snapcast server started at {SnapcastHost}:{SnapcastJsonRpcPort} (JSON-RPC), {SnapcastHost}:{SnapcastHttpPort} (HTTP)"
        );
    }

    private async Task StartSnapcastClientsAsync()
    {
        _output.WriteLine("🐳 Starting Snapcast client containers...");

        // Build custom Snapcast client image if it doesn't exist
        var clientImage = await BuildSnapcastClientImageAsync();

        var clientConfigs = new[]
        {
            ("living-room", "02:42:ac:11:00:10"),
            ("kitchen", "02:42:ac:11:00:11"),
            ("bedroom", "02:42:ac:11:00:12"),
        };

        var clientTasks = clientConfigs.Select(async config =>
        {
            var (clientId, macAddress) = config;
            return await StartSnapcastClientAsync(clientImage, clientId, macAddress);
        });

        var clients = await Task.WhenAll(clientTasks);
        _livingRoomClientContainer = clients[0];
        _kitchenClientContainer = clients[1];
        _bedroomClientContainer = clients[2];

        _output.WriteLine("✅ Snapcast clients started: living-room, kitchen, bedroom");
        _output.WriteLine("   Living Room MAC: 02:42:ac:11:00:10");
        _output.WriteLine("   Kitchen MAC: 02:42:ac:11:00:11");
        _output.WriteLine("   Bedroom MAC: 02:42:ac:11:00:12");
        _output.WriteLine("✅ Snapcast clients should now be connected to server");
    }

    private async Task<IImage> BuildSnapcastClientImageAsync()
    {
        var dockerfile =
            @"
FROM badaix/snapclient:latest
RUN apt-get update && apt-get install -y alsa-utils && rm -rf /var/lib/apt/lists/*
ENTRYPOINT [""snapclient""]
";

        var image = new ImageFromDockerfileBuilder()
            .WithName("snapdog-snapcast-client:test")
            .WithDockerfile(dockerfile)
            .Build();

        await image.CreateAsync();
        return image;
    }

    private async Task<IContainer> StartSnapcastClientAsync(IImage clientImage, string clientId, string macAddress)
    {
        var networkName = _testNetwork!.Name;

        var container = new ContainerBuilder()
            .WithImage(clientImage)
            .WithName($"snapcast-client-{clientId}-{Guid.NewGuid():N}"[..8])
            .WithNetwork(_testNetwork!)
            .WithNetworkAliases($"snapcast-client-{clientId}")
            .WithEnvironment("FIXED_MAC_ADDRESS", macAddress)
            .WithCommand("--host", "snapcast-server", "--port", "1704", "--hostID", clientId)
            .WithCreateParameterModifier(parameters =>
            {
                // Set static MAC address on container network interface
                parameters.NetworkingConfig ??= new NetworkingConfig();
                parameters.NetworkingConfig.EndpointsConfig ??= new Dictionary<string, EndpointSettings>();
                parameters.NetworkingConfig.EndpointsConfig[networkName] = new EndpointSettings
                {
                    MacAddress = macAddress,
                };
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Connected to"))
            .Build();

        await container.StartAsync();
        return container;
    }

    public async Task DisposeAsync()
    {
        _output.WriteLine("🧹 Cleaning up integration test fixture...");

        var disposeTasks = new List<Task>();

        if (_livingRoomClientContainer != null)
            disposeTasks.Add(_livingRoomClientContainer.DisposeAsync().AsTask());
        if (_kitchenClientContainer != null)
            disposeTasks.Add(_kitchenClientContainer.DisposeAsync().AsTask());
        if (_bedroomClientContainer != null)
            disposeTasks.Add(_bedroomClientContainer.DisposeAsync().AsTask());
        if (_snapcastServerContainer != null)
            disposeTasks.Add(_snapcastServerContainer.DisposeAsync().AsTask());
        if (_mqttContainer != null)
            disposeTasks.Add(_mqttContainer.DisposeAsync().AsTask());
        if (_testNetwork != null)
            disposeTasks.Add(_testNetwork.DisposeAsync().AsTask());

        await Task.WhenAll(disposeTasks);
        _output.WriteLine("✅ Integration test fixture disposed successfully");
    }
}
