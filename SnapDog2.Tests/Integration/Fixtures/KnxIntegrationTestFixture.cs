namespace SnapDog2.Tests.Integration.Fixtures;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using global::Knx.Falcon;
using global::Knx.Falcon.Configuration;
using global::Knx.Falcon.Sdk;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Xunit;

/// <summary>
/// Comprehensive test fixture for KNX integration testing.
/// Provides all necessary infrastructure: KNX, MQTT, Snapcast, API, and Mediator spy.
/// Manages TestContainers for realistic integration testing environment.
/// </summary>
public class KnxIntegrationTestFixture : IAsyncLifetime
{
    // Test Clients
    public KnxTestClient KnxClient { get; private set; } = null!;
    public MqttTestClient MqttTestClient { get; private set; } = null!;
    public SnapcastTestClient SnapcastTestClient { get; private set; } = null!;
    public HttpClient ApiClient { get; private set; } = null!;
    public MediatorSpy MediatorSpy { get; private set; } = null!;

    // TestContainers
    public IContainer KnxdContainer { get; private set; } = null!;
    public IContainer MqttContainer { get; private set; } = null!;
    public IContainer SnapcastContainer { get; private set; } = null!;
    public IContainer AppContainer { get; private set; } = null!;

    // Connection Details
    public string KnxHost { get; private set; } = null!;
    public int KnxTcpPort { get; private set; }
    public string MqttHost { get; private set; } = null!;
    public int MqttPort { get; private set; }
    public string SnapcastHost { get; private set; } = null!;
    public int SnapcastPort { get; private set; }
    public string AppHost { get; private set; } = null!;
    public int AppPort { get; private set; }

    public async Task InitializeAsync()
    {
        // Start all containers in parallel for faster setup
        await Task.WhenAll(StartKnxdContainer(), StartMqttContainer(), StartSnapcastContainer());

        // Start app container after dependencies are ready
        await StartAppContainer();

        // Initialize test clients
        await InitializeTestClients();

        // Wait for all services to be fully ready
        await WaitForServicesReady();
    }

    public async Task DisposeAsync()
    {
        // Dispose test clients
        KnxClient?.Dispose();
        await MqttTestClient?.DisposeAsync();
        SnapcastTestClient?.Dispose();
        ApiClient?.Dispose();

        // Stop and dispose containers
        var disposeTasks = new List<Task>();

        if (AppContainer != null)
            disposeTasks.Add(AppContainer.DisposeAsync().AsTask());
        if (SnapcastContainer != null)
            disposeTasks.Add(SnapcastContainer.DisposeAsync().AsTask());
        if (MqttContainer != null)
            disposeTasks.Add(MqttContainer.DisposeAsync().AsTask());
        if (KnxdContainer != null)
            disposeTasks.Add(KnxdContainer.DisposeAsync().AsTask());

        await Task.WhenAll(disposeTasks);
    }

    #region Container Setup

    private async Task StartKnxdContainer()
    {
        KnxdContainer = new ContainerBuilder()
            .WithImage("knxd/knxd:latest")
            .WithPortBinding(6720, true) // TCP port
            .WithPortBinding(3671, true) // ETS port
            .WithCommand(
                "knxd",
                "--eibaddr=1.1.128",
                "--client-addrs=1.1.129:8",
                "--listen-tcp=6720",
                "--listen-local=/tmp/eib",
                "--trace=7",
                "--error=7",
                "dummy:"
            )
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6720))
            .Build();

        await KnxdContainer.StartAsync();

        KnxHost = KnxdContainer.Hostname;
        KnxTcpPort = KnxdContainer.GetMappedPublicPort(6720);
    }

    private async Task StartMqttContainer()
    {
        MqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0")
            .WithPortBinding(1883, true)
            .WithResourceMapping(CreateMosquittoConfig(), "/mosquitto/config/mosquitto.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();

        await MqttContainer.StartAsync();

        MqttHost = MqttContainer.Hostname;
        MqttPort = MqttContainer.GetMappedPublicPort(1883);
    }

    private async Task StartSnapcastContainer()
    {
        SnapcastContainer = new ContainerBuilder()
            .WithImage("saiyato/snapserver:latest")
            .WithPortBinding(1704, true) // JSON-RPC port
            .WithPortBinding(1780, true) // Web UI port
            .WithEnvironment("SNAPSERVER_OPTS", "--http.enabled --http.port=1780")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1704))
            .Build();

        await SnapcastContainer.StartAsync();

        SnapcastHost = SnapcastContainer.Hostname;
        SnapcastPort = SnapcastContainer.GetMappedPublicPort(1704);
    }

    private async Task StartAppContainer()
    {
        var repoRoot = FindRepoRoot() ?? throw new InvalidOperationException("Could not locate repo root");

        AppContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/dotnet/sdk:9.0")
            .WithBindMount(repoRoot, "/app", AccessMode.ReadWrite)
            .WithWorkingDirectory("/app")
            .WithPortBinding(5000, true)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
            .WithEnvironment("SNAPDOG_API_ENABLED", "true")
            .WithEnvironment("SNAPDOG_API_PORT", "5000")
            .WithEnvironment("SNAPDOG_API_AUTH_ENABLED", "false")
            .WithEnvironment("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true")
            // KNX Configuration
            .WithEnvironment("SNAPDOG_SERVICES_KNX_ENABLED", "true")
            .WithEnvironment("SNAPDOG_SERVICES_KNX_GATEWAY", KnxHost)
            .WithEnvironment("SNAPDOG_SERVICES_KNX_PORT", KnxTcpPort.ToString())
            .WithEnvironment("SNAPDOG_SERVICES_KNX_TIMEOUT", "10")
            .WithEnvironment("SNAPDOG_SERVICES_KNX_AUTO_RECONNECT", "true")
            // MQTT Configuration
            .WithEnvironment("SNAPDOG_SERVICES_MQTT_ENABLED", "true")
            .WithEnvironment("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS", MqttHost)
            .WithEnvironment("SNAPDOG_SERVICES_MQTT_PORT", MqttPort.ToString())
            // Snapcast Configuration
            .WithEnvironment("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", SnapcastHost)
            .WithEnvironment("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", SnapcastPort.ToString())
            // Zone Configuration - align with devcontainer/.env
            .WithEnvironment("SNAPDOG_ZONE_1_NAME", "Ground Floor")
            .WithEnvironment("SNAPDOG_ZONE_1_SINK", "/snapsinks/zone1")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_ENABLED", "true")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_VOLUME", "1/2/1")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_VOLUME_STATUS", "1/2/2")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_VOLUME_UP", "1/2/3")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_VOLUME_DOWN", "1/2/4")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_PLAY", "1/1/1")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_PAUSE", "1/1/2")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_STOP", "1/1/3")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_TRACK_NEXT", "1/1/4")
            .WithEnvironment("SNAPDOG_ZONE_1_KNX_TRACK_PREVIOUS", "1/1/5")
            .WithEnvironment("SNAPDOG_ZONE_2_NAME", "1st Floor")
            .WithEnvironment("SNAPDOG_ZONE_2_SINK", "/snapsinks/zone2")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_ENABLED", "true")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_VOLUME", "2/2/1")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_VOLUME_STATUS", "2/2/2")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_VOLUME_UP", "2/2/3")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_VOLUME_DOWN", "2/2/4")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_PLAY", "2/1/1")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_PAUSE", "2/1/2")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_STOP", "2/1/3")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_TRACK_NEXT", "2/1/4")
            .WithEnvironment("SNAPDOG_ZONE_2_KNX_TRACK_PREVIOUS", "2/1/5")
            // System KNX Configuration
            .WithEnvironment("SNAPDOG_SYSTEM_KNX_STATUS", "0/0/1")
            .WithEnvironment("SNAPDOG_SYSTEM_KNX_TIME", "0/0/2")
            .WithEnvironment("SNAPDOG_SYSTEM_KNX_DATE", "0/0/3")
            .WithEnvironment("SNAPDOG_SYSTEM_KNX_SCENE_MASTER", "0/0/10")
            .WithEnvironment("SNAPDOG_SYSTEM_KNX_EMERGENCY_STOP", "0/0/99")
            .WithCommand("dotnet", "run", "--project", "/app/SnapDog2", "-c", "Debug")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5000))
            .Build();

        await AppContainer.StartAsync();

        AppHost = AppContainer.Hostname;
        AppPort = AppContainer.GetMappedPublicPort(5000);
    }

    #endregion

    #region Test Client Initialization

    private async Task InitializeTestClients()
    {
        // Initialize KNX Test Client
        KnxClient = new KnxTestClient(KnxHost, KnxTcpPort);
        await KnxClient.ConnectAsync();

        // Initialize MQTT Test Client
        MqttTestClient = new MqttTestClient(MqttHost, MqttPort);
        await MqttTestClient.ConnectAsync();

        // Initialize Snapcast Test Client
        SnapcastTestClient = new SnapcastTestClient(SnapcastHost, SnapcastPort);
        await SnapcastTestClient.ConnectAsync();

        // Initialize API Client
        ApiClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{AppHost}:{AppPort}"),
            Timeout = TimeSpan.FromSeconds(30),
        };

        // Initialize Mediator Spy (would be injected into the app)
        MediatorSpy = new MediatorSpy();
    }

    private async Task WaitForServicesReady()
    {
        // Wait for API health endpoint
        var healthReady = false;
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var response = await ApiClient.GetAsync("/api/health/ready");
                if (response.IsSuccessStatusCode)
                {
                    healthReady = true;
                    break;
                }
            }
            catch
            {
                // Continue waiting
            }
            await Task.Delay(1000);
        }

        if (!healthReady)
            throw new TimeoutException("API health endpoint not ready after 30 seconds");

        // Additional service readiness checks
        await Task.Delay(2000); // Allow all integrations to initialize
    }

    #endregion

    #region Helper Methods

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SnapDog2.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName;
    }

    private static string CreateMosquittoConfig()
    {
        return """
            listener 1883
            allow_anonymous true
            log_type all
            """;
    }

    #endregion
}

#region Test Client Implementations

/// <summary>
/// KNX test client for sending group address writes and reads
/// </summary>
public class KnxTestClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private KnxBus? _knxBus;

    public KnxTestClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        var config = new KnxBusConfiguration
        {
            ConnectionType = KnxConnectionType.Tcp,
            EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(_host), _port),
            IndividualAddress = KnxAddress.Parse("1.1.200"), // Test client address
        };

        _knxBus = new KnxBus(config);
        await _knxBus.ConnectAsync();
    }

    public async Task WriteGroupValueAsync(string groupAddress, object value)
    {
        if (_knxBus == null)
            throw new InvalidOperationException("Not connected");

        var ga = KnxGroupAddress.Parse(groupAddress);

        // Convert value based on type
        byte[] data = value switch
        {
            bool b => new[] { b ? (byte)1 : (byte)0 },
            byte b => new[] { b },
            int i when i >= 0 && i <= 255 => new[] { (byte)i },
            int i => BitConverter.GetBytes(i),
            _ => throw new ArgumentException($"Unsupported value type: {value.GetType()}"),
        };

        await _knxBus.WriteGroupValueAsync(ga, data);
    }

    public async Task<int> ReadGroupValueAsync(string groupAddress)
    {
        if (_knxBus == null)
            throw new InvalidOperationException("Not connected");

        var ga = KnxGroupAddress.Parse(groupAddress);
        var data = await _knxBus.ReadGroupValueAsync(ga);

        return data.Length switch
        {
            1 => data[0],
            2 => BitConverter.ToInt16(data),
            4 => BitConverter.ToInt32(data),
            _ => throw new InvalidOperationException($"Unexpected data length: {data.Length}"),
        };
    }

    public void Dispose()
    {
        _knxBus?.Dispose();
    }
}

/// <summary>
/// MQTT test client for publishing commands and monitoring status messages
/// </summary>
public class MqttTestClient : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private IManagedMqttClient? _client;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _messageWaiters = new();
    private readonly ConcurrentDictionary<string, string> _lastMessages = new();

    public MqttTestClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        var factory = new MqttFactory();
        _client = factory.CreateManagedMqttClient();

        _client.ApplicationMessageReceivedAsync += OnMessageReceived;

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(1))
            .WithClientOptions(
                new MqttClientOptionsBuilder().WithTcpServer(_host, _port).WithClientId("SnapDog2-TestClient").Build()
            )
            .Build();

        await _client.StartAsync(options);

        // Subscribe to all SnapDog topics
        await _client.SubscribeAsync("snapdog/+/+/+");
        await _client.SubscribeAsync("snapdog/+/+");
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _lastMessages[topic] = payload;

        if (_messageWaiters.TryRemove(topic, out var tcs))
        {
            tcs.SetResult(payload);
        }

        return Task.CompletedTask;
    }

    public async Task<string> WaitForMessage(string topic, TimeSpan timeout)
    {
        // Check if we already have the message
        if (_lastMessages.TryGetValue(topic, out var existingMessage))
        {
            return existingMessage;
        }

        // Wait for new message
        var tcs = new TaskCompletionSource<string>();
        _messageWaiters[topic] = tcs;

        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            _messageWaiters.TryRemove(topic, out _);
            throw new TimeoutException($"Timeout waiting for MQTT message on topic: {topic}");
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        if (_client == null)
            throw new InvalidOperationException("Not connected");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.EnqueueAsync(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.StopAsync();
            _client.Dispose();
        }
    }
}

/// <summary>
/// Snapcast test client for monitoring server and client status
/// </summary>
public class SnapcastTestClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly HttpClient _httpClient;

    public SnapcastTestClient(string host, int port)
    {
        _host = host;
        _port = port;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{host}:{port}"),
            Timeout = TimeSpan.FromSeconds(10),
        };
    }

    public async Task ConnectAsync()
    {
        // Test connection
        await GetServerStatus();
    }

    public async Task<SnapcastServerStatus> GetServerStatus()
    {
        var request = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Server.GetStatus",
        };

        var response = await _httpClient.PostAsync(
            "/jsonrpc",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        );

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        return JsonSerializer.Deserialize<SnapcastServerStatus>(result.GetProperty("result").GetRawText());
    }

    public async Task<SnapcastClientStatus> GetClientStatus(string clientId)
    {
        var serverStatus = await GetServerStatus();
        var client = serverStatus.Groups.SelectMany(g => g.Clients).FirstOrDefault(c => c.Id == clientId);

        return client ?? throw new ArgumentException($"Client not found: {clientId}");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Mediator spy for capturing and verifying command processing
/// </summary>
public class MediatorSpy
{
    private readonly ConcurrentQueue<object> _processedCommands = new();
    private readonly ConcurrentDictionary<Type, TaskCompletionSource<object>> _commandWaiters = new();

    public IReadOnlyCollection<object> ProcessedCommands => _processedCommands.ToArray();

    public void RecordCommand(object command)
    {
        _processedCommands.Enqueue(command);

        var commandType = command.GetType();
        if (_commandWaiters.TryRemove(commandType, out var tcs))
        {
            tcs.SetResult(command);
        }
    }

    public async Task<T> WaitForCommand<T>(TimeSpan timeout)
        where T : class
    {
        // Check if we already have the command
        var existingCommand = _processedCommands.OfType<T>().FirstOrDefault();
        if (existingCommand != null)
        {
            return existingCommand;
        }

        // Wait for new command
        var tcs = new TaskCompletionSource<object>();
        _commandWaiters[typeof(T)] = tcs;

        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            var result = await tcs.Task;
            return (T)result;
        }
        catch (OperationCanceledException)
        {
            _commandWaiters.TryRemove(typeof(T), out _);
            throw new TimeoutException($"Timeout waiting for command: {typeof(T).Name}");
        }
    }
}

#endregion

#region Supporting Types

public record SnapcastServerStatus(SnapcastGroup[] Groups);

public record SnapcastGroup(SnapcastClientStatus[] Clients);

public record SnapcastClientStatus(string Id, int Volume, bool Muted);

#endregion
