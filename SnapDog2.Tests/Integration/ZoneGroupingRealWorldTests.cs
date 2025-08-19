using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Shared;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Enterprise-grade real-world scenario tests for zone grouping functionality.
/// Tests complete workflows including fault injection, recovery, and cross-system validation.
/// </summary>
[Collection(TestCategories.Integration)]
[Trait("Category", TestCategories.Integration)]
[Trait("TestType", TestTypes.RealWorldScenario)]
[Trait("TestSpeed", TestSpeed.Slow)]
public class ZoneGroupingRealWorldTests : IClassFixture<DockerComposeTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly DockerComposeTestFixture _containersFixture;
    private readonly DockerComposeTestFixture _integrationFixture;
    private readonly HttpClient _httpClient;
    private readonly IZoneGroupingService _zoneGroupingService;

    public ZoneGroupingRealWorldTests(
        ITestOutputHelper output,
        DockerComposeTestFixture containersFixture,
        DockerComposeTestFixture integrationFixture
    )
    {
        _output = output;
        _containersFixture = containersFixture;
        _integrationFixture = integrationFixture;
        _httpClient = _integrationFixture.HttpClient;
        _zoneGroupingService = _integrationFixture.ServiceProvider.GetRequiredService<IZoneGroupingService>();
    }

    [Fact]
    [TestPriority(1)]
    public async Task Scenario_01_InitialState_ShouldHaveCorrectZoneGrouping()
    {
        // Arrange
        _output.WriteLine("🧪 Testing initial zone grouping state");

        // Act - Get zone grouping status
        var response = await _httpClient.GetAsync("/api/zone-grouping/status");
        response.Should().BeSuccessful();

        var status = await response.Content.ReadFromJsonAsync<ZoneGroupingStatus>();

        // Assert - Initial state should be healthy
        status.Should().NotBeNull();
        status!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);
        status.TotalZones.Should().Be(2);
        status.HealthyZones.Should().Be(2);
        status.UnhealthyZones.Should().Be(0);
        status.TotalClients.Should().Be(3);
        status.CorrectlyGroupedClients.Should().Be(3);
        status.Issues.Should().BeEmpty();

        // Verify zone details
        var zone1 = status.ZoneDetails.FirstOrDefault(z => z.ZoneId == 1);
        zone1.Should().NotBeNull();
        zone1!.ZoneName.Should().Be("Ground Floor");
        zone1.ExpectedClients.Should().HaveCount(2); // Living Room + Kitchen
        zone1.Health.Should().Be(ZoneGroupingHealth.Healthy);

        var zone2 = status.ZoneDetails.FirstOrDefault(z => z.ZoneId == 2);
        zone2.Should().NotBeNull();
        zone2!.ZoneName.Should().Be("1st Floor");
        zone2.ExpectedClients.Should().HaveCount(1); // Bedroom
        zone2.Health.Should().Be(ZoneGroupingHealth.Healthy);

        _output.WriteLine("✅ Initial state validation passed");
    }

    [Fact]
    [TestPriority(2)]
    public async Task Scenario_02_ValidationAPI_ShouldConfirmConsistency()
    {
        // Arrange
        _output.WriteLine("🧪 Testing zone grouping validation API");

        // Act
        var response = await _httpClient.GetAsync("/api/zone-grouping/validate");
        response.Should().BeSuccessful();

        var result = await response.Content.ReadAsStringAsync();
        var validationResult = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        validationResult.GetProperty("status").GetString().Should().Be("valid");
        validationResult.GetProperty("message").GetString().Should().Be("Zone grouping is consistent");

        _output.WriteLine("✅ Validation API test passed");
    }

    [Fact]
    [TestPriority(3)]
    public async Task Scenario_03_FaultInjection_BreakGroupingViaSnapcastAPI()
    {
        // Arrange
        _output.WriteLine("🧪 Testing fault injection - breaking grouping via direct Snapcast API");

        // First, get current groups to identify target group IDs
        var currentGroups = await GetSnapcastGroupsAsync();
        currentGroups.Should().HaveCount(2);

        var zone1Group = currentGroups.First(g => g.Clients.Count == 2);
        var zone2Group = currentGroups.First(g => g.Clients.Count == 1);

        // Act - Break grouping by moving kitchen to bedroom's group
        await _output.MeasureAsync(
            "Breaking grouping via Snapcast API",
            async () =>
            {
                var breakCommand = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "Group.SetClients",
                    @params = new { id = zone2Group.Id, clients = new[] { "bedroom", "kitchen" } },
                };

                var result = await SendSnapcastCommandAsync(breakCommand);
                result.Should().NotBeNull();
            }
        );

        // Assert - Verify broken state
        var brokenGroups = await GetSnapcastGroupsAsync();
        var livingRoomGroup = brokenGroups.FirstOrDefault(g => g.Clients.Any(c => c.Id == "living-room"));
        var kitchenBedroomGroup = brokenGroups.FirstOrDefault(g =>
            g.Clients.Any(c => c.Id == "kitchen") && g.Clients.Any(c => c.Id == "bedroom")
        );

        livingRoomGroup.Should().NotBeNull();
        livingRoomGroup!.Clients.Should().HaveCount(1);
        livingRoomGroup.Clients.First().Id.Should().Be("living-room");

        kitchenBedroomGroup.Should().NotBeNull();
        kitchenBedroomGroup!.Clients.Should().HaveCount(2);
        kitchenBedroomGroup.Clients.Should().Contain(c => c.Id == "kitchen");
        kitchenBedroomGroup.Clients.Should().Contain(c => c.Id == "bedroom");

        _output.WriteLine("✅ Fault injection successful - grouping broken as expected");
    }

    [Fact]
    [TestPriority(4)]
    public async Task Scenario_04_ProblemDetection_ShouldDetectBrokenGrouping()
    {
        // Arrange
        _output.WriteLine("🧪 Testing problem detection after fault injection");

        // Act - Check validation API
        var validationResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");
        validationResponse.Should().BeSuccessful();

        var validationResult = await validationResponse.Content.ReadAsStringAsync();
        var validation = JsonSerializer.Deserialize<JsonElement>(validationResult);

        // Assert - Should detect the problem
        validation.GetProperty("status").GetString().Should().Be("invalid");
        validation.GetProperty("message").GetString().Should().Contain("inconsistencies");

        // Get detailed status
        var statusResponse = await _httpClient.GetAsync("/api/zone-grouping/status");
        statusResponse.Should().BeSuccessful();

        var status = await statusResponse.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        status.Should().NotBeNull();
        status!.OverallHealth.Should().Be(ZoneGroupingHealth.Degraded);
        status.UnhealthyZones.Should().BeGreaterThan(0);
        status.Issues.Should().NotBeEmpty();

        _output.WriteLine("✅ Problem detection working correctly");
    }

    [Fact]
    [TestPriority(5)]
    public async Task Scenario_05_AutomaticRecovery_ShouldFixBrokenGroupingAutomatically()
    {
        // Arrange
        _output.WriteLine("🧪 Testing automatic recovery - no manual intervention needed");

        // Wait for automatic correction (background service monitors every 30 seconds)
        _output.WriteLine("⏳ Waiting for automatic correction...");

        var corrected = false;
        var maxWaitTime = TimeSpan.FromSeconds(45);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime && !corrected)
        {
            await Task.Delay(5000); // Check every 5 seconds

            var validationResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");
            var validationResult = await validationResponse.Content.ReadAsStringAsync();
            var validation = JsonSerializer.Deserialize<JsonElement>(validationResult);

            if (validation.GetProperty("status").GetString() == "valid")
            {
                corrected = true;
                _output.WriteLine(
                    $"✅ Automatic correction detected after {(DateTime.UtcNow - startTime).TotalSeconds:F1} seconds"
                );
            }
        }

        // Assert - Should be automatically corrected
        corrected.Should().BeTrue("Background service should automatically correct grouping issues");

        // Verify recovery
        var recoveredGroups = await GetSnapcastGroupsAsync();
        var zone1Group = recoveredGroups.FirstOrDefault(g =>
            g.Clients.Any(c => c.Id == "living-room") && g.Clients.Any(c => c.Id == "kitchen")
        );
        var zone2Group = recoveredGroups.FirstOrDefault(g =>
            g.Clients.Count == 1 && g.Clients.Any(c => c.Id == "bedroom")
        );

        zone1Group.Should().NotBeNull();
        zone1Group!.Clients.Should().HaveCount(2);

        zone2Group.Should().NotBeNull();
        zone2Group!.Clients.Should().HaveCount(1);

        _output.WriteLine("✅ Automatic recovery working perfectly");
    }

    [Fact]
    [TestPriority(6)]
    public async Task Scenario_06_ComplexBrokenState_ShouldHandleAutomatically()
    {
        // Arrange
        _output.WriteLine("🧪 Testing automatic handling of complex broken state");

        // Create complex broken state - all clients in one group
        var groups = await GetSnapcastGroupsAsync();
        var targetGroup = groups.First();

        var complexBreakCommand = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Group.SetClients",
            @params = new { id = targetGroup.Id, clients = new[] { "living-room", "kitchen", "bedroom" } },
        };

        await SendSnapcastCommandAsync(complexBreakCommand);

        // Verify broken state
        var brokenGroups = await GetSnapcastGroupsAsync();
        var allInOneGroup = brokenGroups.FirstOrDefault(g => g.Clients.Count == 3);
        allInOneGroup.Should().NotBeNull();

        _output.WriteLine("🔧 Created complex broken state - all clients in one group");

        // Act - Wait for automatic correction
        _output.WriteLine("⏳ Waiting for automatic correction of complex state...");

        var corrected = false;
        var maxWaitTime = TimeSpan.FromSeconds(60); // Allow more time for complex correction
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime && !corrected)
        {
            await Task.Delay(5000);

            var validationResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");
            var validationResult = await validationResponse.Content.ReadAsStringAsync();
            var validation = JsonSerializer.Deserialize<JsonElement>(validationResult);

            if (validation.GetProperty("status").GetString() == "valid")
            {
                corrected = true;
                _output.WriteLine(
                    $"✅ Complex state automatically corrected after {(DateTime.UtcNow - startTime).TotalSeconds:F1} seconds"
                );
            }
        }

        // Assert - Should be automatically corrected
        corrected.Should().BeTrue("Background service should automatically correct even complex broken states");

        // Verify final state
        var finalGroups = await GetSnapcastGroupsAsync();
        finalGroups.Should().HaveCount(2);

        var finalZone1Group = finalGroups.FirstOrDefault(g =>
            g.Clients.Any(c => c.Id == "living-room") && g.Clients.Any(c => c.Id == "kitchen")
        );
        var finalZone2Group = finalGroups.FirstOrDefault(g =>
            g.Clients.Count == 1 && g.Clients.Any(c => c.Id == "bedroom")
        );

        finalZone1Group.Should().NotBeNull();
        finalZone2Group.Should().NotBeNull();

        _output.WriteLine("✅ Complex broken state handled automatically");
    }

    [Fact]
    [TestPriority(7)]
    public async Task Scenario_07_CrossSystemValidation_ShouldMaintainConsistency()
    {
        // Arrange
        _output.WriteLine("🧪 Testing cross-system validation between SnapDog and Snapcast");

        // Act - Get state from both systems
        var snapDogStatus = await _httpClient.GetAsync("/api/zone-grouping/status");
        snapDogStatus.Should().BeSuccessful();
        var snapDogState = await snapDogStatus.Content.ReadFromJsonAsync<ZoneGroupingStatus>();

        var snapcastGroups = await GetSnapcastGroupsAsync();

        // Assert - States should be consistent
        snapDogState.Should().NotBeNull();
        snapDogState!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);

        // Verify client distribution matches
        var snapDogClientCount = snapDogState.ZoneDetails.Sum(z => z.ExpectedClients.Count);
        var snapcastClientCount = snapcastGroups.Sum(g => g.Clients.Count);

        snapDogClientCount.Should().Be(snapcastClientCount);
        snapDogClientCount.Should().Be(3);

        // Verify zone grouping consistency
        foreach (var zoneDetail in snapDogState.ZoneDetails)
        {
            if (zoneDetail.ExpectedClients.Count > 1)
            {
                // Multi-client zone should have all clients in same group
                var groupIds = zoneDetail.ExpectedClients.Select(c => c.CurrentGroupId).Distinct().ToList();
                groupIds.Should().HaveCount(1, $"Zone {zoneDetail.ZoneId} clients should be in same group");
            }
        }

        _output.WriteLine("✅ Cross-system validation passed");
    }

    [Fact]
    [TestPriority(8)]
    public async Task Scenario_08_PerformanceUnderLoad_ShouldHandleMultipleOperations()
    {
        // Arrange
        _output.WriteLine("🧪 Testing performance under load with multiple monitoring operations");

        var tasks = new List<Task>();

        // Act - Execute multiple monitoring operations concurrently
        await _output.MeasureAsync(
            "Concurrent monitoring operations",
            async () =>
            {
                // Multiple status checks
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(_httpClient.GetAsync("/api/zone-grouping/status"));
                }

                // Multiple validation checks
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(_httpClient.GetAsync("/api/zone-grouping/validate"));
                }

                await Task.WhenAll(tasks);
            }
        );

        // Assert - All operations should succeed
        foreach (var task in tasks.Cast<Task<HttpResponseMessage>>())
        {
            var response = await task;
            response.Should().BeSuccessful();
        }

        // Verify system is still healthy after load
        var finalStatus = await _httpClient.GetAsync("/api/zone-grouping/status");
        finalStatus.Should().BeSuccessful();

        var finalState = await finalStatus.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        finalState.Should().NotBeNull();
        finalState!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);

        _output.WriteLine("✅ Performance under monitoring load test passed");
    }

    #region Helper Methods

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
        public bool Connected { get; set; }
    }

    #endregion
}
