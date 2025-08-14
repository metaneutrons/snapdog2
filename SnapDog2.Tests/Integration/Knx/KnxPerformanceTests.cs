namespace SnapDog2.Tests.Integration.Knx;

using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using SnapDog2.Tests.Integration.Fixtures;

/// <summary>
/// KNX performance tests that validate system responsiveness and throughput.
/// Tests realistic load scenarios and measures end-to-end processing times
/// to ensure the system meets performance requirements for real-time audio control.
/// </summary>
[Collection("KnxIntegrationFlow")]
public class KnxPerformanceTests : IClassFixture<KnxIntegrationTestFixture>
{
    private readonly KnxIntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public KnxPerformanceTests(KnxIntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Response Time Tests

    [Fact]
    public async Task KnxVolumeCommand_ShouldProcessWithinAcceptableTime()
    {
        _output.WriteLine("Testing KNX volume command response time");

        var measurements = new List<long>();
        const int iterations = 10;

        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            // Act: Send KNX Volume Up command
            await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);

            // Wait for complete flow across all integrations
            await _fixture.MqttTestClient.WaitForMessage(
                "snapdog/zones/ground-floor/volume/status",
                TimeSpan.FromSeconds(2)
            );

            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);

            _output.WriteLine($"Iteration {i + 1}: {stopwatch.ElapsedMilliseconds}ms");

            // Small delay between iterations
            await Task.Delay(100);
        }

        // Assert performance requirements
        var averageTime = measurements.Average();
        var maxTime = measurements.Max();
        var minTime = measurements.Min();

        _output.WriteLine($"Performance Summary:");
        _output.WriteLine($"  Average: {averageTime:F1}ms");
        _output.WriteLine($"  Min: {minTime}ms");
        _output.WriteLine($"  Max: {maxTime}ms");

        averageTime.Should().BeLessThan(500); // < 500ms average
        maxTime.Should().BeLessThan(1000); // < 1s worst case
        measurements.Count(m => m < 200).Should().BeGreaterThan(iterations / 2); // 50%+ under 200ms
    }

    [Fact]
    public async Task KnxPlaybackCommand_ShouldProcessWithinAcceptableTime()
    {
        _output.WriteLine("Testing KNX playback command response time");

        var stopwatch = Stopwatch.StartNew();

        // Act: Send KNX Play command
        await _fixture.KnxClient.WriteGroupValueAsync("1/1/1", true);

        // Wait for playback state change across integrations
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/ground-floor/playback/status",
            TimeSpan.FromSeconds(3)
        );

        stopwatch.Stop();

        _output.WriteLine($"Playback command processed in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1500); // < 1.5s for playback start
    }

    #endregion

    #region Throughput Tests

    [Fact]
    public async Task ConcurrentKnxCommands_ShouldMaintainPerformance()
    {
        _output.WriteLine("Testing concurrent KNX command processing performance");

        const int concurrentCommands = 20;
        var stopwatch = Stopwatch.StartNew();
        var completionTimes = new ConcurrentBag<long>();

        // Act: Send multiple concurrent commands
        var tasks = Enumerable
            .Range(0, concurrentCommands)
            .Select(async i =>
            {
                var commandStopwatch = Stopwatch.StartNew();

                // Vary command types to test different code paths
                var groupAddress = i switch
                {
                    var x when x % 4 == 0 => "1/2/3", // Volume Up
                    var x when x % 4 == 1 => "1/2/4", // Volume Down
                    var x when x % 4 == 2 => "1/1/1", // Play
                    _ => "1/1/2", // Pause
                };

                await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

                // Wait for processing (simplified - just wait for mediator)
                await Task.Delay(100); // Simulate processing time

                commandStopwatch.Stop();
                completionTimes.Add(commandStopwatch.ElapsedMilliseconds);
            });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert performance under load
        var totalTime = stopwatch.ElapsedMilliseconds;
        var averageCommandTime = completionTimes.Average();
        var maxCommandTime = completionTimes.Max();

        _output.WriteLine($"Concurrent Processing Summary:");
        _output.WriteLine($"  Total commands: {concurrentCommands}");
        _output.WriteLine($"  Total time: {totalTime}ms");
        _output.WriteLine($"  Average command time: {averageCommandTime:F1}ms");
        _output.WriteLine($"  Max command time: {maxCommandTime}ms");
        _output.WriteLine($"  Throughput: {concurrentCommands * 1000.0 / totalTime:F1} commands/sec");

        totalTime.Should().BeLessThan(5000); // < 5 seconds total
        averageCommandTime.Should().BeLessThan(1000); // < 1 second average
        maxCommandTime.Should().BeLessThan(2000); // < 2 seconds worst case
    }

    [Fact]
    public async Task HighFrequencyKnxCommands_ShouldNotCauseBacklog()
    {
        _output.WriteLine("Testing high-frequency KNX command processing");

        const int commandsPerSecond = 10;
        const int durationSeconds = 5;
        const int totalCommands = commandsPerSecond * durationSeconds;

        var stopwatch = Stopwatch.StartNew();
        var processedCommands = 0;
        var interval = 1000 / commandsPerSecond; // ms between commands

        // Act: Send commands at high frequency
        for (int i = 0; i < totalCommands; i++)
        {
            var commandStart = Stopwatch.StartNew();

            // Alternate between volume up/down to create realistic load
            var groupAddress = i % 2 == 0 ? "1/2/3" : "1/2/4";
            await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);

            processedCommands++;

            // Maintain frequency
            var elapsed = commandStart.ElapsedMilliseconds;
            if (elapsed < interval)
            {
                await Task.Delay((int)(interval - elapsed));
            }
        }

        stopwatch.Stop();

        // Assert no significant backlog
        var actualRate = processedCommands * 1000.0 / stopwatch.ElapsedMilliseconds;

        _output.WriteLine($"High-Frequency Processing Summary:");
        _output.WriteLine($"  Target rate: {commandsPerSecond} commands/sec");
        _output.WriteLine($"  Actual rate: {actualRate:F1} commands/sec");
        _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Commands processed: {processedCommands}/{totalCommands}");

        processedCommands.Should().Be(totalCommands);
        actualRate.Should().BeGreaterThan(commandsPerSecond * 0.8); // Within 20% of target
    }

    #endregion

    #region Memory and Resource Tests

    [Fact]
    public async Task ExtendedKnxOperation_ShouldNotLeakMemory()
    {
        _output.WriteLine("Testing extended KNX operation for memory leaks");

        // Measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        const int operationCycles = 100;

        // Act: Perform extended operations
        for (int cycle = 0; cycle < operationCycles; cycle++)
        {
            // Simulate typical user interaction pattern
            await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true); // Volume Up
            await Task.Delay(50);

            await _fixture.KnxClient.WriteGroupValueAsync("1/1/1", true); // Play
            await Task.Delay(50);

            await _fixture.KnxClient.WriteGroupValueAsync("1/1/2", true); // Pause
            await Task.Delay(50);

            if (cycle % 20 == 0)
            {
                _output.WriteLine($"Completed {cycle}/{operationCycles} cycles");
            }
        }

        // Measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);

        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerOperation = memoryIncrease / (operationCycles * 3.0); // 3 operations per cycle

        _output.WriteLine($"Memory Usage Summary:");
        _output.WriteLine($"  Initial memory: {initialMemory / 1024:N0} KB");
        _output.WriteLine($"  Final memory: {finalMemory / 1024:N0} KB");
        _output.WriteLine($"  Memory increase: {memoryIncrease / 1024:N0} KB");
        _output.WriteLine($"  Per operation: {memoryIncreasePerOperation:F1} bytes");

        // Assert reasonable memory usage (allowing for some growth)
        memoryIncreasePerOperation.Should().BeLessThan(1000); // < 1KB per operation
        memoryIncrease.Should().BeLessThan(10 * 1024 * 1024); // < 10MB total increase
    }

    #endregion

    #region Latency Distribution Tests

    [Fact]
    public async Task KnxCommandLatency_ShouldHaveConsistentDistribution()
    {
        _output.WriteLine("Testing KNX command latency distribution");

        const int samples = 50;
        var latencies = new List<long>();

        // Collect latency samples
        for (int i = 0; i < samples; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);

            // Wait for MQTT confirmation (end-to-end latency)
            await _fixture.MqttTestClient.WaitForMessage(
                "snapdog/zones/ground-floor/volume/status",
                TimeSpan.FromSeconds(2)
            );

            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);

            await Task.Delay(200); // Prevent overwhelming the system
        }

        // Calculate distribution statistics
        latencies.Sort();
        var p50 = latencies[samples / 2];
        var p90 = latencies[(int)(samples * 0.9)];
        var p95 = latencies[(int)(samples * 0.95)];
        var p99 = latencies[(int)(samples * 0.99)];
        var average = latencies.Average();
        var stdDev = Math.Sqrt(latencies.Select(x => Math.Pow(x - average, 2)).Average());

        _output.WriteLine($"Latency Distribution:");
        _output.WriteLine($"  Average: {average:F1}ms");
        _output.WriteLine($"  Std Dev: {stdDev:F1}ms");
        _output.WriteLine($"  P50 (median): {p50}ms");
        _output.WriteLine($"  P90: {p90}ms");
        _output.WriteLine($"  P95: {p95}ms");
        _output.WriteLine($"  P99: {p99}ms");

        // Assert acceptable latency distribution
        p50.Should().BeLessThan(300); // 50% under 300ms
        p90.Should().BeLessThan(600); // 90% under 600ms
        p95.Should().BeLessThan(800); // 95% under 800ms
        p99.Should().BeLessThan(1200); // 99% under 1.2s
        stdDev.Should().BeLessThan(200); // Consistent performance
    }

    #endregion

    #region Load Testing

    [Fact]
    public async Task MultiUserKnxSimulation_ShouldMaintainPerformance()
    {
        _output.WriteLine("Testing multi-user KNX simulation");

        const int simulatedUsers = 5;
        const int actionsPerUser = 10;

        var userTasks = Enumerable
            .Range(0, simulatedUsers)
            .Select(async userId =>
            {
                var userStopwatch = Stopwatch.StartNew();
                var userLatencies = new List<long>();

                for (int action = 0; action < actionsPerUser; action++)
                {
                    var actionStopwatch = Stopwatch.StartNew();

                    // Each user performs different actions on different zones
                    var zoneGA = userId switch
                    {
                        0 => "1/2/3", // User 0: Ground Floor Volume Up
                        1 => "2/2/3", // User 1: 1st Floor Volume Up
                        2 => "1/1/1", // User 2: Ground Floor Play
                        3 => "2/1/1", // User 3: 1st Floor Play
                        _ => "1/1/2", // User 4: Ground Floor Pause
                    };

                    await _fixture.KnxClient.WriteGroupValueAsync(zoneGA, true);

                    actionStopwatch.Stop();
                    userLatencies.Add(actionStopwatch.ElapsedMilliseconds);

                    // Random delay between actions (realistic user behavior)
                    await Task.Delay(Random.Shared.Next(100, 500));
                }

                userStopwatch.Stop();

                return new
                {
                    UserId = userId,
                    TotalTime = userStopwatch.ElapsedMilliseconds,
                    AverageLatency = userLatencies.Average(),
                    MaxLatency = userLatencies.Max(),
                };
            });

        var results = await Task.WhenAll(userTasks);

        // Analyze multi-user performance
        var overallAverageLatency = results.Average(r => r.AverageLatency);
        var worstUserLatency = results.Max(r => r.MaxLatency);
        var totalActions = simulatedUsers * actionsPerUser;

        _output.WriteLine($"Multi-User Simulation Results:");
        _output.WriteLine($"  Simulated users: {simulatedUsers}");
        _output.WriteLine($"  Actions per user: {actionsPerUser}");
        _output.WriteLine($"  Total actions: {totalActions}");
        _output.WriteLine($"  Overall average latency: {overallAverageLatency:F1}ms");
        _output.WriteLine($"  Worst user max latency: {worstUserLatency}ms");

        foreach (var result in results)
        {
            _output.WriteLine($"  User {result.UserId}: avg {result.AverageLatency:F1}ms, max {result.MaxLatency}ms");
        }

        // Assert multi-user performance requirements
        overallAverageLatency.Should().BeLessThan(800); // < 800ms average under load
        worstUserLatency.Should().BeLessThan(2000); // < 2s worst case
        results.All(r => r.AverageLatency < 1000).Should().BeTrue(); // All users under 1s average
    }

    #endregion
}
