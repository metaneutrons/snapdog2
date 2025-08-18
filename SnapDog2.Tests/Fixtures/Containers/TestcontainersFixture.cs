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

    public TestcontainersFixture() { }

    public async Task InitializeAsync()
    {
        Console.WriteLine("🚀 Starting integration test fixture initialization...");

        try
        {
            Console.WriteLine("📡 Step 1: Creating test network...");
            await CreateTestNetworkAsync();

            Console.WriteLine("📡 Step 2: Starting MQTT broker...");
            await StartMqttBrokerAsync();

            Console.WriteLine("📡 Step 3: Starting Snapcast server...");
            await StartSnapcastServerAsync();

            Console.WriteLine("📡 Step 4: Starting Snapcast clients...");
            await StartSnapcastClientsAsync();

            Console.WriteLine("✅ Integration test fixture initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize test fixture: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task CreateTestNetworkAsync()
    {
        _testNetwork = new NetworkBuilder()
            .WithName($"snapdog-test-{Guid.NewGuid():N}")
            .WithDriver(NetworkDriver.Bridge)
            .Build();

        await _testNetwork.CreateAsync();
        Console.WriteLine("✅ Test network created");
    }

    private async Task StartMqttBrokerAsync()
    {
        Console.WriteLine("🐳 Starting MQTT broker container...");

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
        Console.WriteLine($"✅ MQTT broker started at {MqttHost}:{MqttPort}");
    }

    private async Task StartSnapcastServerAsync()
    {
        Console.WriteLine("🐳 Starting Snapcast server container...");

        const int jsonRpcPort = 1705;
        const int httpPort = 1780;

        Console.WriteLine($"📡 Configuring Snapcast server with ports {jsonRpcPort} (JSON-RPC) and {httpPort} (HTTP)");

        // TODO: Use custom image once build issues are resolved
        // For now, use public image to unblock testing
        // _snapcastServerImage = await BuildSnapcastServerImageAsync();

        _snapcastServerContainer = new ContainerBuilder()
            .WithImage("saiyato/snapserver:latest") // Using public image temporarily
            .WithName($"snapcast-server-{Guid.NewGuid():N}")
            .WithNetwork(_testNetwork!)
            .WithNetworkAliases("snapcast-server")
            .WithPortBinding(0, jsonRpcPort)
            .WithPortBinding(0, httpPort)
            .WithCommand(
                "--tcp.enabled=true",
                "--http.enabled=true",
                $"--http.port={httpPort}",
                $"--tcp.port={jsonRpcPort}",
                // Configure multiple streams for zone testing
                "--source=pipe:///tmp/zone1.fifo?name=Zone1",
                "--source=pipe:///tmp/zone2.fifo?name=Zone2",
                "--source=pipe:///tmp/default.fifo?name=default"
            )
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(jsonRpcPort).UntilPortIsAvailable(httpPort))
            .WithStartupCallback(
                (container, ct) =>
                {
                    SnapcastJsonRpcPort = container.GetMappedPublicPort(jsonRpcPort);
                    SnapcastHttpPort = container.GetMappedPublicPort(httpPort);
                    Console.WriteLine(
                        $"📡 Snapcast server ports mapped: {SnapcastJsonRpcPort} (JSON-RPC), {SnapcastHttpPort} (HTTP)"
                    );
                    return Task.CompletedTask;
                }
            )
            .Build();

        Console.WriteLine("📡 Starting Snapcast server container...");
        await _snapcastServerContainer.StartAsync();
        Console.WriteLine(
            $"✅ Snapcast server started at {SnapcastHost}:{SnapcastJsonRpcPort} (JSON-RPC), {SnapcastHost}:{SnapcastHttpPort} (HTTP)"
        );
        Console.WriteLine("   Configured streams: Zone1, Zone2, default");
    }

    private async Task<IImage> BuildSnapcastServerImageAsync()
    {
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "devcontainer/snapcast-server")
            .WithDockerfile("Dockerfile")
            .WithName($"snapdog-snapcast-server-test:{Guid.NewGuid():N}"[..12])
            .Build();

        await image.CreateAsync();
        return image;
    }

    private async Task StartSnapcastClientsAsync()
    {
        Console.WriteLine("🐳 Starting Snapcast client containers...");

        // Use public snapcast client image instead of building custom one
        var clientImage = "saiyato/snapclient:latest";

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

        Console.WriteLine("✅ Snapcast clients started: living-room, kitchen, bedroom");
        Console.WriteLine("   Living Room MAC: 02:42:ac:11:00:10");
        Console.WriteLine("   Kitchen MAC: 02:42:ac:11:00:11");
        Console.WriteLine("   Bedroom MAC: 02:42:ac:11:00:12");
        Console.WriteLine("✅ Snapcast clients should now be connected to server");
    }

    private async Task<IContainer> StartSnapcastClientAsync(string clientImage, string clientId, string macAddress)
    {
        var networkName = _testNetwork!.Name;

        var container = new ContainerBuilder()
            .WithImage(clientImage)
            .WithName($"snapcast-client-{clientId}-{Guid.NewGuid():N}")
            .WithNetwork(_testNetwork!)
            .WithNetworkAliases($"snapcast-client-{clientId}")
            .WithEnvironment("FIXED_MAC_ADDRESS", macAddress)
            .WithCommand("--host", "snapcast-server", "--port", "1705", "--hostID", clientId)
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
        Console.WriteLine("🧹 Cleaning up integration test fixture...");

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
        Console.WriteLine("✅ Integration test fixture disposed successfully");
    }
}
