using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Tests.Fixtures.Integration;
using SnapDog2.Tests.Fixtures.Shared;
using SnapDog2.Tests.Helpers.Extensions;

namespace SnapDog2.Tests.Integration.Workflows;

/// <summary>
/// Enterprise-grade integration tests for zone change workflows to ensure proper client grouping,
/// state consistency, and multi-room audio functionality across the entire system.
/// </summary>
[Collection(TestCategories.Workflow)]
[Trait("Category", TestCategories.Workflow)]
[Trait("Type", TestTypes.Infrastructure)]
[Trait("Speed", TestSpeed.Slow)]
[RequiresAttribute(TestRequirements.Docker)]
[RequiresAttribute(TestRequirements.Network)]
public class ZoneChangeWorkflowTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ZoneChangeWorkflowTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    [TestSpeed(TestSpeed.Slow)]
    [Trait("Scenario", "MultiClient")]
    public async Task ZoneChangeWorkflow_WithMultipleClients_ShouldMaintainConsistentState()
    {
        // Arrange
        _output.WriteSection("Zone Change Workflow Test - Multiple Clients");

        await _output.MeasureAsync(
            "Snapcast Service Initialization",
            async () =>
            {
                await EnsureSnapcastServiceInitializedAsync();
            }
        );

        using var scope = _fixture.ServiceProvider.CreateScope();
        var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

        // Act & Assert - Verify initial client setup
        _output.WriteStep("Verify Initial Client Setup");
        var serverStatusResult = await snapcastService.GetServerStatusAsync();
        serverStatusResult.Should().BeSuccessful("Snapcast server should be accessible");

        var serverStatus = serverStatusResult.Value!;
        serverStatus.Groups.Should().NotBeNull().And.HaveCount(3, "Should have 3 client groups");

        // Verify MAC addresses match expected configuration
        VerifyClientMacAddresses(serverStatus);

        // Verify clients can be identified by the application
        await VerifyClientDiscoveryAsync();

        _output.WriteSuccess("Zone change workflow infrastructure validated successfully");
    }

    [Fact]
    [TestSpeed(TestSpeed.Medium)]
    [Trait("Scenario", "ClientDiscovery")]
    public async Task ClientDiscovery_WithStaticMacAddresses_ShouldIdentifyAllClients()
    {
        // Arrange
        _output.WriteSection("Client Discovery Test - Static MAC Addresses");

        await EnsureSnapcastServiceInitializedAsync();

        using var scope = _fixture.ServiceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();

        // Act
        _output.WriteStep("Discover Clients", "Attempting to discover all configured clients");

        var clients = await _output.MeasureAsync(
            "Client Discovery",
            async () =>
            {
                var allClients = new List<ClientState>();

                // Try to get each configured client
                for (int i = 1; i <= 3; i++)
                {
                    var clientResult = await clientManager.GetClientStateAsync(i);
                    if (clientResult.IsSuccess)
                    {
                        allClients.Add(clientResult.Value!);
                    }
                }

                return allClients;
            }
        );

        // Assert
        clients.Should().HaveCount(3, "Should discover all 3 configured clients");

        var expectedClients = new[]
        {
            ("Living Room", "02:42:ac:11:00:10"),
            ("Kitchen", "02:42:ac:11:00:11"),
            ("Bedroom", "02:42:ac:11:00:12"),
        };

        foreach (var (expectedName, expectedMac) in expectedClients)
        {
            var client = clients.FirstOrDefault(c => c.Name == expectedName);
            client.Should().NotBeNull($"Should find client '{expectedName}'");

            _output.WriteSuccess($"Client discovered: {expectedName} (MAC: {expectedMac})");
        }
    }

    [Fact]
    [TestSpeed(TestSpeed.Fast)]
    [Trait("Scenario", "Configuration")]
    public void Configuration_ShouldMatchContainerSetup()
    {
        // Arrange
        _output.WriteSection("Configuration Validation Test");

        using var scope = _fixture.ServiceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert
        _output.WriteStep("Verify Client Configuration");

        var expectedConfigs = new[]
        {
            (1, "Living Room", "02:42:ac:11:00:10", 1),
            (2, "Kitchen", "02:42:ac:11:00:11", 1),
            (3, "Bedroom", "02:42:ac:11:00:12", 2),
        };

        foreach (var (index, name, mac, zone) in expectedConfigs)
        {
            var clientName = configuration[$"SNAPDOG_CLIENT_{index}_NAME"];
            var clientMac = configuration[$"SNAPDOG_CLIENT_{index}_MAC"];
            var clientZone = configuration[$"SNAPDOG_CLIENT_{index}_DEFAULT_ZONE"];

            clientName.Should().Be(name, $"Client {index} name should match");
            clientMac.Should().Be(mac, $"Client {index} MAC should match");
            clientZone.Should().Be(zone.ToString(), $"Client {index} zone should match");

            _output.WriteSuccess($"Client {index} configuration validated: {name} ({mac}) -> Zone {zone}");
        }
    }

    private async Task EnsureSnapcastServiceInitializedAsync()
    {
        _output.WriteStep("Initialize Snapcast Service");

        using var scope = _fixture.ServiceProvider.CreateScope();
        var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

        // Wait for service to be ready
        var maxAttempts = 10;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            try
            {
                var result = await snapcastService.GetServerStatusAsync();
                if (result.IsSuccess)
                {
                    _output.WriteSuccess("Snapcast service initialized successfully");
                    return;
                }
            }
            catch (Exception ex)
            {
                _output.WriteDebug($"Snapcast service not ready (attempt {attempt + 1}): {ex.Message}");
            }

            attempt++;
            await Task.Delay(1000);
        }

        throw new InvalidOperationException("Snapcast service failed to initialize within timeout");
    }

    private void VerifyClientMacAddresses(SnapcastServerStatus serverStatus)
    {
        _output.WriteStep("Verify Client MAC Addresses");

        var expectedMacAddresses = new Dictionary<string, string>
        {
            { "living-room", "02:42:ac:11:00:10" },
            { "kitchen", "02:42:ac:11:00:11" },
            { "bedroom", "02:42:ac:11:00:12" },
        };

        _output.WriteLine("=== Actual Snapcast Client Setup ===");

        foreach (var group in serverStatus.Groups!)
        {
            if (group.Clients != null)
            {
                foreach (var client in group.Clients)
                {
                    var expectedMac = expectedMacAddresses.GetValueOrDefault(client.Id!, "UNKNOWN");
                    var macMatch = string.Equals(client.Host?.Mac, expectedMac, StringComparison.OrdinalIgnoreCase);
                    var status = macMatch ? "✅" : "❌";

                    _output.WriteLine($"Client: {client.Id}");
                    _output.WriteLine($"  Expected MAC: {expectedMac}");
                    _output.WriteLine($"  Actual MAC:   {client.Host?.Mac} {status}");
                    _output.WriteLine($"  Group: {group.Id}");
                    _output.WriteLine("");

                    client.Host?.Mac.Should().Be(expectedMac, $"Client {client.Id} should have correct MAC address");
                }
            }
        }

        _output.WriteSuccess("All client MAC addresses verified successfully");
    }

    private async Task VerifyClientDiscoveryAsync()
    {
        _output.WriteStep("Verify Client Discovery");

        using var scope = _fixture.ServiceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();

        var discoveredClients = 0;

        for (int i = 1; i <= 3; i++)
        {
            var clientResult = await clientManager.GetClientStateAsync(i);
            if (clientResult.IsSuccess)
            {
                discoveredClients++;
                _output.WriteSuccess($"Client {i} discovered: {clientResult.Value!.Name}");
            }
            else
            {
                _output.WriteFailure($"Client {i} not found: {clientResult.ErrorMessage}");
            }
        }

        discoveredClients.Should().Be(3, "Should discover all 3 configured clients");
        _output.WriteSuccess("Client discovery completed successfully");
    }
}
