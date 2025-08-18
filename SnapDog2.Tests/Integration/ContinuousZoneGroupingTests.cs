using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Integration;
using SnapDog2.Tests.Fixtures.Shared;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Tests for the continuous zone grouping background service.
/// Validates automatic detection and correction of grouping issues.
/// </summary>
[Collection(TestCategories.Integration)]
[Trait("Category", TestCategories.Integration)]
[Trait("TestType", TestTypes.RealWorldScenario)]
[Trait("TestSpeed", TestSpeed.Slow)]
public class ContinuousZoneGroupingTests : IClassFixture<TestcontainersFixture>, IClassFixture<IntegrationTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly TestcontainersFixture _containersFixture;
    private readonly IntegrationTestFixture _integrationFixture;
    private readonly HttpClient _httpClient;

    public ContinuousZoneGroupingTests(
        ITestOutputHelper output,
        TestcontainersFixture containersFixture,
        IntegrationTestFixture integrationFixture
    )
    {
        _output = output;
        _containersFixture = containersFixture;
        _integrationFixture = integrationFixture;
        _httpClient = _integrationFixture.HttpClient;
    }

    [Fact]
    [TestPriority(1)]
    public async Task StartupBehavior_ShouldPerformInitialReconciliation()
    {
        // Arrange
        _output.WriteLine("🧪 Testing startup behavior - initial reconciliation");

        // Wait a moment for background service to complete startup
        await Task.Delay(2000);

        // Act - Check initial state
        var response = await _httpClient.GetAsync("/api/zone-grouping/status");
        response.Should().BeSuccessful();

        var status = await response.Content.ReadFromJsonAsync<ZoneGroupingStatus>();

        // Assert - Should be properly grouped after startup
        status.Should().NotBeNull();
        status!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);
        status.TotalZones.Should().Be(2);
        status.HealthyZones.Should().Be(2);
        status.UnhealthyZones.Should().Be(0);
        status.TotalClients.Should().Be(3);
        status.CorrectlyGroupedClients.Should().Be(3);

        _output.WriteLine("✅ Startup reconciliation completed successfully");
    }

    [Fact]
    [TestPriority(2)]
    public async Task ContinuousMonitoring_ShouldDetectAndCorrectManualChanges()
    {
        // Arrange
        _output.WriteLine("🧪 Testing continuous monitoring - automatic correction of manual changes");

        // First ensure we start in a good state
        var initialResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");
        initialResponse.Should().BeSuccessful();

        // Act - Manually break grouping via Snapcast API
        _output.WriteLine("🔧 Breaking grouping manually via Snapcast API");
        await BreakGroupingManually();

        // Verify broken state
        var brokenValidation = await _httpClient.GetAsync("/api/zone-grouping/validate");
        var brokenResult = await brokenValidation.Content.ReadAsStringAsync();
        var brokenJson = JsonSerializer.Deserialize<JsonElement>(brokenResult);
        brokenJson.GetProperty("status").GetString().Should().Be("invalid");

        _output.WriteLine("⚠️ Grouping successfully broken, waiting for automatic correction...");

        // Wait for background service to detect and fix (monitoring interval is 30s, so wait up to 45s)
        var corrected = false;
        var maxWaitTime = TimeSpan.FromSeconds(45);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime && !corrected)
        {
            await Task.Delay(5000); // Check every 5 seconds

            var validationResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");
            var validationResult = await validationResponse.Content.ReadAsStringAsync();
            var validationJson = JsonSerializer.Deserialize<JsonElement>(validationResult);

            if (validationJson.GetProperty("status").GetString() == "valid")
            {
                corrected = true;
                _output.WriteLine(
                    $"✅ Automatic correction detected after {(DateTime.UtcNow - startTime).TotalSeconds:F1} seconds"
                );
            }
            else
            {
                _output.WriteLine(
                    $"⏳ Still waiting for correction... ({(DateTime.UtcNow - startTime).TotalSeconds:F1}s elapsed)"
                );
            }
        }

        // Assert - Should be automatically corrected
        corrected.Should().BeTrue("Background service should automatically correct grouping issues within 45 seconds");

        // Verify final state
        var finalStatus = await _httpClient.GetAsync("/api/zone-grouping/status");
        var finalState = await finalStatus.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        finalState!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);
        finalState.CorrectlyGroupedClients.Should().Be(3);

        _output.WriteLine("✅ Continuous monitoring and automatic correction working perfectly");
    }

    [Fact]
    [TestPriority(3)]
    public async Task EdgeCase_MultipleManualChanges_ShouldHandleGracefully()
    {
        // Arrange
        _output.WriteLine("🧪 Testing edge case - multiple rapid manual changes");

        // Act - Make multiple rapid changes
        for (int i = 0; i < 3; i++)
        {
            _output.WriteLine($"🔧 Making manual change #{i + 1}");
            await BreakGroupingManually();
            await Task.Delay(2000); // Small delay between changes
        }

        // Wait for stabilization
        _output.WriteLine("⏳ Waiting for system to stabilize after multiple changes...");
        await Task.Delay(50000); // Wait longer for multiple corrections

        // Assert - Should eventually stabilize
        var finalValidation = await _httpClient.GetAsync("/api/zone-grouping/validate");
        var finalResult = await finalValidation.Content.ReadAsStringAsync();
        var finalJson = JsonSerializer.Deserialize<JsonElement>(finalResult);
        finalJson.GetProperty("status").GetString().Should().Be("valid");

        _output.WriteLine("✅ System handled multiple rapid changes gracefully");
    }

    [Fact]
    [TestPriority(4)]
    public async Task MonitoringEndpoints_ShouldProvideAccurateStatus()
    {
        // Arrange
        _output.WriteLine("🧪 Testing monitoring endpoints accuracy");

        // Act - Get status and validation
        var statusResponse = await _httpClient.GetAsync("/api/zone-grouping/status");
        var validationResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");

        // Assert - Both should be successful and consistent
        statusResponse.Should().BeSuccessful();
        validationResponse.Should().BeSuccessful();

        var status = await statusResponse.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        var validationResult = await validationResponse.Content.ReadAsStringAsync();
        var validation = JsonSerializer.Deserialize<JsonElement>(validationResult);

        // Status and validation should be consistent
        if (status!.OverallHealth == ZoneGroupingHealth.Healthy)
        {
            validation.GetProperty("status").GetString().Should().Be("valid");
        }
        else
        {
            validation.GetProperty("status").GetString().Should().Be("invalid");
        }

        _output.WriteLine("✅ Monitoring endpoints providing accurate status");
    }

    [Fact]
    [TestPriority(5)]
    public async Task ClientNameSynchronization_ShouldHappenAutomatically()
    {
        // Arrange
        _output.WriteLine("🧪 Testing automatic client name synchronization");

        // Act - Check current client names via Snapcast
        var groups = await GetSnapcastGroupsAsync();
        var allClients = groups.SelectMany(g => g.Clients).ToList();

        // Assert - Client names should be friendly names, not MAC addresses
        foreach (var client in allClients)
        {
            client.Name.Should().NotBeNullOrEmpty();
            client
                .Name.Should()
                .NotMatchRegex(
                    @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
                    "Client names should be friendly names, not MAC addresses"
                );
        }

        // Verify expected client names
        var clientNames = allClients.Select(c => c.Name.ToLower()).ToList();
        clientNames.Should().Contain("living room");
        clientNames.Should().Contain("kitchen");
        clientNames.Should().Contain("bedroom");

        _output.WriteLine("✅ Client name synchronization working automatically");
    }

    #region Helper Methods

    private async Task BreakGroupingManually()
    {
        // Get current groups
        var groups = await GetSnapcastGroupsAsync();
        if (groups.Count < 2)
            return;

        // Move kitchen client to bedroom's group (breaking Zone 1 grouping)
        var bedroomGroup = groups.FirstOrDefault(g => g.Clients.Any(c => c.Id.Contains("bedroom")));
        if (bedroomGroup == null)
            return;

        var breakCommand = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Group.SetClients",
            @params = new { id = bedroomGroup.Id, clients = new[] { "bedroom", "kitchen" } },
        };

        await SendSnapcastCommandAsync(breakCommand);
    }

    private async Task<List<SimpleSnapcastGroup>> GetSnapcastGroupsAsync()
    {
        var command = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Server.GetStatus",
        };

        var result = await SendSnapcastCommandAsync(command);
        var groups = new List<SimpleSnapcastGroup>();

        if (
            result?.TryGetProperty("result", out var resultProp) == true
            && resultProp.TryGetProperty("server", out var serverProp)
            && serverProp.TryGetProperty("groups", out var groupsProp)
        )
        {
            foreach (var groupElement in groupsProp.EnumerateArray())
            {
                var clients = new List<SimpleSnapcastClient>();

                if (groupElement.TryGetProperty("clients", out var clientsProp))
                {
                    foreach (var clientElement in clientsProp.EnumerateArray())
                    {
                        var client = new SimpleSnapcastClient
                        {
                            Id = clientElement.GetProperty("id").GetString() ?? "",
                            Name =
                                clientElement.TryGetProperty("config", out var configProp)
                                && configProp.TryGetProperty("name", out var nameProp)
                                    ? nameProp.GetString() ?? ""
                                    : "",
                            Connected = clientElement.GetProperty("connected").GetBoolean(),
                        };
                        clients.Add(client);
                    }
                }

                var group = new SimpleSnapcastGroup
                {
                    Id = groupElement.GetProperty("id").GetString() ?? "",
                    Clients = clients,
                };
                groups.Add(group);
            }
        }

        return groups;
    }

    private async Task<JsonElement?> SendSnapcastCommandAsync(object command)
    {
        var json = JsonSerializer.Serialize(command);
        var tcpClient = new System.Net.Sockets.TcpClient();

        try
        {
            await tcpClient.ConnectAsync("localhost", 1705);
            var stream = tcpClient.GetStream();
            var writer = new StreamWriter(stream);
            var reader = new StreamReader(stream);

            await writer.WriteLineAsync(json);
            await writer.FlushAsync();

            var response = await reader.ReadLineAsync();
            if (response != null)
            {
                return JsonSerializer.Deserialize<JsonElement>(response);
            }
        }
        finally
        {
            tcpClient.Close();
        }

        return null;
    }

    #endregion

    #region Helper Classes

    private class SimpleSnapcastGroup
    {
        public string Id { get; set; } = "";
        public List<SimpleSnapcastClient> Clients { get; set; } = new();
    }

    private class SimpleSnapcastClient
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Connected { get; set; }
    }

    #endregion
}
