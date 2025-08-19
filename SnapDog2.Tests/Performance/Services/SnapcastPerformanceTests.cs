using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Fixtures.Shared;
using SnapDog2.Tests.Helpers.Extensions;

namespace SnapDog2.Tests.Performance.Services;

/// <summary>
/// Enterprise-grade performance tests for Snapcast service operations.
/// These tests measure response times, throughput, and resource usage under various load conditions.
/// </summary>
[Collection(TestCategories.Performance)]
[Trait("Category", TestCategories.Performance)]
[Trait("Type", TestTypes.Service)]
[Trait("Speed", TestSpeed.Slow)]
[RequiresAttribute(TestRequirements.Docker)]
public class SnapcastPerformanceTests
{
    private readonly DockerComposeTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SnapcastPerformanceTests(DockerComposeTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    [TestSpeed(TestSpeed.Slow)]
    [Trait("Benchmark", "ResponseTime")]
    public async Task GetServerStatus_PerformanceBenchmark_ShouldMeetResponseTimeRequirements()
    {
        // Arrange
        _output.WriteSection("Snapcast Server Status Performance Benchmark");

        using var scope = _fixture.ServiceProvider.CreateScope();
        var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

        const int iterations = 100;
        const int maxAcceptableResponseTimeMs = 100;
        var responseTimes = new List<double>();

        // Warm up
        _output.WriteStep("Warm Up", "Performing warm-up requests");
        for (int i = 0; i < 5; i++)
        {
            await snapcastService.GetServerStatusAsync();
        }

        // Act - Performance measurement
        _output.WriteStep("Performance Measurement", $"Executing {iterations} requests");

        var totalStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await snapcastService.GetServerStatusAsync();
            stopwatch.Stop();

            result.Should().BeSuccessful($"Request {i + 1} should succeed");
            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);

            if ((i + 1) % 20 == 0)
            {
                _output.WriteInfo($"Completed {i + 1}/{iterations} requests");
            }
        }

        totalStopwatch.Stop();

        // Assert - Performance requirements
        var avgResponseTime = responseTimes.Average();
        var minResponseTime = responseTimes.Min();
        var maxResponseTime = responseTimes.Max();
        var p95ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(iterations * 0.95)).First();
        var p99ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(iterations * 0.99)).First();
        var throughput = iterations / totalStopwatch.Elapsed.TotalSeconds;

        // Log performance metrics
        _output.WriteSection("Performance Results");
        _output.WritePerformance("Average Response Time", TimeSpan.FromMilliseconds(avgResponseTime));
        _output.WritePerformance("Min Response Time", TimeSpan.FromMilliseconds(minResponseTime));
        _output.WritePerformance("Max Response Time", TimeSpan.FromMilliseconds(maxResponseTime));
        _output.WritePerformance("95th Percentile", TimeSpan.FromMilliseconds(p95ResponseTime));
        _output.WritePerformance("99th Percentile", TimeSpan.FromMilliseconds(p99ResponseTime));
        _output.WriteInfo($"📊 Throughput: {throughput:F2} requests/second");
        _output.WriteInfo($"📊 Total Test Duration: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");

        // Performance assertions
        avgResponseTime
            .Should()
            .BeLessThan(
                maxAcceptableResponseTimeMs,
                $"Average response time should be less than {maxAcceptableResponseTimeMs}ms"
            );

        p95ResponseTime
            .Should()
            .BeLessThan(
                maxAcceptableResponseTimeMs * 2,
                $"95th percentile response time should be less than {maxAcceptableResponseTimeMs * 2}ms"
            );

        throughput.Should().BeGreaterThan(10, "Should handle at least 10 requests per second");

        _output.WriteSuccess("Performance benchmark completed successfully");
    }

    [Fact]
    [TestSpeed(TestSpeed.VerySlow)]
    [Trait("Benchmark", "Concurrency")]
    public async Task GetServerStatus_ConcurrentRequests_ShouldHandleLoadEfficiently()
    {
        // Arrange
        _output.WriteSection("Snapcast Concurrent Request Performance Test");

        using var scope = _fixture.ServiceProvider.CreateScope();
        var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

        const int concurrentUsers = 10;
        const int requestsPerUser = 20;
        const int maxAcceptableResponseTimeMs = 200; // Higher threshold for concurrent load

        // Act - Concurrent load test
        _output.WriteStep(
            "Concurrent Load Test",
            $"Simulating {concurrentUsers} concurrent users, {requestsPerUser} requests each"
        );

        var allTasks = new List<Task<List<double>>>();
        var totalStopwatch = Stopwatch.StartNew();

        for (int user = 0; user < concurrentUsers; user++)
        {
            var userIndex = user;
            var userTask = Task.Run(async () =>
            {
                var userResponseTimes = new List<double>();

                for (int request = 0; request < requestsPerUser; request++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var result = await snapcastService.GetServerStatusAsync();
                    stopwatch.Stop();

                    result.Should().BeSuccessful($"User {userIndex}, Request {request + 1} should succeed");
                    userResponseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                }

                return userResponseTimes;
            });

            allTasks.Add(userTask);
        }

        var allResults = await Task.WhenAll(allTasks);
        totalStopwatch.Stop();

        // Analyze results
        var allResponseTimes = allResults.SelectMany(x => x).ToList();
        var totalRequests = concurrentUsers * requestsPerUser;

        var avgResponseTime = allResponseTimes.Average();
        var minResponseTime = allResponseTimes.Min();
        var maxResponseTime = allResponseTimes.Max();
        var p95ResponseTime = allResponseTimes.OrderBy(x => x).Skip((int)(totalRequests * 0.95)).First();
        var p99ResponseTime = allResponseTimes.OrderBy(x => x).Skip((int)(totalRequests * 0.99)).First();
        var throughput = totalRequests / totalStopwatch.Elapsed.TotalSeconds;

        // Log concurrent performance metrics
        _output.WriteSection("Concurrent Performance Results");
        _output.WriteInfo($"📊 Total Requests: {totalRequests}");
        _output.WriteInfo($"📊 Concurrent Users: {concurrentUsers}");
        _output.WritePerformance("Average Response Time", TimeSpan.FromMilliseconds(avgResponseTime));
        _output.WritePerformance("Min Response Time", TimeSpan.FromMilliseconds(minResponseTime));
        _output.WritePerformance("Max Response Time", TimeSpan.FromMilliseconds(maxResponseTime));
        _output.WritePerformance("95th Percentile", TimeSpan.FromMilliseconds(p95ResponseTime));
        _output.WritePerformance("99th Percentile", TimeSpan.FromMilliseconds(p99ResponseTime));
        _output.WriteInfo($"📊 Concurrent Throughput: {throughput:F2} requests/second");
        _output.WriteInfo($"📊 Total Test Duration: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");

        // Performance assertions for concurrent load
        avgResponseTime
            .Should()
            .BeLessThan(
                maxAcceptableResponseTimeMs,
                $"Average response time under concurrent load should be less than {maxAcceptableResponseTimeMs}ms"
            );

        p95ResponseTime
            .Should()
            .BeLessThan(
                maxAcceptableResponseTimeMs * 3,
                $"95th percentile response time under load should be reasonable"
            );

        throughput.Should().BeGreaterThan(5, "Should maintain reasonable throughput under concurrent load");

        // Check for any failed requests
        var successfulRequests = allResults.Sum(x => x.Count);
        successfulRequests.Should().Be(totalRequests, "All concurrent requests should succeed");

        _output.WriteSuccess("Concurrent performance test completed successfully");
    }

    [Fact]
    [TestSpeed(TestSpeed.Medium)]
    [Trait("Benchmark", "Memory")]
    public async Task GetServerStatus_MemoryUsage_ShouldNotLeakMemory()
    {
        // Arrange
        _output.WriteSection("Snapcast Memory Usage Test");

        using var scope = _fixture.ServiceProvider.CreateScope();
        var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

        const int iterations = 50;

        // Measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);
        _output.WriteInfo($"📊 Initial Memory: {initialMemory / 1024.0 / 1024.0:F2} MB");

        // Act - Execute operations
        _output.WriteStep("Memory Test", $"Executing {iterations} operations");

        for (int i = 0; i < iterations; i++)
        {
            var result = await snapcastService.GetServerStatusAsync();
            result.Should().BeSuccessful($"Request {i + 1} should succeed");

            if ((i + 1) % 10 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                _output.WriteInfo($"Memory after {i + 1} requests: {currentMemory / 1024.0 / 1024.0:F2} MB");
            }
        }

        // Force garbage collection and measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePercentage = (double)memoryIncrease / initialMemory * 100;

        _output.WriteSection("Memory Usage Results");
        _output.WriteInfo($"📊 Initial Memory: {initialMemory / 1024.0 / 1024.0:F2} MB");
        _output.WriteInfo($"📊 Final Memory: {finalMemory / 1024.0 / 1024.0:F2} MB");
        _output.WriteInfo(
            $"📊 Memory Increase: {memoryIncrease / 1024.0 / 1024.0:F2} MB ({memoryIncreasePercentage:F2}%)"
        );

        // Assert - Memory usage should be reasonable
        memoryIncreasePercentage.Should().BeLessThan(50, "Memory increase should be less than 50% of initial memory");

        _output.WriteSuccess("Memory usage test completed successfully");
    }
}
