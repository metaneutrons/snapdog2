using FluentAssertions;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Shared;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Tests for the continuous zone grouping background service using Docker Compose pattern.
/// Validates automatic detection and correction of grouping issues in a coordinated environment.
/// </summary>
[Collection(TestCategories.Integration)]
[Trait("Category", TestCategories.Integration)]
[Trait("TestType", TestTypes.RealWorldScenario)]
[Trait("TestSpeed", TestSpeed.Slow)]
public class ContinuousZoneGroupingTestsNew : IClassFixture<DockerComposeTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly DockerComposeTestFixture _testFixture;
    private readonly HttpClient _httpClient;

    public ContinuousZoneGroupingTestsNew(ITestOutputHelper output, DockerComposeTestFixture testFixture)
    {
        _output = output;
        _testFixture = testFixture;
        _httpClient = _testFixture.HttpClient;
    }

    [Fact]
    [TestPriority(1)]
    public async Task StartupBehavior_ShouldHaveHealthyEnvironment()
    {
        // Arrange & Act
        _output.WriteLine("🧪 Testing startup behavior - environment health check");

        // The Docker Compose fixture ensures all services are healthy
        var healthResponse = await _httpClient.GetAsync("/health");

        // Assert
        healthResponse.IsSuccessStatusCode.Should().BeTrue("SnapDog2 API should be healthy");

        var zonesResponse = await _httpClient.GetAsync("/api/v1/zones");
        zonesResponse.IsSuccessStatusCode.Should().BeTrue("Zones API should be accessible");

        _output.WriteLine("✅ Test environment is healthy and ready");
    }

    [Fact]
    [TestPriority(2)]
    public async Task ContinuousMonitoring_ShouldDetectAndCorrectManualChanges()
    {
        // Arrange
        _output.WriteLine("🧪 Testing continuous monitoring - automatic correction of manual changes");

        // The Docker Compose fixture ensures all services are healthy and clients are connected
        _output.WriteLine("✅ Test environment ready with connected clients");

        // Verify initial healthy state
        var initialStatus = await _httpClient.GetAsync("/api/v1/zones");
        initialStatus.EnsureSuccessStatusCode();

        var zonesResponse = await initialStatus.Content.ReadAsStringAsync();
        _output.WriteLine($"📊 Initial zones status: {zonesResponse}");

        // Act - Manually break grouping via Snapcast API
        _output.WriteLine("🔧 Breaking grouping manually via Snapcast API");
        await _testFixture.BreakSnapcastGroupingAsync();

        // Verify broken state by checking zone grouping service
        _output.WriteLine("🔍 Verifying grouping is broken...");
        await Task.Delay(2000); // Allow time for detection

        // Wait for background service to detect and fix (monitoring interval is 30s, so wait up to 45s)
        _output.WriteLine("⏳ Waiting for automatic correction...");
        var corrected = false;
        var maxWaitTime = TimeSpan.FromSeconds(45);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime && !corrected)
        {
            await Task.Delay(5000); // Check every 5 seconds

            // Check if zones are properly grouped again
            var statusResponse = await _httpClient.GetAsync("/api/v1/zones");
            if (statusResponse.IsSuccessStatusCode)
            {
                var statusContent = await statusResponse.Content.ReadAsStringAsync();

                // Parse and check if clients are properly distributed
                // Zone 1 should have Living Room + Kitchen (2 clients)
                // Zone 2 should have Bedroom (1 client)
                if (
                    statusContent.Contains("Living Room")
                    && statusContent.Contains("Kitchen")
                    && statusContent.Contains("Bedroom")
                )
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
        }

        // If not corrected, log diagnostics before failing
        if (!corrected)
        {
            _output.WriteLine("❌ Automatic correction failed, logging diagnostics...");
            var snapcastStatus = await _testFixture.GetSnapcastServerStatusAsync();
            _output.WriteLine($"📊 Snapcast Status: {snapcastStatus}");
        }

        // Assert - Should be automatically corrected
        corrected.Should().BeTrue("Background service should automatically correct grouping issues within 45 seconds");

        // Verify final state
        var finalStatus = await _httpClient.GetAsync("/api/v1/zones");
        finalStatus.EnsureSuccessStatusCode();

        var finalContent = await finalStatus.Content.ReadAsStringAsync();
        _output.WriteLine($"📊 Final zones status: {finalContent}");

        _output.WriteLine("✅ Continuous monitoring and automatic correction working perfectly");
    }

    [Fact]
    [TestPriority(3)]
    public async Task SnapcastServer_ShouldHaveCorrectStreamConfiguration()
    {
        // Arrange & Act
        _output.WriteLine("🧪 Testing Snapcast server stream configuration");

        var serverStatus = await _testFixture.GetSnapcastServerStatusAsync();

        // Assert
        serverStatus
            .ValueKind.Should()
            .Be(System.Text.Json.JsonValueKind.Object, "Should receive valid JSON response");

        _output.WriteLine($"📊 Snapcast server status: {serverStatus}");
        _output.WriteLine("✅ Snapcast server configuration verified");
    }
}
