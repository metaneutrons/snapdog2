using System.Text;
using System.Text.Json;
using Cortex.Mediator;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using FluentAssertions;
using global::Knx.Falcon;
using global::Knx.Falcon.Configuration;
using global::Knx.Falcon.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Integrations.Knx;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration.Fixtures;

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
    private KnxdFixture? _knxdFixture;
    private IMqttClient? _testMqttClient;
    private KnxBus? _testKnxBus;

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

            // Start MQTT broker container
            await StartMqttBrokerAsync();

            // Start Snapcast server container
            await StartSnapcastServerAsync();

            // Start KNXd container
            await StartKnxdAsync();

            // Create and configure web application factory
            Console.WriteLine("🏭 About to create web application factory...");
            await CreateWebApplicationFactoryAsync();
            Console.WriteLine("✅ Web application factory created successfully");

            // Initialize test clients
            Console.WriteLine("🔧 About to initialize test clients...");
            await InitializeTestClientsAsync();
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
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_GATEWAY", null);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_PORT", null);

            // Dispose test clients
            if (_testMqttClient != null)
            {
                if (_testMqttClient.IsConnected)
                {
                    await _testMqttClient.DisconnectAsync();
                }
                _testMqttClient.Dispose();
            }

            if (_testKnxBus != null)
            {
                await _testKnxBus.DisposeAsync();
                _testKnxBus.Dispose();
            }

            // Dispose application factory
            _factory?.Dispose();

            // Dispose containers
            if (_mqttContainer != null)
            {
                await _mqttContainer.DisposeAsync();
            }

            if (_snapcastContainer != null)
            {
                await _snapcastContainer.DisposeAsync();
            }

            if (_knxdFixture != null)
            {
                await _knxdFixture.DisposeAsync();
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
        var configPath = "/Users/fabian/Source/snapdog/devcontainer/mosquitto/mosquitto.conf";
        var passwdPath = "/Users/fabian/Source/snapdog/devcontainer/mosquitto/passwd";

        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithPortBinding(containerMqttPort, true) // Use dynamic port binding
            .WithBindMount(configPath, "/mosquitto/config/mosquitto.conf")
            .WithBindMount(passwdPath, "/mosquitto/config/passwd")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(containerMqttPort))
            .Build();

        await _mqttContainer.StartAsync();

        MqttBrokerHost = _mqttContainer.Hostname;
        MqttBrokerPort = _mqttContainer.GetMappedPublicPort(containerMqttPort);

        Console.WriteLine($"✅ MQTT broker started at {MqttBrokerHost}:{MqttBrokerPort}");
    }

    private async Task StartSnapcastServerAsync()
    {
        Console.WriteLine("🐳 Starting Snapcast server container...");

        const int containerJsonRpcPort = 1705;
        const int containerHttpPort = 1780;

        // Use dynamic ports to avoid conflicts
        _snapcastContainer = new ContainerBuilder()
            .WithImage("saiyato/snapserver:latest")
            .WithPortBinding(containerJsonRpcPort, true) // Use dynamic port binding
            .WithPortBinding(containerHttpPort, true)
            .WithEnvironment("SNAPCAST_LOG_LEVEL", "info")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(containerJsonRpcPort))
            .Build();

        await _snapcastContainer.StartAsync();

        SnapcastHost = _snapcastContainer.Hostname;
        SnapcastJsonRpcPort = _snapcastContainer.GetMappedPublicPort(containerJsonRpcPort);
        SnapcastHttpPort = _snapcastContainer.GetMappedPublicPort(containerHttpPort);

        Console.WriteLine(
            $"✅ Snapcast server started at {SnapcastHost}:{SnapcastJsonRpcPort} (JSON-RPC), {SnapcastHost}:{SnapcastHttpPort} (HTTP)"
        );
    }

    private async Task StartKnxdAsync()
    {
        Console.WriteLine("🐳 Starting KNXd container...");

        _knxdFixture = new KnxdFixture();
        await _knxdFixture.InitializeAsync();

        KnxdHost = _knxdFixture.KnxHost;
        KnxdPort = _knxdFixture.KnxTcpPort;

        Console.WriteLine($"✅ KNXd started at {KnxdHost}:{KnxdPort}");
    }

    private Task CreateWebApplicationFactoryAsync()
    {
        Console.WriteLine("🏭 Creating web application factory...");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Set the environment to Testing to trigger the correct path in Program.cs
            builder.UseEnvironment("Testing");

            // Set environment variables with dynamic ports from containers
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", SnapcastHost);
            Environment.SetEnvironmentVariable(
                "SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT",
                SnapcastJsonRpcPort.ToString()
            );
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS", MqttBrokerHost);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PORT", MqttBrokerPort.ToString());
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_USERNAME", "snapdog");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PASSWORD", "snapdog");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_ENABLED", "true"); // Enable KNX service
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_CONNECTION_TYPE", "tunnel"); // Use tunnel mode like devcontainer
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_GATEWAY", KnxdHost);
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_PORT", KnxdPort.ToString());

            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    // Add test configuration with highest priority (last wins)
                    var testConfig = new Dictionary<string, string?>
                    {
                        // MQTT Configuration
                        ["Services:Mqtt:Enabled"] = "true",
                        ["Services:Mqtt:BrokerAddress"] = MqttBrokerHost,
                        ["Services:Mqtt:Port"] = MqttBrokerPort.ToString(),
                        ["Services:Mqtt:ClientIndex"] = "test-client",
                        ["Services:Mqtt:Username"] = "snapdog",
                        ["Services:Mqtt:Password"] = "snapdog",
                        ["Services:Mqtt:KeepAlive"] = "60",

                        // KNX Configuration
                        ["Services:Knx:Enabled"] = "true",
                        ["Services:Knx:ConnectionType"] = "Tunnel",
                        ["Services:Knx:Gateway"] = KnxdHost,
                        ["Services:Knx:Port"] = KnxdPort.ToString(),
                        ["Services:Knx:AutoReconnect"] = "true",

                        // Snapcast Configuration
                        ["Services:Snapcast:Address"] = SnapcastHost,
                        ["Services:Snapcast:JsonRpcPort"] = SnapcastJsonRpcPort.ToString(),
                        ["Services:Snapcast:HttpPort"] = SnapcastHttpPort.ToString(),
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
        });

        Console.WriteLine("🏭 Creating HTTP client...");
        HttpClient = _factory.CreateClient();
        Console.WriteLine("✅ HTTP client created");

        Console.WriteLine("🏭 Getting service provider...");
        ServiceProvider = _factory.Services;
        Console.WriteLine("✅ Service provider obtained");

        Console.WriteLine("✅ Web application factory created");
        return Task.CompletedTask;
    }

    private async Task InitializeTestClientsAsync()
    {
        Console.WriteLine("🔧 Initializing test clients...");

        // Initialize MQTT test client using MQTTnet 5.x patterns from the application
        await InitializeMqttTestClientAsync();

        // Initialize KNX test client using patterns from the application
        await InitializeKnxTestClientAsync();

        Console.WriteLine("✅ Test clients initialized");
    }

    private async Task InitializeMqttTestClientAsync()
    {
        try
        {
            // Create MQTT client using the same pattern as MqttService
            var factory = new MqttClientFactory();
            _testMqttClient = factory.CreateMqttClient();

            // Configure client options using dynamic container ports
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(MqttBrokerHost, MqttBrokerPort)
                .WithClientId("test-client")
                .WithCredentials("snapdog", "snapdog") // Use same credentials as devcontainer
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithCleanSession(true);

            var options = optionsBuilder.Build();

            // Connect to broker
            await _testMqttClient.ConnectAsync(options);

            Console.WriteLine($"✅ MQTT test client connected to {MqttBrokerHost}:{MqttBrokerPort}");
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
            var connectorParams = new IpTunnelingConnectorParameters(KnxdHost, KnxdPort);
            _testKnxBus = new KnxBus(connectorParams);

            // Connect to KNX bus
            await _testKnxBus.ConnectAsync();

            if (_testKnxBus.ConnectionState != BusConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    $"KNX test client connection failed - state: {_testKnxBus.ConnectionState}"
                );
            }

            Console.WriteLine($"✅ KNX test client connected to {KnxdHost}:{KnxdPort}");
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
        if (_testMqttClient == null || !_testMqttClient.IsConnected)
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

            await _testMqttClient.PublishAsync(message, cancellationToken);
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
        if (_testKnxBus == null || _testKnxBus.ConnectionState != BusConnectionState.Connected)
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

            await _testKnxBus.WriteGroupValueAsync(ga, groupValue);
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
            using var scope = ServiceProvider.CreateScope();
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
            var response = await HttpClient.GetAsync($"/api/v1/zones/{zoneIndex}", cancellationToken);
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
        var service = ServiceProvider.GetService<T>();

        // Debug output
        Console.WriteLine($"Checking service {typeof(T).Name}: {(service != null ? "Found" : "Not Found")}");

        service.Should().NotBeNull($"{typeof(T).Name} should be registered in DI container");
    }

    public void AssertConfigurationIsValid()
    {
        using var scope = ServiceProvider.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Verify MQTT configuration
        config["Services:Mqtt:Enabled"].Should().Be("true");
        config["Services:Mqtt:BrokerAddress"].Should().Be(MqttBrokerHost);
        config["Services:Mqtt:Port"].Should().Be(MqttBrokerPort.ToString());

        // Verify KNX configuration
        config["Services:Knx:Enabled"].Should().Be("true");
        config["Services:Knx:Gateway"].Should().Be(KnxdHost);
        config["Services:Knx:Port"].Should().Be(KnxdPort.ToString());
    }
}
