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
/// Enterprise-grade fault injection tests for zone grouping resilience.
/// Systematically tests system behavior under various failure conditions.
/// </summary>
[Collection(TestCategories.Integration)]
[Trait("Category", TestCategories.Integration)]
[Trait("TestType", TestTypes.FaultInjection)]
[Trait("TestSpeed", TestSpeed.Slow)]
[Trait("TestRequirement", TestRequirements.Container)]
public class FaultInjectionTests : IClassFixture<DockerComposeTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly DockerComposeTestFixture _containersFixture;
    private readonly DockerComposeTestFixture _integrationFixture;
    private readonly HttpClient _httpClient;
    private readonly IZoneGroupingService _zoneGroupingService;

    public FaultInjectionTests(
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

    [Theory]
    [InlineData("living-room", "kitchen", "Split Zone 1 clients")]
    [InlineData("bedroom", "living-room", "Cross-zone contamination")]
    [InlineData("kitchen", "bedroom", "Reverse zone assignment")]
    public async Task FaultInjection_ClientMovement_ShouldDetectAndRecover(
        string sourceClient,
        string targetClient,
        string scenario
    )
    {
        // Arrange
        _output.WriteLine($"🧪 Fault Injection: {scenario}");

        // Get initial state
        var initialGroups = await GetSnapcastGroupsAsync();
        var targetGroup = initialGroups.First(g => g.Clients.Any(c => c.Id == targetClient));
        var sourceGroup = initialGroups.First(g => g.Clients.Any(c => c.Id == sourceClient));

        // Act - Inject fault by moving client
        await _output.MeasureAsync(
            $"Injecting fault: {scenario}",
            async () =>
            {
                var faultCommand = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "Group.SetClients",
                    @params = new
                    {
                        id = targetGroup.Id,
                        clients = targetGroup.Clients.Select(c => c.Id).Concat(new[] { sourceClient }).ToArray(),
                    },
                };

                await SendSnapcastCommandAsync(faultCommand);
            }
        );

        // Assert - System should detect the fault
        var validationResponse = await _httpClient.GetAsync("/api/zone-grouping/validate");
        validationResponse.Should().BeSuccessful();

        var validationResult = await validationResponse.Content.ReadAsStringAsync();
        var validation = JsonSerializer.Deserialize<JsonElement>(validationResult);

        if (ShouldDetectAsProblem(sourceClient, targetClient))
        {
            validation.GetProperty("status").GetString().Should().Be("invalid");
            _output.WriteLine($"✅ Fault correctly detected: {scenario}");
        }
        else
        {
            validation.GetProperty("status").GetString().Should().Be("valid");
            _output.WriteLine($"✅ Fault correctly ignored (valid scenario): {scenario}");
        }

        // Recovery - Always attempt recovery to restore clean state
        var recoveryResponse = await _httpClient.PostAsync("/api/zone-grouping/reconcile", null);
        recoveryResponse.Should().BeSuccessful();

        _output.WriteLine($"✅ Recovery completed for: {scenario}");
    }

    [Fact]
    public async Task FaultInjection_AllClientsInOneGroup_ShouldHandleGracefully()
    {
        // Arrange
        _output.WriteLine("🧪 Fault Injection: All clients in one group (whole-house audio scenario)");

        var groups = await GetSnapcastGroupsAsync();
        var targetGroup = groups.First();
        var allClientIds = groups.SelectMany(g => g.Clients.Select(c => c.Id)).ToArray();

        // Act - Move all clients to one group
        await _output.MeasureAsync(
            "Creating whole-house audio scenario",
            async () =>
            {
                var wholeHouseCommand = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "Group.SetClients",
                    @params = new { id = targetGroup.Id, clients = allClientIds },
                };

                await SendSnapcastCommandAsync(wholeHouseCommand);
            }
        );

        // Assert - System should handle this gracefully (it's a valid use case)
        var statusResponse = await _httpClient.GetAsync("/api/zone-grouping/status");
        statusResponse.Should().BeSuccessful();

        var status = await statusResponse.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        status.Should().NotBeNull();

        // This should be healthy because:
        // - Zone 1 clients (living-room + kitchen) are together ✅
        // - Zone 2 client (bedroom) is with them, but that's not unhealthy ✅
        // - System can recover if needed ✅
        status!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);

        _output.WriteLine("✅ Whole-house audio scenario handled correctly");
    }

    [Fact]
    public async Task FaultInjection_EmptyGroups_ShouldHandleGracefully()
    {
        // Arrange
        _output.WriteLine("🧪 Fault Injection: Creating empty groups");

        var groups = await GetSnapcastGroupsAsync();
        var sourceGroup = groups.First(g => g.Clients.Count > 0);
        var targetGroup = groups.First(g => g.Id != sourceGroup.Id);

        // Act - Move all clients from one group to another, leaving empty group
        await _output.MeasureAsync(
            "Creating empty group scenario",
            async () =>
            {
                var allClients = groups.SelectMany(g => g.Clients.Select(c => c.Id)).ToArray();

                var emptyGroupCommand = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "Group.SetClients",
                    @params = new { id = targetGroup.Id, clients = allClients },
                };

                await SendSnapcastCommandAsync(emptyGroupCommand);
            }
        );

        // Assert - System should handle empty groups gracefully
        var statusResponse = await _httpClient.GetAsync("/api/zone-grouping/status");
        statusResponse.Should().BeSuccessful();

        var status = await statusResponse.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        status.Should().NotBeNull();
        status!.TotalClients.Should().Be(3); // All clients still accounted for

        // Recovery
        var recoveryResponse = await _httpClient.PostAsync("/api/zone-grouping/reconcile", null);
        recoveryResponse.Should().BeSuccessful();

        _output.WriteLine("✅ Empty group scenario handled correctly");
    }

    [Fact]
    public async Task FaultInjection_RapidStateChanges_ShouldMaintainConsistency()
    {
        // Arrange
        _output.WriteLine("🧪 Fault Injection: Rapid state changes");

        var groups = await GetSnapcastGroupsAsync();
        var commands = new List<object>();

        // Create multiple rapid state changes
        for (int i = 0; i < 5; i++)
        {
            var targetGroup = groups[i % groups.Count];
            var clients = i % 2 == 0 ? new[] { "living-room", "kitchen" } : new[] { "bedroom" };

            commands.Add(
                new
                {
                    id = i + 1,
                    jsonrpc = "2.0",
                    method = "Group.SetClients",
                    @params = new { id = targetGroup.Id, clients = clients },
                }
            );
        }

        // Act - Execute rapid changes
        await _output.MeasureAsync(
            "Executing rapid state changes",
            async () =>
            {
                var tasks = commands.Select(cmd => SendSnapcastCommandAsync(cmd));
                await Task.WhenAll(tasks);
            }
        );

        // Small delay to allow system to process
        await Task.Delay(1000);

        // Assert - System should maintain consistency
        var statusResponse = await _httpClient.GetAsync("/api/zone-grouping/status");
        statusResponse.Should().BeSuccessful();

        var status = await statusResponse.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        status.Should().NotBeNull();
        status!.TotalClients.Should().Be(3);

        // Recovery
        var recoveryResponse = await _httpClient.PostAsync("/api/zone-grouping/reconcile", null);
        recoveryResponse.Should().BeSuccessful();

        _output.WriteLine("✅ Rapid state changes handled with consistency maintained");
    }

    [Theory]
    [InlineData(1, "Zone 1 synchronization under stress")]
    [InlineData(2, "Zone 2 synchronization under stress")]
    public async Task FaultInjection_ZoneSynchronizationUnderStress_ShouldSucceed(int zoneId, string scenario)
    {
        // Arrange
        _output.WriteLine($"🧪 Fault Injection: {scenario}");

        // Create stressed state by rapid group changes
        var groups = await GetSnapcastGroupsAsync();
        for (int i = 0; i < 3; i++)
        {
            var stressCommand = new
            {
                id = i + 1,
                jsonrpc = "2.0",
                method = "Group.SetClients",
                @params = new
                {
                    id = groups[i % groups.Count].Id,
                    clients = new[] { "living-room", "kitchen", "bedroom" },
                },
            };
            await SendSnapcastCommandAsync(stressCommand);
        }

        // Act - Attempt zone synchronization under stress
        var syncResponse = await _output.MeasureAsync(
            $"Zone {zoneId} sync under stress",
            async () =>
            {
                return await _httpClient.PostAsync($"/api/zone-grouping/zones/{zoneId}/synchronize", null);
            }
        );

        // Assert - Synchronization should succeed despite stress
        syncResponse.Should().BeSuccessful();

        var result = await syncResponse.Content.ReadAsStringAsync();
        var syncResult = JsonSerializer.Deserialize<JsonElement>(result);
        syncResult.GetProperty("message").GetString().Should().Contain("synchronized successfully");

        _output.WriteLine($"✅ {scenario} completed successfully");
    }

    [Fact]
    public async Task FaultInjection_ConcurrentRecoveryOperations_ShouldHandleGracefully()
    {
        // Arrange
        _output.WriteLine("🧪 Fault Injection: Concurrent recovery operations");

        // Create broken state
        var groups = await GetSnapcastGroupsAsync();
        var breakCommand = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Group.SetClients",
            @params = new { id = groups.First().Id, clients = new[] { "living-room", "kitchen", "bedroom" } },
        };
        await SendSnapcastCommandAsync(breakCommand);

        // Act - Execute multiple concurrent recovery operations
        var recoveryTasks = new List<Task<HttpResponseMessage>>();

        await _output.MeasureAsync(
            "Concurrent recovery operations",
            async () =>
            {
                // Multiple reconciliation attempts
                for (int i = 0; i < 3; i++)
                {
                    recoveryTasks.Add(_httpClient.PostAsync("/api/zone-grouping/reconcile", null));
                }

                // Zone-specific synchronizations
                recoveryTasks.Add(_httpClient.PostAsync("/api/zone-grouping/zones/1/synchronize", null));
                recoveryTasks.Add(_httpClient.PostAsync("/api/zone-grouping/zones/2/synchronize", null));

                await Task.WhenAll(recoveryTasks);
            }
        );

        // Assert - All operations should succeed or handle gracefully
        foreach (var task in recoveryTasks)
        {
            var response = await task;
            response.Should().BeSuccessful();
        }

        // Verify final state is consistent
        var finalStatus = await _httpClient.GetAsync("/api/zone-grouping/status");
        finalStatus.Should().BeSuccessful();

        var status = await finalStatus.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
        status.Should().NotBeNull();
        status!.OverallHealth.Should().Be(ZoneGroupingHealth.Healthy);

        _output.WriteLine("✅ Concurrent recovery operations handled gracefully");
    }

    #region Helper Methods

    private bool ShouldDetectAsProblem(string sourceClient, string targetClient)
    {
        // Define which client movements should be detected as problems
        var zone1Clients = new[] { "living-room", "kitchen" };
        var zone2Clients = new[] { "bedroom" };

        var sourceZone = zone1Clients.Contains(sourceClient) ? 1 : 2;
        var targetZone = zone1Clients.Contains(targetClient) ? 1 : 2;

        // Problem: Moving a client away from its zone partners
        if (sourceZone == 1 && targetZone == 2)
        {
            // Moving Zone 1 client to Zone 2 - this splits Zone 1
            return true;
        }

        if (sourceZone == 2 && targetZone == 1)
        {
            // Moving Zone 2 client to Zone 1 - this might be OK (whole-house audio)
            return false;
        }

        return false;
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
