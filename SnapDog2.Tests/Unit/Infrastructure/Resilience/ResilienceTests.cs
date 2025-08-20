using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Moq;
using Polly;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Helpers;

namespace SnapDog2.Tests.Unit.Infrastructure.Resilience;

/// <summary>
/// Real-world resilience scenario tests that simulate actual failure patterns
/// encountered in smart home environments with KNX, MQTT, Snapcast, and Subsonic services.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Scenario", "RealWorld")]
public class ResilienceTests
{
    #region Network Partition Scenarios

    [Fact]
    public async Task NetworkPartition_WhenKnxGatewayTemporarilyUnavailable_ShouldRecoverGracefully()
    {
        // Arrange - Simulate KNX gateway going offline and coming back
        var config = CreateProductionResilienceConfig();
        var pipeline = ResiliencePolicyFactory.CreateConnectionPipeline(config, "KNX");

        var mockKnxGateway = new Mock<IKnxGateway>();
        var callCount = 0;
        var networkPartitionDuration = TimeSpan.FromMilliseconds(200);
        var partitionStart = DateTime.UtcNow;

        mockKnxGateway
            .Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                var elapsed = DateTime.UtcNow - partitionStart;

                if (elapsed < networkPartitionDuration)
                {
                    throw new SocketException((int)SocketError.NetworkUnreachable, "KNX gateway network unreachable");
                }

                return Task.CompletedTask; // Network recovered
            });

        // Act
        var startTime = DateTime.UtcNow;
        await pipeline.ExecuteAsync(async _ =>
        {
            await mockKnxGateway.Object.ConnectAsync(CancellationToken.None);
        });
        var endTime = DateTime.UtcNow;

        // Assert
        callCount.Should().BeGreaterThan(1, "should retry during network partition");
        (endTime - startTime).Should().BeGreaterThan(networkPartitionDuration, "should wait for network recovery");
        mockKnxGateway.Verify(x => x.ConnectAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    #endregion

    #region Service Overload Scenarios

    [Fact]
    public async Task ServiceOverload_WhenMqttBrokerOverloaded_ShouldBackoffAndRecover()
    {
        // Arrange - Simulate MQTT broker under heavy load
        var config = CreateProductionResilienceConfig();
        var pipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "MQTT");

        var mockMqttBroker = new Mock<IMqttBroker>();
        var callCount = 0;
        var overloadThreshold = 3; // Broker recovers after 3 attempts

        mockMqttBroker
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;

                if (callCount <= overloadThreshold)
                {
                    throw new HttpRequestException(
                        "Service temporarily overloaded",
                        null,
                        HttpStatusCode.ServiceUnavailable
                    );
                }

                return Task.CompletedTask; // Broker recovered
            });

        // Act
        await pipeline.ExecuteAsync(async _ =>
        {
            await mockMqttBroker.Object.PublishAsync(
                "home/living-room/light",
                System.Text.Encoding.UTF8.GetBytes("ON"),
                CancellationToken.None
            );
        });

        // Assert
        callCount.Should().Be(overloadThreshold + 1, "should retry until broker recovers");
        mockMqttBroker.Verify(
            x => x.PublishAsync("home/living-room/light", It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(overloadThreshold + 1)
        );
    }

    #endregion

    #region Audio System Scenarios

    [Fact]
    public async Task AudioSystemRestart_WhenSnapcastServerRestarting_ShouldWaitAndReconnect()
    {
        // Arrange - Simulate Snapcast server restart cycle
        var config = CreateProductionResilienceConfig();
        var pipeline = ResiliencePolicyFactory.CreateConnectionPipeline(config, "Snapcast");

        var mockSnapcastServer = new Mock<ISnapcastServer>();
        var callCount = 0;
        var restartPhases = new[]
        {
            "Connection refused", // Server stopping
            "Connection refused", // Server stopped
            "Service unavailable", // Server starting
            "Connected", // Server ready
        };

        mockSnapcastServer
            .Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var phase = restartPhases[Math.Min(callCount, restartPhases.Length - 1)];
                callCount++;

                if (phase != "Connected")
                {
                    throw new InvalidOperationException($"Snapcast server: {phase}");
                }

                return Task.CompletedTask;
            });

        // Act
        await pipeline.ExecuteAsync(async _ =>
        {
            await mockSnapcastServer.Object.ConnectAsync(CancellationToken.None);
        });

        // Assert
        callCount.Should().Be(4, "should retry through all restart phases");
        mockSnapcastServer.Verify(x => x.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task MediaServerMaintenance_WhenSubsonicInMaintenanceMode_ShouldHandleGracefully()
    {
        // Arrange - Simulate Subsonic maintenance window
        var config = CreateProductionResilienceConfig();
        var pipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "Subsonic");

        var mockSubsonicServer = new Mock<ISubsonicServer>();
        var callCount = 0;
        var maintenanceWindow = TimeSpan.FromMilliseconds(150);
        var maintenanceStart = DateTime.UtcNow;

        mockSubsonicServer
            .Setup(x => x.GetAlbumsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                var elapsed = DateTime.UtcNow - maintenanceStart;

                if (elapsed < maintenanceWindow)
                {
                    throw new HttpRequestException(
                        "Server in maintenance mode",
                        null,
                        HttpStatusCode.ServiceUnavailable
                    );
                }

                return Task.FromResult(
                    new List<Album>
                    {
                        new() { Id = "1", Name = "Test Album" },
                    }
                );
            });

        // Act
        var result = await pipeline.ExecuteAsync(async _ =>
        {
            return await mockSubsonicServer.Object.GetAlbumsAsync(CancellationToken.None);
        });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        callCount.Should().BeGreaterThan(1, "should retry during maintenance");
    }

    #endregion

    #region Cascading Failure Scenarios

    [Fact]
    public async Task CascadingFailure_WhenMultipleServicesFailSequentially_ShouldIsolateFailures()
    {
        // Arrange - Simulate cascading failure across services
        var config = CreateProductionResilienceConfig();
        var knxPipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "KNX");
        var mqttPipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "MQTT");
        var snapcastPipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "Snapcast");

        var mockKnx = new Mock<IKnxGateway>();
        var mockMqtt = new Mock<IMqttBroker>();
        var mockSnapcast = new Mock<ISnapcastServer>();

        // KNX fails completely
        mockKnx
            .Setup(x => x.SendCommandAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("KNX gateway timeout"));

        // MQTT recovers after 2 attempts
        var mqttCallCount = 0;
        mockMqtt
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                mqttCallCount++;
                if (mqttCallCount <= 2)
                    throw new SocketException((int)SocketError.ConnectionReset, "Connection reset");
                return Task.CompletedTask;
            });

        // Snapcast works immediately
        mockSnapcast
            .Setup(x => x.SetVolumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Execute operations in parallel to simulate real scenario
        var knxTask = TryExecuteAsync<string>(() =>
            knxPipeline
                .ExecuteAsync(async _ =>
                {
                    await mockKnx.Object.SendCommandAsync("1/2/3", true, CancellationToken.None);
                    return "KNX Success";
                })
                .AsTask()
        );

        var mqttTask = TryExecuteAsync<string>(() =>
            mqttPipeline
                .ExecuteAsync(async _ =>
                {
                    await mockMqtt.Object.PublishAsync(
                        "status",
                        System.Text.Encoding.UTF8.GetBytes("online"),
                        CancellationToken.None
                    );
                    return "MQTT Success";
                })
                .AsTask()
        );

        var snapcastTask = TryExecuteAsync<string>(() =>
            snapcastPipeline
                .ExecuteAsync(async _ =>
                {
                    await mockSnapcast.Object.SetVolumeAsync("client1", 75, CancellationToken.None);
                    return "Snapcast Success";
                })
                .AsTask()
        );

        var results = await Task.WhenAll(knxTask, mqttTask, snapcastTask);

        // Assert - Failures should be isolated
        results[0].Success.Should().BeFalse("KNX should fail after retries");
        results[1].Success.Should().BeTrue("MQTT should recover after retries");
        results[2].Success.Should().BeTrue("Snapcast should work immediately");

        mqttCallCount.Should().Be(3, "MQTT should retry twice before succeeding");
    }

    #endregion

    #region Performance Under Load Scenarios

    [Fact]
    public async Task HighLoadScenario_WhenMultipleConcurrentOperations_ShouldMaintainResilience()
    {
        // Arrange - Simulate high load with multiple concurrent operations
        var config = CreateProductionResilienceConfig();
        var pipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "MQTT");

        var mockMqttBroker = new Mock<IMqttBroker>();
        var totalCalls = 0;
        var successfulCalls = 0;

        mockMqttBroker
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref totalCalls);

                // Simulate 30% failure rate under load
                if (Random.Shared.NextDouble() < 0.3)
                {
                    throw new HttpRequestException("Broker overloaded", null, HttpStatusCode.TooManyRequests);
                }

                Interlocked.Increment(ref successfulCalls);
                return Task.CompletedTask;
            });

        // Act - Execute 20 concurrent operations
        var concurrentOperations = Enumerable
            .Range(0, 20)
            .Select(i =>
                pipeline
                    .ExecuteAsync(async _ =>
                    {
                        await mockMqttBroker.Object.PublishAsync(
                            $"sensor/{i}",
                            System.Text.Encoding.UTF8.GetBytes($"value_{i}"),
                            CancellationToken.None
                        );
                        return i;
                    })
                    .AsTask()
            )
            .ToArray();

        var results = await Task.WhenAll(concurrentOperations);

        // Assert
        results.Should().HaveCount(20, "all operations should eventually succeed");
        results.Should().OnlyContain(x => x >= 0, "all results should be valid");
        totalCalls.Should().BeGreaterThan(20, "should have retries due to simulated failures");
    }

    #endregion

    #region Helper Methods

    private static ResilienceConfig CreateProductionResilienceConfig()
    {
        return new ResilienceConfig
        {
            Connection = new PolicyConfig
            {
                MaxRetries = 5,
                RetryDelayMs = 100,
                BackoffType = "Exponential",
                UseJitter = true,
                TimeoutSeconds = 10,
                JitterPercentage = 25,
            },
            Operation = new PolicyConfig
            {
                MaxRetries = 3,
                RetryDelayMs = 50,
                BackoffType = "Exponential",
                UseJitter = true,
                TimeoutSeconds = 5,
                JitterPercentage = 20,
            },
        };
    }

    private static async Task<(bool Success, T? Result, Exception? Exception)> TryExecuteAsync<T>(
        Func<Task<T>> operation
    )
    {
        try
        {
            var result = await operation();
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, default, ex);
        }
    }

    #endregion

    #region Mock Interfaces and Models

    public interface IKnxGateway
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SendCommandAsync(string address, bool value, CancellationToken cancellationToken);
    }

    public interface IMqttBroker
    {
        Task PublishAsync(string topic, byte[] payload, CancellationToken cancellationToken);
    }

    public interface ISnapcastServer
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SetVolumeAsync(string clientId, int volume, CancellationToken cancellationToken);
    }

    public interface ISubsonicServer
    {
        Task<List<Album>> GetAlbumsAsync(CancellationToken cancellationToken);
    }

    public record Album
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
    }

    #endregion
}
