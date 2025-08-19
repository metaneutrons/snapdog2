using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Shared;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Performance;

/// <summary>
/// Enterprise-grade performance tests for zone grouping under various load conditions.
/// Tests system behavior, response times, and resource usage under stress.
/// </summary>
[Collection(TestCategories.Performance)]
[Trait("Category", TestCategories.Performance)]
[Trait("TestType", TestTypes.Performance)]
[Trait("TestSpeed", TestSpeed.Slow)]
[Trait("TestRequirement", TestRequirements.Container)]
public class ZoneGroupingPerformanceTests : IClassFixture<DockerComposeTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly DockerComposeTestFixture _containersFixture;
    private readonly DockerComposeTestFixture _integrationFixture;
    private readonly HttpClient _httpClient;
    private readonly IZoneGroupingService _zoneGroupingService;

    public ZoneGroupingPerformanceTests(
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
    public async Task Performance_StatusAPI_ShouldMeetResponseTimeTargets()
    {
        // Arrange
        _output.WriteLine("🚀 Performance Test: Zone Grouping Status API Response Times");
        const int iterations = 100;
        var responseTimes = new List<TimeSpan>();

        // Act - Measure response times over multiple iterations
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await _httpClient.GetAsync("/api/zone-grouping/status");
            response.Should().BeSuccessful();

            var status = await response.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
            status.Should().NotBeNull();

            stopwatch.Stop();
            responseTimes.Add(stopwatch.Elapsed);
        }

        // Assert - Performance targets
        var averageTime = TimeSpan.FromTicks((long)responseTimes.Select(t => t.Ticks).Average());
        var p95Time = responseTimes.OrderBy(t => t).Skip((int)(iterations * 0.95)).First();
        var maxTime = responseTimes.Max();

        _output.WriteLine($"📊 Status API Performance Results:");
        _output.WriteLine($"   Average: {averageTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"   95th percentile: {p95Time.TotalMilliseconds:F2}ms");
        _output.WriteLine($"   Maximum: {maxTime.TotalMilliseconds:F2}ms");

        // Performance targets
        averageTime.Should().BeLessThan(TimeSpan.FromMilliseconds(100), "Average response time should be under 100ms");
        p95Time.Should().BeLessThan(TimeSpan.FromMilliseconds(500), "95th percentile should be under 500ms");
        maxTime.Should().BeLessThan(TimeSpan.FromSeconds(2), "Maximum response time should be under 2 seconds");

        _output.WriteLine("✅ Status API performance targets met");
    }

    [Fact]
    public async Task Performance_ConcurrentStatusRequests_ShouldHandleLoad()
    {
        // Arrange
        _output.WriteLine("🚀 Performance Test: Concurrent Status Requests");
        const int concurrentRequests = 50;
        const int requestsPerClient = 10;

        var results = new ConcurrentBag<(bool Success, TimeSpan Duration)>();
        var semaphore = new SemaphoreSlim(concurrentRequests);

        // Act - Execute concurrent requests
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable
            .Range(0, concurrentRequests)
            .Select(async clientId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    for (int i = 0; i < requestsPerClient; i++)
                    {
                        var requestStopwatch = Stopwatch.StartNew();
                        try
                        {
                            var response = await _httpClient.GetAsync("/api/zone-grouping/status");
                            var success = response.IsSuccessStatusCode;

                            if (success)
                            {
                                var status = await response.Content.ReadFromJsonAsync<ZoneGroupingStatus>();
                                success = status != null;
                            }

                            requestStopwatch.Stop();
                            results.Add((success, requestStopwatch.Elapsed));
                        }
                        catch
                        {
                            requestStopwatch.Stop();
                            results.Add((false, requestStopwatch.Elapsed));
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Analyze results
        var totalRequests = concurrentRequests * requestsPerClient;
        var successfulRequests = results.Count(r => r.Success);
        var failedRequests = results.Count(r => !r.Success);
        var successRate = (double)successfulRequests / totalRequests * 100;
        var averageResponseTime = TimeSpan.FromTicks(
            (long)results.Where(r => r.Success).Select(r => r.Duration.Ticks).Average()
        );
        var throughput = totalRequests / stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"📊 Concurrent Load Test Results:");
        _output.WriteLine($"   Total requests: {totalRequests}");
        _output.WriteLine($"   Successful: {successfulRequests}");
        _output.WriteLine($"   Failed: {failedRequests}");
        _output.WriteLine($"   Success rate: {successRate:F2}%");
        _output.WriteLine($"   Average response time: {averageResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"   Throughput: {throughput:F2} requests/second");
        _output.WriteLine($"   Total duration: {stopwatch.Elapsed.TotalSeconds:F2}s");

        // Performance assertions
        successRate.Should().BeGreaterThan(95, "Success rate should be above 95%");
        averageResponseTime
            .Should()
            .BeLessThan(TimeSpan.FromSeconds(1), "Average response time should be under 1 second under load");
        throughput.Should().BeGreaterThan(10, "Throughput should be at least 10 requests/second");

        _output.WriteLine("✅ Concurrent load test passed");
    }

    [Fact]
    public async Task Performance_ReconciliationUnderLoad_ShouldMaintainPerformance()
    {
        // Arrange
        _output.WriteLine("🚀 Performance Test: Reconciliation Under Load");
        const int reconciliationIterations = 20;
        var reconciliationTimes = new List<TimeSpan>();

        // Act - Perform multiple reconciliations while measuring performance
        for (int i = 0; i < reconciliationIterations; i++)
        {
            // Create some chaos first
            await CreateRandomGroupingStateAsync();

            // Measure reconciliation time
            var stopwatch = Stopwatch.StartNew();

            var response = await _httpClient.PostAsync("/api/zone-grouping/reconcile", null);
            response.Should().BeSuccessful();

            var result = await response.Content.ReadFromJsonAsync<ZoneGroupingReconciliationResult>();
            result.Should().NotBeNull();

            stopwatch.Stop();
            reconciliationTimes.Add(stopwatch.Elapsed);

            _output.WriteLine($"   Reconciliation {i + 1}: {stopwatch.ElapsedMilliseconds}ms");
        }

        // Assert - Performance analysis
        var averageTime = TimeSpan.FromTicks((long)reconciliationTimes.Select(t => t.Ticks).Average());
        var maxTime = reconciliationTimes.Max();
        var minTime = reconciliationTimes.Min();

        _output.WriteLine($"📊 Reconciliation Performance Results:");
        _output.WriteLine($"   Average: {averageTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"   Minimum: {minTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"   Maximum: {maxTime.TotalMilliseconds:F2}ms");

        // Performance targets for reconciliation
        averageTime.Should().BeLessThan(TimeSpan.FromSeconds(2), "Average reconciliation should be under 2 seconds");
        maxTime.Should().BeLessThan(TimeSpan.FromSeconds(5), "Maximum reconciliation should be under 5 seconds");

        _output.WriteLine("✅ Reconciliation performance test passed");
    }

    [Fact]
    public async Task Performance_MixedWorkload_ShouldHandleRealisticUsage()
    {
        // Arrange
        _output.WriteLine("🚀 Performance Test: Mixed Workload (Realistic Usage)");
        const int testDurationSeconds = 30;
        const int statusCheckInterval = 100; // ms
        const int reconciliationInterval = 5000; // ms
        const int validationInterval = 1000; // ms

        var results = new ConcurrentBag<(string Operation, bool Success, TimeSpan Duration)>();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(testDurationSeconds));

        // Act - Run mixed workload
        var tasks = new List<Task>
        {
            // Continuous status checking (monitoring)
            RunContinuousOperationAsync(
                "StatusCheck",
                statusCheckInterval,
                async () => await _httpClient.GetAsync("/api/zone-grouping/status"),
                results,
                cancellationTokenSource.Token
            ),
            // Periodic validation
            RunContinuousOperationAsync(
                "Validation",
                validationInterval,
                async () => await _httpClient.GetAsync("/api/zone-grouping/validate"),
                results,
                cancellationTokenSource.Token
            ),
            // Occasional reconciliation
            RunContinuousOperationAsync(
                "Reconciliation",
                reconciliationInterval,
                async () => await _httpClient.PostAsync("/api/zone-grouping/reconcile", null),
                results,
                cancellationTokenSource.Token
            ),
            // Zone synchronization
            RunContinuousOperationAsync(
                "ZoneSync",
                3000,
                async () => await _httpClient.PostAsync("/api/zone-grouping/zones/1/synchronize", null),
                results,
                cancellationTokenSource.Token
            ),
        };

        await Task.WhenAll(tasks);

        // Assert - Analyze mixed workload results
        var operationGroups = results.GroupBy(r => r.Operation).ToList();

        _output.WriteLine($"📊 Mixed Workload Results ({testDurationSeconds}s):");

        foreach (var group in operationGroups)
        {
            var operations = group.ToList();
            var successCount = operations.Count(o => o.Success);
            var totalCount = operations.Count;
            var successRate = (double)successCount / totalCount * 100;
            var avgDuration = TimeSpan.FromTicks(
                (long)operations.Where(o => o.Success).Select(o => o.Duration.Ticks).Average()
            );

            _output.WriteLine($"   {group.Key}:");
            _output.WriteLine($"     Total: {totalCount}, Success: {successCount} ({successRate:F1}%)");
            _output.WriteLine($"     Avg Duration: {avgDuration.TotalMilliseconds:F2}ms");

            // All operations should have high success rates
            successRate.Should().BeGreaterThan(90, $"{group.Key} should have >90% success rate");
        }

        _output.WriteLine("✅ Mixed workload test passed");
    }

    [Fact]
    public async Task Performance_MemoryUsage_ShouldRemainStable()
    {
        // Arrange
        _output.WriteLine("🚀 Performance Test: Memory Usage Stability");
        const int iterations = 100;

        var initialMemory = GC.GetTotalMemory(true);
        var memoryReadings = new List<long>();

        // Act - Perform operations while monitoring memory
        for (int i = 0; i < iterations; i++)
        {
            // Perform various operations
            await _httpClient.GetAsync("/api/zone-grouping/status");
            await _httpClient.GetAsync("/api/zone-grouping/validate");

            if (i % 10 == 0)
            {
                await _httpClient.PostAsync("/api/zone-grouping/reconcile", null);
            }

            // Record memory usage every 10 iterations
            if (i % 10 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                memoryReadings.Add(currentMemory);
                _output.WriteLine($"   Iteration {i}: {currentMemory / 1024 / 1024:F2} MB");
            }
        }

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - Memory should remain stable
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePercent = (double)memoryIncrease / initialMemory * 100;

        _output.WriteLine($"📊 Memory Usage Results:");
        _output.WriteLine($"   Initial: {initialMemory / 1024 / 1024:F2} MB");
        _output.WriteLine($"   Final: {finalMemory / 1024 / 1024:F2} MB");
        _output.WriteLine($"   Increase: {memoryIncrease / 1024 / 1024:F2} MB ({memoryIncreasePercent:F2}%)");

        // Memory increase should be reasonable (less than 50% increase)
        memoryIncreasePercent.Should().BeLessThan(50, "Memory usage should not increase by more than 50%");

        _output.WriteLine("✅ Memory usage stability test passed");
    }

    #region Helper Methods

    private async Task CreateRandomGroupingStateAsync()
    {
        var random = new Random();
        var clients = new[] { "living-room", "kitchen", "bedroom" };

        // Randomly distribute clients across groups
        var command = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "Group.SetClients",
            @params = new
            {
                id = "08fee2d1-49c2-1806-dbf2-07c0f14ef951", // Use first group
                clients = random.Next(2) == 0
                    ? new[] { clients[random.Next(clients.Length)] }
                    : clients.OrderBy(x => random.Next()).Take(random.Next(1, 4)).ToArray(),
            },
        };

        await SendSnapcastCommandAsync(command);
    }

    private async Task RunContinuousOperationAsync(
        string operationName,
        int intervalMs,
        Func<Task<HttpResponseMessage>> operation,
        ConcurrentBag<(string Operation, bool Success, TimeSpan Duration)> results,
        CancellationToken cancellationToken
    )
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            bool success = false;

            try
            {
                var response = await operation();
                success = response.IsSuccessStatusCode;
            }
            catch
            {
                success = false;
            }

            stopwatch.Stop();
            results.Add((operationName, success, stopwatch.Elapsed));

            try
            {
                await Task.Delay(intervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
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
}
