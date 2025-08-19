using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Cortex.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using SnapDog2.Tests.Fixtures.Shared;

namespace SnapDog2.Tests.Fixtures.Containers;

/// <summary>
/// Enhanced Docker Compose-based test fixture that mirrors the proven devcontainer setup.
/// Uses single coordinated environment instead of multiple conflicting containers.
/// Includes all methods and properties from the old fixtures for complete compatibility.
/// </summary>
public class DockerComposeTestFixture : IAsyncLifetime
{
    private readonly string _projectName = $"snapdog-test-{Guid.NewGuid():N}"[..12];
    private WebApplicationFactory<Program>? _factory;
    private IMqttClient? _testMqttClient;

    public string ApiBaseUrl { get; private set; } = "http://localhost:5001";
    public HttpClient HttpClient { get; private set; } = null!;

    // Properties from old IntegrationTestFixture
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public string MqttBrokerHost { get; private set; } = "localhost";
    public int MqttBrokerPort { get; private set; } = 1883;
    public string SnapcastHost { get; private set; } = "localhost";
    public int SnapcastJsonRpcPort { get; private set; } = 1705;
    public int SnapcastHttpPort { get; private set; } = 1780;

    public async Task InitializeAsync()
    {
        Console.WriteLine("🚀 Starting Docker Compose test environment...");

        try
        {
            // Start the test environment using docker-compose
            await StartDockerComposeAsync();

            // Wait for services to be healthy
            await WaitForServicesHealthyAsync();

            // Create web application factory with proper configuration
            await CreateWebApplicationFactoryAsync();

            // Initialize test clients (MQTT, etc.)
            await InitializeTestClientsAsync();

            Console.WriteLine("✅ Docker Compose test environment ready");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize Docker Compose test environment: {ex.Message}");
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("🧹 Cleaning up Docker Compose test environment...");

        try
        {
            // Dispose test clients
            if (_testMqttClient != null)
            {
                if (_testMqttClient.IsConnected)
                {
                    await _testMqttClient.DisconnectAsync();
                }
                _testMqttClient.Dispose();
            }

            // Dispose HTTP client and factory
            HttpClient?.Dispose();
            _factory?.Dispose();

            // Always stop Docker Compose on disposal
            await StopDockerComposeAsync();

            Console.WriteLine("✅ Docker Compose test environment cleaned up");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error during cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Manually break Snapcast grouping for testing automatic correction.
    /// </summary>
    public async Task BreakSnapcastGroupingAsync()
    {
        Console.WriteLine("🔧 Breaking Snapcast grouping manually...");

        // Get current server status
        var serverStatus = await GetSnapcastServerStatusAsync();

        // Find groups
        var groups = ExtractGroupsFromStatus(serverStatus);
        if (groups.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 groups to break grouping");
        }

        // Move kitchen client to bedroom's group (breaking Zone 1 grouping)
        var bedroomGroup = groups.FirstOrDefault(g => g.Clients.Any(c => c.Id.Contains("bedroom")));

        if (bedroomGroup == null)
        {
            throw new InvalidOperationException("Could not find bedroom group");
        }

        var breakCommand = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Group.SetClients",
            @params = new { id = bedroomGroup.Id, clients = new[] { "bedroom", "kitchen" } },
        };

        await SendSnapcastCommandAsync(breakCommand);
        Console.WriteLine("✅ Snapcast grouping broken successfully");
    }

    /// <summary>
    /// Get current Snapcast server status for validation.
    /// </summary>
    public async Task<JsonElement> GetSnapcastServerStatusAsync()
    {
        var command = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Server.GetStatus",
        };

        return await SendSnapcastCommandAsync(command);
    }

    // Methods from old IntegrationTestFixture
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
        try
        {
            // Use the KNX service from the application's DI container
            using var scope = ServiceProvider.CreateScope();
            var knxService = scope.ServiceProvider.GetRequiredService<IKnxService>();

            // Ensure KNX service is initialized
            if (!knxService.IsConnected)
            {
                var initResult = await knxService.InitializeAsync();
                if (!initResult.IsSuccess)
                {
                    return Result.Failure($"Failed to initialize KNX service: {initResult.ErrorMessage}");
                }
            }

            // Send the KNX command
            var result = await knxService.WriteGroupValueAsync(groupAddress, value);
            return result;
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
        var service = ServiceProvider.GetService<T>();
        Console.WriteLine($"Checking service {typeof(T).Name}: {(service != null ? "Found" : "Not Found")}");
        service.Should().NotBeNull($"{typeof(T).Name} should be registered in DI container");
    }

    public void AssertConfigurationIsValid()
    {
        using var scope = ServiceProvider.CreateScope();
        var snapDogConfig = scope.ServiceProvider.GetRequiredService<IOptions<SnapDogConfiguration>>().Value;

        // Verify MQTT configuration
        snapDogConfig.Services.Mqtt.Enabled.Should().BeTrue("MQTT should be enabled in tests");
        snapDogConfig
            .Services.Mqtt.BrokerAddress.Should()
            .Be("mqtt-test", "MQTT broker should use test container address");
        snapDogConfig.Services.Mqtt.Port.Should().Be(1883, "MQTT should use test container port");

        // Verify KNX configuration (enabled in tests with knxd-test)
        snapDogConfig.Services.Knx.Enabled.Should().BeTrue("KNX should be enabled in tests");
        snapDogConfig.Services.Knx.Gateway.Should().Be("knxd-test", "KNX gateway should use test container address");

        // Verify API configuration
        snapDogConfig.Api.Enabled.Should().BeTrue("API should be enabled in tests");
        snapDogConfig.Api.Port.Should().Be(5000, "API should be on port 5000 in tests");
        snapDogConfig.Api.AuthEnabled.Should().BeFalse("API auth should be disabled in tests");

        // Verify system configuration
        snapDogConfig.System.HealthChecksEnabled.Should().BeTrue("Health checks should be enabled in tests");
        snapDogConfig.System.LogLevel.Should().Be("Information", "Log level should be Information in tests");
    }

    private async Task StartDockerComposeAsync()
    {
        // Find the project root directory (where docker-compose.test.yml is located)
        var projectRoot = FindProjectRoot();
        Console.WriteLine($"🔍 Using project root: {projectRoot}");

        // Try new location first, then fallback to old location
        var composeFilePath = Path.Combine(
            projectRoot,
            "SnapDog2.Tests",
            "TestData",
            "Docker",
            "docker-compose.test.yml"
        );
        if (!File.Exists(composeFilePath))
        {
            composeFilePath = Path.Combine(projectRoot, "docker-compose.test.yml");
        }

        var relativePath = Path.GetRelativePath(projectRoot, composeFilePath);
        Console.WriteLine($"📄 Using compose file: {relativePath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f {relativePath} -p {_projectName} up -d", // Removed --build to prevent hanging
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = projectRoot,
        };

        Console.WriteLine($"🐳 Starting Docker Compose with command: docker {startInfo.Arguments}");
        Console.WriteLine($"📁 Working directory: {startInfo.WorkingDirectory}");

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start docker compose");
        }

        // Add timeout to prevent hanging
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        var processTask = process.WaitForExitAsync();

        var completedTask = await Task.WhenAny(processTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Console.WriteLine("⏰ Docker Compose command timed out, killing process...");
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to kill process: {ex.Message}");
            }
            throw new TimeoutException("Docker Compose command timed out after 5 minutes");
        }

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            Console.WriteLine($"❌ Docker Compose failed with exit code {process.ExitCode}");
            Console.WriteLine($"📄 Output: {output}");
            Console.WriteLine($"🚨 Error: {error}");
            throw new InvalidOperationException($"Docker compose failed: {error}");
        }

        var successOutput = await process.StandardOutput.ReadToEndAsync();
        Console.WriteLine($"📄 Docker Compose output: {successOutput}");
        Console.WriteLine("✅ Docker Compose services started");

        // Ensure all containers are actually started (workaround for dependency chain issues)
        await EnsureAllContainersStartedAsync();
    }

    private async Task EnsureAllContainersStartedAsync()
    {
        Console.WriteLine("🔄 Ensuring all containers are started...");

        var containerNames = new[]
        {
            $"{_projectName}-app-test-1",
            $"{_projectName}-snapcast-client-living-room-test-1",
            $"{_projectName}-snapcast-client-kitchen-test-1",
            $"{_projectName}-snapcast-client-bedroom-test-1",
        };

        foreach (var containerName in containerNames)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"start {containerName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"✅ Started container: {containerName}");
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Console.WriteLine($"⚠️ Failed to start container {containerName}: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error starting container {containerName}: {ex.Message}");
            }
        }

        Console.WriteLine("🔄 Container startup check completed");
    }

    private async Task StopDockerComposeAsync()
    {
        // Find the project root directory (where docker-compose.test.yml is located)
        var projectRoot = FindProjectRoot();

        // Try new location first, then fallback to old location
        var composeFilePath = Path.Combine(
            projectRoot,
            "SnapDog2.Tests",
            "TestData",
            "Docker",
            "docker-compose.test.yml"
        );
        if (!File.Exists(composeFilePath))
        {
            composeFilePath = Path.Combine(projectRoot, "docker-compose.test.yml");
        }

        var relativePath = Path.GetRelativePath(projectRoot, composeFilePath);

        var stopInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f {relativePath} -p {_projectName} down -v",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = projectRoot,
        };

        using var process = Process.Start(stopInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    private string FindProjectRoot()
    {
        // Start from the current directory and walk up to find the project root
        var currentDir = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDir);

        while (directory != null)
        {
            // Look for docker-compose.test.yml file in TestData/Docker directory (new location)
            var testDataDockerPath = Path.Combine(
                directory.FullName,
                "SnapDog2.Tests",
                "TestData",
                "Docker",
                "docker-compose.test.yml"
            );
            if (File.Exists(testDataDockerPath))
            {
                return directory.FullName;
            }

            // Fallback: Look for docker-compose.test.yml file in root (old location)
            if (File.Exists(Path.Combine(directory.FullName, "docker-compose.test.yml")))
            {
                return directory.FullName;
            }

            // Also look for .git directory or solution file as indicators of project root
            if (
                Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || Directory.GetFiles(directory.FullName, "*.sln").Length > 0
            )
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        // Fallback to current directory if not found
        throw new InvalidOperationException("Could not find project root directory containing docker-compose.test.yml");
    }

    private async Task WaitForServicesHealthyAsync()
    {
        Console.WriteLine("⏳ Waiting for services to become healthy...");

        var maxWaitTime = TimeSpan.FromSeconds(120); // Increased timeout
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            try
            {
                // Check if SnapDog2 API is responding
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync($"{ApiBaseUrl}/health");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ SnapDog2 API is healthy");

                    // Give a bit more time for Snapcast clients to connect
                    await Task.Delay(5000);

                    // Try to check Snapcast clients, but don't fail if they're not ready yet
                    try
                    {
                        var connectedClients = await GetConnectedClientCountAsync();
                        Console.WriteLine($"📊 {connectedClients} Snapcast clients connected");

                        if (connectedClients >= 3)
                        {
                            Console.WriteLine($"✅ All {connectedClients} Snapcast clients connected");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Could not check Snapcast clients: {ex.Message}");
                    }

                    // Return success if API is healthy, regardless of Snapcast client status
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⏳ Services not ready yet: {ex.Message}");
            }

            await Task.Delay(2000);
        }

        throw new TimeoutException("Services did not become healthy within timeout period");
    }

    private Task CreateWebApplicationFactoryAsync()
    {
        Console.WriteLine("🏭 Creating web application factory...");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            // Configure services to ensure environment variables are loaded
            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    config.Sources.Clear();
                    config.AddEnvironmentVariables();
                    config.AddEnvironmentVariables("SNAPDOG_");
                }
            );

            // Set environment variables for test configuration
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", "localhost");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", "1705");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_HTTP_PORT", "1780");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ENABLED", "true");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_ENABLED", "false"); // Disable MQTT for WebApplicationFactory tests
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_ENABLED", "true");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_GATEWAY", "localhost");
            Environment.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_PORT", "3671");
            Environment.SetEnvironmentVariable("SNAPDOG_API_ENABLED", "true");
            Environment.SetEnvironmentVariable("SNAPDOG_API_PORT", "5000");
            Environment.SetEnvironmentVariable("SNAPDOG_API_AUTH_ENABLED", "false");
            Environment.SetEnvironmentVariable("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true");
            Environment.SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_LEVEL", "Information");

            // Zone configuration
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_NAME", "Ground Floor");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_1_SINK", "/snapsinks/zone1");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_NAME", "1st Floor");
            Environment.SetEnvironmentVariable("SNAPDOG_ZONE_2_SINK", "/snapsinks/zone2");

            // Client configuration
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_NAME", "Living Room");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_MAC", "02:42:ac:11:00:10");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_1_DEFAULT_ZONE", "1");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_NAME", "Kitchen");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_MAC", "02:42:ac:11:00:11");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_2_DEFAULT_ZONE", "1");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_NAME", "Bedroom");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_MAC", "02:42:ac:11:00:12");
            Environment.SetEnvironmentVariable("SNAPDOG_CLIENT_3_DEFAULT_ZONE", "2");

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        });

        HttpClient = _factory.CreateClient();
        ServiceProvider = _factory.Services;

        Console.WriteLine("✅ Web application factory created");
        return Task.CompletedTask;
    }

    private async Task InitializeTestClientsAsync()
    {
        Console.WriteLine("🔧 Initializing test clients...");

        // Initialize MQTT test client
        try
        {
            var factory = new MqttClientFactory();
            _testMqttClient = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883) // Use localhost since we're connecting from host
                .WithClientId("test-client")
                .WithCredentials("snapdog", "snapdog")
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithCleanSession(true);

            var options = optionsBuilder.Build();
            await _testMqttClient.ConnectAsync(options);

            Console.WriteLine("✅ MQTT test client connected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ MQTT test client initialization failed: {ex.Message}");
        }

        Console.WriteLine("✅ Test clients initialized");
    }

    private async Task<int> GetConnectedClientCountAsync()
    {
        try
        {
            // Use SnapDog2 API to get client status instead of direct Snapcast connection
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{ApiBaseUrl}/api/v1/clients");

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            var clients = JsonSerializer.Deserialize<JsonElement[]>(json);

            return clients?.Count(c => c.GetProperty("connected").GetBoolean()) ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<JsonElement> SendSnapcastCommandAsync(object command)
    {
        var json = JsonSerializer.Serialize(command);
        Console.WriteLine($"🔍 Sending JSON-RPC command: {json}");

        // Use bare TCP connection like SnapcastClient does (no HTTP)
        using var tcpClient = new TcpClient();

        // Add connection timeout to prevent hanging
        var connectTask = tcpClient.ConnectAsync("localhost", 1705);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(connectTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("TCP connection to Snapcast server timed out after 10 seconds");
        }

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Send raw JSON-RPC over TCP (no HTTP headers)
        await writer.WriteLineAsync(json);
        await writer.FlushAsync();

        // Read the JSON-RPC response with timeout
        var readTask = reader.ReadLineAsync();
        var readTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
        var readCompletedTask = await Task.WhenAny(readTask, readTimeoutTask);

        if (readCompletedTask == readTimeoutTask)
        {
            throw new TimeoutException("Reading Snapcast server response timed out after 5 seconds");
        }

        var responseJson = await readTask;
        Console.WriteLine($"🔍 Response content: {responseJson}");

        if (string.IsNullOrEmpty(responseJson))
        {
            throw new InvalidOperationException("Empty response from Snapcast server");
        }

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

        // Check for JSON-RPC errors
        if (jsonResponse.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
            var errorData = error.TryGetProperty("data", out var data) ? data.GetString() : "";
            throw new InvalidOperationException($"JSON-RPC error: {errorMessage}. Data: {errorData}");
        }

        return jsonResponse;
    }

    private static List<SnapcastGroup> ExtractGroupsFromStatus(JsonElement status)
    {
        var groups = new List<SnapcastGroup>();

        if (
            status.TryGetProperty("result", out var result)
            && result.TryGetProperty("server", out var server)
            && server.TryGetProperty("groups", out var groupsArray)
        )
        {
            foreach (var groupElement in groupsArray.EnumerateArray())
            {
                var group = new SnapcastGroup
                {
                    Id = groupElement.GetProperty("id").GetString() ?? "",
                    Clients = new List<SnapcastClient>(),
                };

                if (groupElement.TryGetProperty("clients", out var clientsArray))
                {
                    foreach (var clientElement in clientsArray.EnumerateArray())
                    {
                        var client = new SnapcastClient
                        {
                            Id = clientElement.GetProperty("id").GetString() ?? "",
                            Connected = clientElement.GetProperty("connected").GetBoolean(),
                        };
                        group.Clients.Add(client);
                    }
                }

                groups.Add(group);
            }
        }

        return groups;
    }

    private record SnapcastGroup
    {
        public required string Id { get; init; }
        public required List<SnapcastClient> Clients { get; init; }
    }

    private record SnapcastClient
    {
        public required string Id { get; init; }
        public required bool Connected { get; init; }
    }
}
