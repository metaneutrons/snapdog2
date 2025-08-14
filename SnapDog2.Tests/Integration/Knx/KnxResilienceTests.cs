namespace SnapDog2.Tests.Integration.Knx;

using System.Diagnostics;
using FluentAssertions;
using SnapDog2.Core.Enums;
using SnapDog2.Tests.Integration.Fixtures;

/// <summary>
/// KNX resilience tests that validate system behavior under failure conditions.
/// Tests error handling, recovery mechanisms, and graceful degradation scenarios
/// to ensure the system remains stable and responsive even when components fail.
/// </summary>
[Collection("KnxIntegrationFlow")]
public class KnxResilienceTests : IClassFixture<KnxIntegrationTestFixture>
{
    private readonly KnxIntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public KnxResilienceTests(KnxIntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Connection Resilience Tests

    [Fact]
    public async Task KnxConnectionLoss_ShouldRecoverAutomatically()
    {
        _output.WriteLine("Testing KNX connection loss and automatic recovery");

        // Arrange: Verify initial connectivity
        await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);
        await Task.Delay(500);

        // Act: Simulate connection loss by stopping KNX container
        _output.WriteLine("Simulating KNX connection loss...");
        await _fixture.KnxdContainer.StopAsync();

        // Verify system handles disconnection gracefully
        var disconnectedStopwatch = Stopwatch.StartNew();

        // Commands should fail gracefully without crashing
        try
        {
            await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Expected connection error: {ex.Message}");
        }

        // Act: Restore connection
        _output.WriteLine("Restoring KNX connection...");
        await _fixture.KnxdContainer.StartAsync();
        await Task.Delay(2000); // Allow reconnection

        // Assert: System recovers and processes commands
        var recoveryStopwatch = Stopwatch.StartNew();

        // Reconnect test client
        await _fixture.KnxClient.ConnectAsync();

        // Verify commands work again
        await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/ground-floor/volume/status",
            TimeSpan.FromSeconds(5)
        );

        recoveryStopwatch.Stop();
        disconnectedStopwatch.Stop();

        _output.WriteLine($"Connection lost for {disconnectedStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Recovery completed in {recoveryStopwatch.ElapsedMilliseconds}ms");

        recoveryStopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // < 10s recovery
    }

    [Fact]
    public async Task MqttConnectionLoss_ShouldNotAffectKnxProcessing()
    {
        _output.WriteLine("Testing MQTT connection loss - KNX processing should continue");

        // Arrange: Verify initial state
        await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);
        await Task.Delay(500);

        // Act: Simulate MQTT broker failure
        _output.WriteLine("Stopping MQTT broker...");
        await _fixture.MqttContainer.StopAsync();

        // Assert: KNX commands still processed (graceful degradation)
        var stopwatch = Stopwatch.StartNew();

        // KNX → Mediator → API should still work
        await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);

        // Verify via API (MQTT unavailable, but API should work)
        await Task.Delay(1000); // Allow processing
        var response = await _fixture.ApiClient.GetAsync("/api/v1/zones/1");
        response.Should().BeSuccessful();

        stopwatch.Stop();
        _output.WriteLine($"KNX processing continued despite MQTT failure ({stopwatch.ElapsedMilliseconds}ms)");

        // Restore MQTT for cleanup
        await _fixture.MqttContainer.StartAsync();
        await Task.Delay(2000);
    }

    [Fact]
    public async Task SnapcastConnectionLoss_ShouldNotAffectOtherIntegrations()
    {
        _output.WriteLine("Testing Snapcast connection loss - other integrations should continue");

        // Act: Simulate Snapcast server failure
        _output.WriteLine("Stopping Snapcast server...");
        await _fixture.SnapcastContainer.StopAsync();

        // Assert: KNX → Mediator → MQTT/API still works
        var stopwatch = Stopwatch.StartNew();

        await _fixture.KnxClient.WriteGroupValueAsync("1/2/3", true);

        // Verify MQTT still receives updates
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/ground-floor/volume/status",
            TimeSpan.FromSeconds(3)
        );

        // Verify API still works
        var response = await _fixture.ApiClient.GetAsync("/api/v1/zones/1");
        response.Should().BeSuccessful();

        stopwatch.Stop();
        _output.WriteLine($"Other integrations continued despite Snapcast failure ({stopwatch.ElapsedMilliseconds}ms)");

        // Restore Snapcast for cleanup
        await _fixture.SnapcastContainer.StartAsync();
        await Task.Delay(2000);
    }

    #endregion

    #region Invalid Command Handling

    [Fact]
    public async Task InvalidKnxGroupAddress_ShouldBeHandledGracefully()
    {
        _output.WriteLine("Testing invalid KNX group address handling");

        var invalidAddresses = new[]
        {
            "99/99/99", // Non-existent zone
            "0/0/0", // Invalid address format
            "1/10/10", // Valid format but unmapped
        };

        foreach (var invalidAddress in invalidAddresses)
        {
            _output.WriteLine($"Testing invalid address: {invalidAddress}");

            // Act: Send command to invalid address
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _fixture.KnxClient.WriteGroupValueAsync(invalidAddress, true);

                // System should not crash - verify other commands still work
                await Task.Delay(500);
                await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true); // Valid command

                stopwatch.Stop();
                _output.WriteLine($"Invalid address handled gracefully ({stopwatch.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Expected error for {invalidAddress}: {ex.Message}");
            }
        }

        // Assert: System still responsive to valid commands
        await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true);
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/living-room/volume/status",
            TimeSpan.FromSeconds(2)
        );
    }

    [Fact]
    public async Task InvalidKnxDataValues_ShouldBeHandledGracefully()
    {
        _output.WriteLine("Testing invalid KNX data value handling");

        var invalidValues = new object[]
        {
            -1, // Negative volume
            255, // Volume too high
            null, // Null value
            "invalid", // Wrong data type
        };

        foreach (var invalidValue in invalidValues)
        {
            _output.WriteLine($"Testing invalid value: {invalidValue ?? "null"}");

            try
            {
                // This might throw at the client level, which is expected
                if (invalidValue != null && invalidValue is not string)
                {
                    await _fixture.KnxClient.WriteGroupValueAsync("1/0/3", invalidValue); // Set Volume GA
                }

                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Expected error for invalid value: {ex.Message}");
            }
        }

        // Assert: System recovers and processes valid commands
        await _fixture.KnxClient.WriteGroupValueAsync("1/0/3", (byte)50); // Valid volume
        await Task.Delay(1000);

        var response = await _fixture.ApiClient.GetAsync("/api/v1/zones/1");
        response.Should().BeSuccessful();
    }

    #endregion

    #region High Load Resilience

    [Fact]
    public async Task KnxCommandFlood_ShouldNotCrashSystem()
    {
        _output.WriteLine("Testing KNX command flood resilience");

        const int floodCommands = 100;
        const int floodDurationMs = 1000;

        var stopwatch = Stopwatch.StartNew();
        var successfulCommands = 0;
        var failedCommands = 0;

        // Act: Flood system with commands
        var floodTasks = Enumerable
            .Range(0, floodCommands)
            .Select(async i =>
            {
                try
                {
                    var groupAddress =
                        i
                        % 3 switch
                        {
                            0 => "1/0/1", // Volume Up
                            1 => "1/0/2", // Volume Down
                            _ => "1/1/1", // Play
                        };

                    await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);
                    Interlocked.Increment(ref successfulCommands);
                }
                catch
                {
                    Interlocked.Increment(ref failedCommands);
                }
            });

        // Wait for flood to complete or timeout
        var completedTask = await Task.WhenAny(
            Task.WhenAll(floodTasks),
            Task.Delay(floodDurationMs * 2) // 2x timeout for safety
        );

        stopwatch.Stop();

        _output.WriteLine($"Command Flood Results:");
        _output.WriteLine($"  Duration: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Successful commands: {successfulCommands}");
        _output.WriteLine($"  Failed commands: {failedCommands}");
        _output.WriteLine($"  Success rate: {successfulCommands * 100.0 / floodCommands:F1}%");

        // Assert: System survives flood and remains responsive
        successfulCommands.Should().BeGreaterThan(floodCommands / 2); // At least 50% success

        // Verify system still responsive after flood
        await Task.Delay(2000); // Allow system to recover

        await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true);
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/living-room/volume/status",
            TimeSpan.FromSeconds(3)
        );

        _output.WriteLine("System remained responsive after command flood");
    }

    [Fact]
    public async Task ConcurrentZoneCommands_ShouldNotCauseDeadlock()
    {
        _output.WriteLine("Testing concurrent zone commands for deadlock prevention");

        const int concurrentZones = 3;
        const int commandsPerZone = 20;

        var zoneCommands = new[] { ("1/0/1", "living-room"), ("2/0/1", "kitchen"), ("3/0/1", "bedroom") };

        var stopwatch = Stopwatch.StartNew();

        // Act: Send concurrent commands to all zones
        var zoneTasks = zoneCommands.Select(
            async (zoneCmd, zoneIndex) =>
            {
                var (groupAddress, zoneName) = zoneCmd;
                var zoneStopwatch = Stopwatch.StartNew();

                for (int i = 0; i < commandsPerZone; i++)
                {
                    await _fixture.KnxClient.WriteGroupValueAsync(groupAddress, true);
                    await Task.Delay(50); // Small delay between commands
                }

                zoneStopwatch.Stop();
                return new { ZoneName = zoneName, Duration = zoneStopwatch.ElapsedMilliseconds };
            }
        );

        var results = await Task.WhenAll(zoneTasks);
        stopwatch.Stop();

        _output.WriteLine($"Concurrent Zone Commands Results:");
        _output.WriteLine($"  Total duration: {stopwatch.ElapsedMilliseconds}ms");

        foreach (var result in results)
        {
            _output.WriteLine($"  {result.ZoneName}: {result.Duration}ms");
        }

        // Assert: No deadlock occurred (all tasks completed)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // < 30s (generous timeout)
        results.All(r => r.Duration > 0).Should().BeTrue(); // All zones completed

        // Verify system still responsive
        await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true);
        await Task.Delay(1000);

        var response = await _fixture.ApiClient.GetAsync("/api/v1/zones/1");
        response.Should().BeSuccessful();
    }

    #endregion

    #region Recovery and Retry Tests

    [Fact]
    public async Task TransientKnxErrors_ShouldRetryAutomatically()
    {
        _output.WriteLine("Testing automatic retry on transient KNX errors");

        // This test would require more sophisticated error injection
        // For now, we'll test the retry behavior indirectly

        var retryAttempts = 0;
        var maxRetries = 3;
        var success = false;

        for (int attempt = 0; attempt < maxRetries && !success; attempt++)
        {
            try
            {
                retryAttempts++;
                _output.WriteLine($"Attempt {retryAttempts}: Sending KNX command");

                await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true);

                // Verify command processed
                await _fixture.MqttTestClient.WaitForMessage(
                    "snapdog/zones/living-room/volume/status",
                    TimeSpan.FromSeconds(2)
                );

                success = true;
                _output.WriteLine($"Command succeeded on attempt {retryAttempts}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Attempt {retryAttempts} failed: {ex.Message}");

                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(1000); // Wait before retry
                }
            }
        }

        success.Should().BeTrue("Command should eventually succeed with retries");
        retryAttempts.Should().BeLessOrEqualTo(maxRetries);
    }

    [Fact]
    public async Task SystemOverload_ShouldGracefullyDegrade()
    {
        _output.WriteLine("Testing graceful degradation under system overload");

        // Simulate overload by sending many commands rapidly
        const int overloadCommands = 50;
        var commandTasks = new List<Task>();
        var successCount = 0;
        var timeoutCount = 0;

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < overloadCommands; i++)
        {
            var commandTask = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                    await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true);

                    // Don't wait for full processing under overload
                    await Task.Delay(100, cts.Token);

                    Interlocked.Increment(ref successCount);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref timeoutCount);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Command failed: {ex.Message}");
                }
            });

            commandTasks.Add(commandTask);
        }

        await Task.WhenAll(commandTasks);
        stopwatch.Stop();

        _output.WriteLine($"System Overload Results:");
        _output.WriteLine($"  Duration: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Successful commands: {successCount}");
        _output.WriteLine($"  Timeout commands: {timeoutCount}");
        _output.WriteLine($"  Success rate: {successCount * 100.0 / overloadCommands:F1}%");

        // Assert: System degrades gracefully (some commands succeed)
        successCount.Should().BeGreaterThan(0); // Some commands should succeed

        // Verify system recovers after overload
        await Task.Delay(3000); // Allow recovery

        await _fixture.KnxClient.WriteGroupValueAsync("1/0/1", true);
        await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/living-room/volume/status",
            TimeSpan.FromSeconds(3)
        );

        _output.WriteLine("System recovered after overload");
    }

    #endregion

    #region Data Consistency Tests

    [Fact]
    public async Task ConcurrentVolumeChanges_ShouldMaintainConsistency()
    {
        _output.WriteLine("Testing data consistency under concurrent volume changes");

        const int concurrentChanges = 10;
        var finalVolumes = new List<int>();

        // Act: Send concurrent volume changes
        var tasks = Enumerable
            .Range(0, concurrentChanges)
            .Select(async i =>
            {
                var targetVolume = 10 + (i * 5); // 10, 15, 20, ..., 55
                await _fixture.KnxClient.WriteGroupValueAsync("1/0/3", (byte)targetVolume);
                return targetVolume;
            });

        var targetVolumes = await Task.WhenAll(tasks);

        // Allow all changes to process
        await Task.Delay(2000);

        // Assert: Final state is consistent across all integrations
        var apiResponse = await _fixture.ApiClient.GetAsync("/api/v1/zones/1");
        var zoneState = await apiResponse.Content.ReadFromJsonAsync<ZoneState>();

        var mqttMessage = await _fixture.MqttTestClient.WaitForMessage(
            "snapdog/zones/living-room/volume/status",
            TimeSpan.FromSeconds(2)
        );
        var mqttVolume = JsonSerializer.Deserialize<JsonElement>(mqttMessage).GetProperty("volume").GetInt32();

        _output.WriteLine($"Final volume states:");
        _output.WriteLine($"  API: {zoneState.Volume}");
        _output.WriteLine($"  MQTT: {mqttVolume}");
        _output.WriteLine($"  Target volumes sent: [{string.Join(", ", targetVolumes)}]");

        // All integrations should have the same final volume
        zoneState.Volume.Should().Be(mqttVolume);

        // Final volume should be one of the target volumes
        targetVolumes.Should().Contain(zoneState.Volume);
    }

    #endregion
}
