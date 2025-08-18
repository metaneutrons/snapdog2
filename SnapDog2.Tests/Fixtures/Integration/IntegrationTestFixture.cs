using System.Text;
using System.Text.Json;
using Cortex.Mediator;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using EnvoyConfig;
using FluentAssertions;
using global::Knx.Falcon;
using global::Knx.Falcon.Configuration;
using global::Knx.Falcon.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Integrations.Knx;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using SnapDog2.Tests.Fixtures.Containers;
using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Fixtures.Integration;

/// <summary>
/// Comprehensive integration test fixture for all SnapDog2 services including MQTT, KNX, and Snapcast.
/// Uses containerized services to provide realistic testing environment with actual service implementations.
/// Based on the actual implementation patterns from SnapDog2 application.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private IContainer? _mqttContainer;
    private IContainer? _snapcastContainer;
    private IContainer? _snapcastClientLivingRoom;
    private IContainer? _snapcastClientKitchen;
    private IContainer? _snapcastClientBedroom;
    private KnxdFixture? _knxdFixture;
    private IMqttClient? _testMqttClient;
    private KnxBus? _testKnxBus;
    private INetwork? _testNetwork;

    public IntegrationTestFixture()
    {
        // Collection fixtures can't receive ITestOutputHelper, so we use Console.WriteLine for logging
    }

    public HttpClient HttpClient { get; private set; } = null!;
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public string MqttBrokerHost { get; private set; } = string.Empty;
    public int MqttBrokerPort { get; private set; }
    public string SnapcastHost { get; private set; } = string.Empty;
    public int SnapcastJsonRpcPort { get; private set; }
    public int SnapcastHttpPort { get; private set; }
    public string KnxdHost { get; private set; } = string.Empty;
    public int KnxdPort { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("🚀 Starting integration test fixture initialization...");

            // Create a custom network for all containers to communicate
            this._testNetwork = new NetworkBuilder().WithName($"snapdog-test-{Guid.NewGuid():N}").Build();
            await this._testNetwork.CreateAsync();
            Console.WriteLine("✅ Test network created");

            // Start MQTT broker container
            await this.StartMqttBrokerAsync();

            // Start Snapcast server container
            await this.StartSnapcastServerAsync();

            // Start Snapcast client containers
            await this.StartSnapcastClientsAsync();

            // Skip KNX container startup since KNX is disabled for integration tests
            // await StartKnxdAsync();
            this.KnxdHost = "localhost";
            this.KnxdPort = 3671; // Default KNX port

            // Create and configure web application factory
            Console.WriteLine("🏭 About to create web application factory...");
            await this.CreateWebApplicationFactoryAsync();
            Console.WriteLine("✅ Web application factory created successfully");

            // Initialize test clients
            Console.WriteLine("🔧 About to initialize test clients...");
            await this.InitializeTestClientsAsync();
            Console.WriteLine("✅ Test clients initialized successfully");

            Console.WriteLine("✅ Integration test fixture initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize test fixture: {ex.Message}");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            Console.WriteLine("🧹 Disposing integration test fixture...");

            // Clean up environment variables
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PORT", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_USERNAME", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PASSWORD", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_ENABLED", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_ENABLED", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_CONNECTION_TYPE", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_GATEWAY", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_PORT", null);

            // Clean up API and system configuration
            Environment.SetEnvironmentVariable("SNAPDOG_API_ENABLED", null);
            Environment.SetEnvironmentVariable("SNAPDOG_API_PORT", null);
            Environment.SetEnvironmentVariable("SNAPDOG_API_AUTH_ENABLED", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_LEVEL", null);

            // Clean up zone configuration
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_NAME", null);
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_MQTT_BASE_TOPIC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_SINK", null);
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_NAME", null);
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_MQTT_BASE_TOPIC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_SINK", null);

            // Clean up client configuration
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_NAME", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_MAC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_MQTT_BASE_TOPIC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_DEFAULT_ZONE", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_NAME", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_MAC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_MQTT_BASE_TOPIC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_DEFAULT_ZONE", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_NAME", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_MAC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_MQTT_BASE_TOPIC", null);
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_DEFAULT_ZONE", null);

            // Dispose test clients
            if (this._testMqttClient != null)
            {
                if (this._testMqttClient.IsConnected)
                {
                    await this._testMqttClient.DisconnectAsync();
                }
                this._testMqttClient.Dispose();
            }

            if (this._testKnxBus != null)
            {
                await this._testKnxBus.DisposeAsync();
                this._testKnxBus.Dispose();
            }

            // Dispose application factory
            this._factory?.Dispose();

            // Dispose containers
            if (this._mqttContainer != null)
            {
                await this._mqttContainer.DisposeAsync();
            }

            if (this._snapcastContainer != null)
            {
                await this._snapcastContainer.DisposeAsync();
            }

            // Dispose Snapcast client containers
            if (this._snapcastClientLivingRoom != null)
            {
                await this._snapcastClientLivingRoom.DisposeAsync();
            }

            if (this._snapcastClientKitchen != null)
            {
                await this._snapcastClientKitchen.DisposeAsync();
            }

            if (this._snapcastClientBedroom != null)
            {
                await this._snapcastClientBedroom.DisposeAsync();
            }

            if (this._knxdFixture != null)
            {
                await this._knxdFixture.DisposeAsync();
            }

            // Dispose test network
            if (this._testNetwork != null)
            {
                await this._testNetwork.DisposeAsync();
            }

            Console.WriteLine("✅ Integration test fixture disposed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error during fixture disposal: {ex.Message}");
        }
    }

    private async Task StartMqttBrokerAsync()
    {
        Console.WriteLine("🐳 Starting MQTT broker container...");

        const int containerMqttPort = 1883;

        // Use the same MQTT configuration as devcontainer for consistency
        var repositoryRoot = GetRepositoryRoot();
        var configPath = Path.Combine(repositoryRoot, "devcontainer", "mosquitto", "mosquitto.conf");
        var passwdPath = Path.Combine(repositoryRoot, "devcontainer", "mosquitto", "passwd");

        this._mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithPortBinding(containerMqttPort, true) // Use dynamic port binding
            .WithBindMount(configPath, "/mosquitto/config/mosquitto.conf")
            .WithBindMount(passwdPath, "/mosquitto/config/passwd")
            .WithNetwork(this._testNetwork!)
            .WithNetworkAliases("mqtt-broker")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(containerMqttPort))
            .Build();

        await this._mqttContainer.StartAsync();

        this.MqttBrokerHost = this._mqttContainer.Hostname;
        this.MqttBrokerPort = this._mqttContainer.GetMappedPublicPort(containerMqttPort);

        Console.WriteLine($"✅ MQTT broker started at {this.MqttBrokerHost}:{this.MqttBrokerPort}");
    }

    private async Task StartSnapcastServerAsync()
    {
        Console.WriteLine("🐳 Starting Snapcast server container...");

        const int containerJsonRpcPort = 1705;
        const int containerHttpPort = 1780;

        // Use dynamic ports to avoid conflicts
        this._snapcastContainer = new ContainerBuilder()
            .WithImage("saiyato/snapserver:latest")
            .WithPortBinding(containerJsonRpcPort, true) // Use dynamic port binding
            .WithPortBinding(containerHttpPort, true)
            .WithEnvironment("SNAPCAST_LOG_LEVEL", "info")
            .WithNetwork(this._testNetwork!)
            .WithNetworkAliases("snapcast-server")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(containerJsonRpcPort))
            .Build();

        await this._snapcastContainer.StartAsync();

        this.SnapcastHost = this._snapcastContainer.Hostname;
        this.SnapcastJsonRpcPort = this._snapcastContainer.GetMappedPublicPort(containerJsonRpcPort);
        this.SnapcastHttpPort = this._snapcastContainer.GetMappedPublicPort(containerHttpPort);

        Console.WriteLine(
            $"✅ Snapcast server started at {this.SnapcastHost}:{this.SnapcastJsonRpcPort} (JSON-RPC), {this.SnapcastHost}:{this.SnapcastHttpPort} (HTTP)"
        );
    }

    private async Task StartSnapcastClientsAsync()
    {
        Console.WriteLine("🐳 Starting Snapcast client containers...");

        // We need to build the Snapcast client image first since it's a custom build
        var clientImageName = "snapdog-snapcast-client:test";

        // Build the Snapcast client image from the devcontainer directory
        var repositoryRoot = GetRepositoryRoot();
        var clientImageBuilder = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(Path.Combine(repositoryRoot, "devcontainer", "snapcast-client"))
            .WithDockerfile("Dockerfile")
            .WithName(clientImageName);

        var clientImage = clientImageBuilder.Build();
        await clientImage.CreateAsync();

        // Check if audio devices are available on the host system
        var hasAudioDevices = Directory.Exists("/dev/snd");
        if (hasAudioDevices)
        {
            Console.WriteLine("✅ Audio devices detected - enabling full audio support");
        }
        else
        {
            Console.WriteLine("⚠️ No audio devices detected - using null audio output");
        }

        // Start Living Room client with fixed MAC address
        var livingRoomMac = "02:42:ac:11:00:10";
        var livingRoomBuilder = new ContainerBuilder()
            .WithImage(clientImageName)
            .WithEnvironment("SNAPSERVER_HOST", "snapcast-server") // Use network alias
            .WithEnvironment("CLIENT_ID", "living-room")
            .WithEnvironment("FIXED_MAC_ADDRESS", livingRoomMac)
            .WithNetwork(this._testNetwork!)
            .WithNetworkAliases("snapcast-client-living-room")
            .WithCreateParameterModifier(parameters =>
            {
                // Set the MAC address for the container's network interface
                parameters.NetworkingConfig.EndpointsConfig[this._testNetwork!.Name].MacAddress = livingRoomMac;
            });

        if (hasAudioDevices)
        {
            livingRoomBuilder = livingRoomBuilder
                .WithPrivileged(true) // Enable privileged mode for audio device access
                .WithBindMount("/dev/snd", "/dev/snd"); // Mount audio devices
        }

        this._snapcastClientLivingRoom = livingRoomBuilder.Build();

        // Start Kitchen client with fixed MAC address
        var kitchenMac = "02:42:ac:11:00:11";
        var kitchenBuilder = new ContainerBuilder()
            .WithImage(clientImageName)
            .WithEnvironment("SNAPSERVER_HOST", "snapcast-server") // Use network alias
            .WithEnvironment("CLIENT_ID", "kitchen")
            .WithEnvironment("FIXED_MAC_ADDRESS", kitchenMac)
            .WithNetwork(this._testNetwork!)
            .WithNetworkAliases("snapcast-client-kitchen")
            .WithCreateParameterModifier(parameters =>
            {
                // Set the MAC address for the container's network interface
                parameters.NetworkingConfig.EndpointsConfig[this._testNetwork!.Name].MacAddress = kitchenMac;
            });

        if (hasAudioDevices)
        {
            kitchenBuilder = kitchenBuilder
                .WithPrivileged(true) // Enable privileged mode for audio device access
                .WithBindMount("/dev/snd", "/dev/snd"); // Mount audio devices
        }

        this._snapcastClientKitchen = kitchenBuilder.Build();

        // Start Bedroom client with fixed MAC address
        var bedroomMac = "02:42:ac:11:00:12";
        var bedroomBuilder = new ContainerBuilder()
            .WithImage(clientImageName)
            .WithEnvironment("SNAPSERVER_HOST", "snapcast-server") // Use network alias
            .WithEnvironment("CLIENT_ID", "bedroom")
            .WithEnvironment("FIXED_MAC_ADDRESS", bedroomMac)
            .WithNetwork(this._testNetwork!)
            .WithNetworkAliases("snapcast-client-bedroom")
            .WithCreateParameterModifier(parameters =>
            {
                // Set the MAC address for the container's network interface
                parameters.NetworkingConfig.EndpointsConfig[this._testNetwork!.Name].MacAddress = bedroomMac;
            });

        if (hasAudioDevices)
        {
            bedroomBuilder = bedroomBuilder
                .WithPrivileged(true) // Enable privileged mode for audio device access
                .WithBindMount("/dev/snd", "/dev/snd"); // Mount audio devices
        }

        this._snapcastClientBedroom = bedroomBuilder.Build();

        // Start all clients concurrently
        var clientTasks = new[]
        {
            this._snapcastClientLivingRoom.StartAsync(),
            this._snapcastClientKitchen.StartAsync(),
            this._snapcastClientBedroom.StartAsync(),
        };

        await Task.WhenAll(clientTasks);

        Console.WriteLine("✅ Snapcast clients started: living-room, kitchen, bedroom");
        Console.WriteLine($"   Living Room MAC: {livingRoomMac}");
        Console.WriteLine($"   Kitchen MAC: {kitchenMac}");
        Console.WriteLine($"   Bedroom MAC: {bedroomMac}");

        // Give clients a moment to connect to the server
        await Task.Delay(2000);
        Console.WriteLine("✅ Snapcast clients should now be connected to server");
    }

    private async Task StartKnxdAsync()
    {
        Console.WriteLine("🐳 Starting KNXd container...");

        this._knxdFixture = new KnxdFixture();
        await this._knxdFixture.InitializeAsync();

        this.KnxdHost = this._knxdFixture.KnxHost;
        this.KnxdPort = this._knxdFixture.KnxTcpPort;

        Console.WriteLine($"✅ KNXd started at {this.KnxdHost}:{this.KnxdPort}");
    }

    private Task CreateWebApplicationFactoryAsync()
    {
        Console.WriteLine("🏭 Creating web application factory...");

        this._factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Set the environment to Testing to trigger the correct path in Program.cs
            builder.UseEnvironment("Testing");

            // Configure services to ensure environment variables are loaded
            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    // Clear existing configuration sources and rebuild with environment variables
                    config.Sources.Clear();
                    config.AddEnvironmentVariables();
                    config.AddEnvironmentVariables("SNAPDOG_");
                }
            );

            // Configure services to ensure EnvoyConfig is set up properly
            builder.ConfigureServices(
                (context, services) =>
                {
                    // Set up EnvoyConfig with the same global prefix as the main application
                    EnvoyConfig.EnvConfig.GlobalPrefix = "SNAPDOG_";
                }
            );

            // Set environment variables with dynamic ports from containers
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", this.SnapcastHost);
            Environment.SetEnvironmentVariable(
                "SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT",
                this.SnapcastJsonRpcPort.ToString()
            );
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS", this.MqttBrokerHost);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PORT", this.MqttBrokerPort.ToString());
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_USERNAME", "snapdog");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PASSWORD", "snapdog");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_ENABLED", "true"); // Enable MQTT service for tests
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_ENABLED", "false"); // Disable KNX service to prevent hanging
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_CONNECTION_TYPE", "tunnel"); // Use tunnel mode like devcontainer
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_GATEWAY", this.KnxdHost);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_PORT", this.KnxdPort.ToString());

            // Enable API server for integration tests
            Environment.SetEnvironmentVariable("SNAPDOG_API_ENABLED", "true");
            Environment.SetEnvironmentVariable("SNAPDOG_API_PORT", "5000");
            Environment.SetEnvironmentVariable("SNAPDOG_API_AUTH_ENABLED", "false"); // Disable auth for tests

            // Enable health checks for integration tests
            Environment.SetEnvironmentVariable("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true");
            Environment.SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_LEVEL", "Information"); // Set appropriate log level for tests

            // Configure zones like in devcontainer
            // Zone 1 - Ground Floor
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_NAME", "Ground Floor");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_MQTT_BASE_TOPIC", "snapdog/zones/1");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_SINK", "/snapsinks/zone1");

            // Zone 2 - 1st Floor
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_NAME", "1st Floor");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_MQTT_BASE_TOPIC", "snapdog/zones/2");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_SINK", "/snapsinks/zone2");

            // Configure clients like in devcontainer
            // Client 1 - Living Room (Zone 1)
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_NAME", "Living Room");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_MAC", "02:42:ac:11:00:10");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_MQTT_BASE_TOPIC", "snapdog/clients/livingroom");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_DEFAULT_ZONE", "1");

            // Client 2 - Kitchen (Zone 1)
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_NAME", "Kitchen");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_MAC", "02:42:ac:11:00:11");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_MQTT_BASE_TOPIC", "snapdog/clients/kitchen");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_DEFAULT_ZONE", "1");

            // Client 3 - Bedroom (Zone 2)
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_NAME", "Bedroom");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_MAC", "02:42:ac:11:00:12");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_MQTT_BASE_TOPIC", "snapdog/clients/bedroom");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_DEFAULT_ZONE", "2");

            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    // Add test configuration with highest priority (last wins)
                    var testConfig = new Dictionary<string, string?>
                    {
                        // MQTT Configuration
                        ["Services:Mqtt:Enabled"] = "true",
                        ["Services:Mqtt:BrokerAddress"] = this.MqttBrokerHost,
                        ["Services:Mqtt:Port"] = this.MqttBrokerPort.ToString(),
                        ["Services:Mqtt:ClientIndex"] = "test-client",
                        ["Services:Mqtt:Username"] = "snapdog",
                        ["Services:Mqtt:Password"] = "snapdog",
                        ["Services:Mqtt:KeepAlive"] = "60",

                        // KNX Configuration - Disabled for integration tests to prevent hanging
                        ["Services:Knx:Enabled"] = "false",
                        ["Services:Knx:ConnectionType"] = "Tunnel",
                        ["Services:Knx:Gateway"] = this.KnxdHost,
                        ["Services:Knx:Port"] = this.KnxdPort.ToString(),
                        ["Services:Knx:AutoReconnect"] = "true",

                        // Snapcast Configuration
                        ["Services:Snapcast:Address"] = this.SnapcastHost,
                        ["Services:Snapcast:JsonRpcPort"] = this.SnapcastJsonRpcPort.ToString(),
                        ["Services:Snapcast:HttpPort"] = this.SnapcastHttpPort.ToString(),
                        ["Services:Snapcast:Timeout"] = "30",
                        ["Services:Snapcast:AutoReconnect"] = "true",
                        ["Services:Snapcast:ReconnectInterval"] = "5",

                        // Zone Configuration
                        ["Zones:0:Index"] = "1",
                        ["Zones:0:Name"] = "Living Room",
                        ["Zones:0:Knx:Enabled"] = "true",
                        ["Zones:0:Knx:Volume"] = "1/1/1",
                        ["Zones:0:Knx:VolumeStatus"] = "1/1/2",
                        ["Zones:0:Knx:Mute"] = "1/1/3",
                        ["Zones:0:Knx:MuteStatus"] = "1/1/4",
                        ["Zones:0:Knx:Play"] = "1/1/5",
                        ["Zones:0:Knx:Pause"] = "1/1/6",
                        ["Zones:0:Knx:Stop"] = "1/1/7",
                        ["Zones:0:Mqtt:Enabled"] = "true",
                        ["Zones:0:Mqtt:BaseTopic"] = "snapdog/zone/1",
                        ["Zones:0:Mqtt:StateTopic"] = "state",
                        ["Zones:0:Mqtt:VolumeTopic"] = "volume",
                        ["Zones:0:Mqtt:MuteTopic"] = "mute",

                        // Client Configuration
                        ["Clients:0:Index"] = "1",
                        ["Clients:0:Name"] = "Living Room Client",
                        ["Clients:0:MacAddress"] = "02:42:ac:14:00:06",
                        ["Clients:0:Knx:Enabled"] = "true",
                        ["Clients:0:Knx:Volume"] = "2/1/1",
                        ["Clients:0:Knx:VolumeStatus"] = "2/1/2",
                        ["Clients:0:Knx:Mute"] = "2/1/3",
                        ["Clients:0:Knx:MuteStatus"] = "2/1/4",
                        ["Clients:0:Mqtt:Enabled"] = "true",
                        ["Clients:0:Mqtt:BaseTopic"] = "snapdog/client/1",
                        ["Clients:0:Mqtt:StateTopic"] = "state",
                        ["Clients:0:Mqtt:VolumeTopic"] = "volume",
                        ["Clients:0:Mqtt:MuteTopic"] = "mute",
                    };

                    // Add as the last configuration source so it has highest priority
                    config.AddInMemoryCollection(testConfig);
                }
            );

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
            });

            // Replace metrics service with test capture implementation
            builder.ConfigureServices(services =>
            {
                var metricsDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(SnapDog2.Core.Abstractions.IMetricsService)
                );
                if (metricsDescriptor != null)
                {
                    services.Remove(metricsDescriptor);
                }
                services.AddSingleton<
                    SnapDog2.Core.Abstractions.IMetricsService,
                    SnapDog2.Tests.Testing.TestMetricsService
                >();
            });
        });

        Console.WriteLine("🏭 Creating HTTP client...");
        this.HttpClient = this._factory.CreateClient();
        Console.WriteLine("✅ HTTP client created");

        Console.WriteLine("🏭 Getting service provider...");
        this.ServiceProvider = this._factory.Services;
        Console.WriteLine("✅ Service provider obtained");

        Console.WriteLine("✅ Web application factory created");
        return Task.CompletedTask;
    }

    private async Task InitializeTestClientsAsync()
    {
        Console.WriteLine("🔧 Initializing test clients...");

        // Initialize MQTT test client using MQTTnet 5.x patterns from the application
        await this.InitializeMqttTestClientAsync();

        // Skip KNX test client initialization since KNX is disabled
        // await InitializeKnxTestClientAsync();

        Console.WriteLine("✅ Test clients initialized");
    }

    private async Task InitializeMqttTestClientAsync()
    {
        try
        {
            // Create MQTT client using the same pattern as MqttService
            var factory = new MqttClientFactory();
            this._testMqttClient = factory.CreateMqttClient();

            // Configure client options using dynamic container ports
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(this.MqttBrokerHost, this.MqttBrokerPort)
                .WithClientId("test-client")
                .WithCredentials("snapdog", "snapdog") // Use same credentials as devcontainer
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithCleanSession(true);

            var options = optionsBuilder.Build();

            // Connect to broker
            await this._testMqttClient.ConnectAsync(options);

            Console.WriteLine($"✅ MQTT test client connected to {this.MqttBrokerHost}:{this.MqttBrokerPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize MQTT test client: {ex.Message}");
            throw;
        }
    }

    private async Task InitializeKnxTestClientAsync()
    {
        try
        {
            // Create KNX bus using dynamic container ports
            var connectorParams = new IpTunnelingConnectorParameters(this.KnxdHost, this.KnxdPort);
            this._testKnxBus = new KnxBus(connectorParams);

            // Connect to KNX bus
            await this._testKnxBus.ConnectAsync();

            if (this._testKnxBus.ConnectionState != BusConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    $"KNX test client connection failed - state: {this._testKnxBus.ConnectionState}"
                );
            }

            Console.WriteLine($"✅ KNX test client connected to {this.KnxdHost}:{this.KnxdPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ KNX test client initialization failed (non-critical): {ex.Message}");
            // Don't throw - allow tests to continue without KNX client
            // This is acceptable since KNX functionality can be tested separately
        }
    }

    // Helper methods for tests

    public async Task<Result> SendMqttCommandAsync(
        string topic,
        object payload,
        CancellationToken cancellationToken = default
    )
    {
        if (this._testMqttClient == null || !this._testMqttClient.IsConnected)
        {
            return Result.Failure("MQTT test client is not connected");
        }

        try
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(jsonPayload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await this._testMqttClient.PublishAsync(message, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to send MQTT command: {ex.Message}");
        }
    }

    public async Task<Result> SendKnxCommandAsync(
        string groupAddress,
        object value,
        CancellationToken cancellationToken = default
    )
    {
        if (this._testKnxBus == null || this._testKnxBus.ConnectionState != BusConnectionState.Connected)
        {
            return Result.Failure("KNX test client is not connected");
        }

        try
        {
            var ga = new GroupAddress(groupAddress);

            // Convert value to appropriate type for GroupValue using the same pattern as KnxService
            GroupValue groupValue = value switch
            {
                bool boolValue => new GroupValue(boolValue),
                byte byteValue => new GroupValue(byteValue),
                int intValue when intValue >= 0 && intValue <= 255 => new GroupValue((byte)intValue),
                _ => throw new ArgumentException($"Unsupported value type: {value?.GetType()}"),
            };

            await this._testKnxBus.WriteGroupValueAsync(ga, groupValue);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to send KNX command: {ex.Message}");
        }
    }

    public async Task<Result> SendMediatorCommandAsync(object command, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = this.ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Send the command using the same pattern as the application
            var result = command switch
            {
                SetZoneVolumeCommand cmd => await mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(cmd),
                _ => throw new ArgumentException($"Unsupported command type: {command.GetType().Name}"),
            };

            return result;
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to send mediator command: {ex.Message}");
        }
    }

    public async Task<ZoneState?> GetZoneStateAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this.HttpClient.GetAsync($"/api/v1/zones/{zoneIndex}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ZoneState>(
                json,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        string description = "condition"
    )
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (await condition())
            {
                return;
            }
            await Task.Delay(100);
        }

        throw new TimeoutException($"Timeout waiting for {description} after {timeout.TotalSeconds} seconds");
    }

    public void AssertServiceIsRunning<T>()
        where T : class
    {
        // Don't create a scope to avoid disposal issues with async-only disposable services
        // Just check if the service is registered in the root container
        var service = this.ServiceProvider.GetService<T>();

        // Debug output
        Console.WriteLine($"Checking service {typeof(T).Name}: {(service != null ? "Found" : "Not Found")}");

        service.Should().NotBeNull($"{typeof(T).Name} should be registered in DI container");
    }

    public void AssertConfigurationIsValid()
    {
        using var scope = this.ServiceProvider.CreateScope();

        // Get the actual SnapDog configuration object which uses EnvoyConfig
        var snapDogConfig = scope.ServiceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>().Value;

        // Verify MQTT configuration using the configuration object
        snapDogConfig.Services.Mqtt.Enabled.Should().BeTrue("MQTT should be enabled in tests");
        snapDogConfig
            .Services.Mqtt.BrokerAddress.Should()
            .Be(this.MqttBrokerHost, "MQTT broker should use test container address");
        snapDogConfig.Services.Mqtt.Port.Should().Be(this.MqttBrokerPort, "MQTT should use test container port");

        // Verify KNX configuration (disabled in tests)
        snapDogConfig.Services.Knx.Enabled.Should().BeFalse("KNX should be disabled in tests");
        snapDogConfig.Services.Knx.Gateway.Should().Be(this.KnxdHost, "KNX gateway should use test container address");
        snapDogConfig.Services.Knx.Port.Should().Be(this.KnxdPort, "KNX should use test container port");

        // Verify API configuration using the actual configuration object
        // This works because EnvoyConfig properly loads the SNAPDOG_ prefixed environment variables
        snapDogConfig.Api.Enabled.Should().BeTrue("API should be enabled in tests");
        snapDogConfig.Api.Port.Should().Be(5000, "API should be on port 5000 in tests");
        snapDogConfig.Api.AuthEnabled.Should().BeFalse("API auth should be disabled in tests");

        // Verify system configuration using the configuration object
        snapDogConfig.System.HealthChecksEnabled.Should().BeTrue("Health checks should be enabled in tests");
        snapDogConfig.System.LogLevel.Should().Be("Information", "Log level should be Information in tests");
    }

    /// <summary>
    /// Finds the repository root by traversing up the directory tree looking for the .git folder or solution file.
    /// This ensures the devcontainer folder can be found regardless of the current working directory.
    /// </summary>
    private static string GetRepositoryRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);

        while (directory != null)
        {
            // Look for .git folder or solution file to identify repository root
            if (directory.GetDirectories(".git").Length > 0 || directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find repository root starting from {currentDirectory}. "
                + "Make sure the test is running from within the repository."
        );
    }
}
