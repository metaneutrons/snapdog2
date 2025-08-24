//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Timeout;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Helpers;

namespace SnapDog2.Tests.Unit.Infrastructure.Resilience;

/// <summary>
/// Integration tests for Polly-based retry mechanisms across all external services.
/// Tests connection resilience, operation resilience, and failure scenarios without real external dependencies.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Integration", "Resilience")]
public class PollyRetryIntegrationTests
{
    #region Connection Resilience Tests

    [Fact]
    public async Task KnxConnection_WithTransientFailures_ShouldRetryAndSucceed()
    {
        // Arrange
        var config = CreateResilienceConfig(maxRetries: 3, retryDelayMs: 100);
        var pipeline = ResiliencePolicyFactory.CreateConnectionPipeline(config, "KNX");

        var mockKnxClient = new Mock<IKnxClient>();
        var callCount = 0;

        mockKnxClient
            .Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts
                {
                    throw new InvalidOperationException("KNX gateway temporarily unavailable");
                }

                return Task.CompletedTask; // Succeed on 3rd attempt
            });

        // Act
        var result = await pipeline.ExecuteAsync(async _ =>
        {
            await mockKnxClient.Object.ConnectAsync(CancellationToken.None);
            return "Connected";
        });

        // Assert
        result.Should().Be("Connected");
        callCount.Should().Be(3, "should retry twice before succeeding");
        mockKnxClient.Verify(x => x.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task MqttConnection_WithNetworkFailures_ShouldRetryWithExponentialBackoff()
    {
        // Arrange
        var config = CreateResilienceConfig(maxRetries: 4, retryDelayMs: 50, backoffType: "Exponential");
        var pipeline = ResiliencePolicyFactory.CreateConnectionPipeline(config, "MQTT");

        var mockMqttClient = new Mock<IMqttClient>();
        var callCount = 0;
        var callTimestamps = new List<DateTime>();

        mockMqttClient
            .Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                callTimestamps.Add(DateTime.UtcNow);

                if (callCount <= 3) // Fail first 3 attempts
                {
                    throw new HttpRequestException("Network unreachable");
                }

                return Task.CompletedTask; // Succeed on 4th attempt
            });

        // Act
        var startTime = DateTime.UtcNow;
        await pipeline.ExecuteAsync(async _ =>
        {
            await mockMqttClient.Object.ConnectAsync(CancellationToken.None);
        });
        var endTime = DateTime.UtcNow;

        // Assert
        callCount.Should().Be(4, "should retry 3 times before succeeding");
        callTimestamps.Should().HaveCount(4);

        // Verify exponential backoff (approximate timing)
        var totalDuration = endTime - startTime;
        totalDuration
            .Should()
            .BeGreaterThan(TimeSpan.FromMilliseconds(150), "should have exponential delays between retries");
    }

    [Fact]
    public async Task SnapcastConnection_WithMaxRetriesExceeded_ShouldThrowException()
    {
        // Arrange
        var config = CreateResilienceConfig(maxRetries: 2, retryDelayMs: 10);
        var pipeline = ResiliencePolicyFactory.CreateConnectionPipeline(config, "Snapcast");

        var mockSnapcastClient = new Mock<ISnapcastClient>();
        var callCount = 0;

        mockSnapcastClient
            .Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                throw new SocketException((int)SocketError.ConnectionRefused, "Connection refused");
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SocketException>(() =>
            pipeline
                .ExecuteAsync(async _ =>
                {
                    await mockSnapcastClient.Object.ConnectAsync(CancellationToken.None);
                })
                .AsTask()
        );

        exception.Message.Should().Contain("Connection refused");
        callCount.Should().Be(3, "should attempt initial call + 2 retries");
    }

    #endregion

    #region Operation Resilience Tests

    [Fact]
    public async Task SubsonicOperation_WithTimeoutAndRetry_ShouldHandleTransientErrors()
    {
        // Arrange
        var config = CreateResilienceConfig(maxRetries: 3, retryDelayMs: 50, timeoutSeconds: 1);
        var pipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "Subsonic");

        var mockSubsonicClient = new Mock<ISubsonicClient>();
        var callCount = 0;

        mockSubsonicClient
            .Setup(x => x.GetPlaylistsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts
                {
                    throw new HttpRequestException(
                        "Server temporarily overloaded",
                        null,
                        HttpStatusCode.ServiceUnavailable
                    );
                }

                return Task.FromResult(new List<string> { "Playlist1", "Playlist2" });
            });

        // Act
        var result = await pipeline.ExecuteAsync(async _ =>
        {
            return await mockSubsonicClient.Object.GetPlaylistsAsync(CancellationToken.None);
        });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        callCount.Should().Be(3, "should retry twice before succeeding");
    }

    [Fact]
    public async Task KnxOperation_WithJitterEnabled_ShouldVaryRetryDelays()
    {
        // Arrange
        var config = CreateResilienceConfig(maxRetries: 3, retryDelayMs: 100, useJitter: true, jitterPercentage: 50);
        var pipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "KNX");

        var mockKnxClient = new Mock<IKnxClient>();
        var callCount = 0;
        var callTimestamps = new List<DateTime>();

        mockKnxClient
            .Setup(x => x.SendCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                callTimestamps.Add(DateTime.UtcNow);

                if (callCount <= 2) // Fail first 2 attempts
                {
                    throw new TimeoutException("KNX command timeout");
                }

                return Task.CompletedTask; // Succeed on 3rd attempt
            });

        // Act
        await pipeline.ExecuteAsync(async _ =>
        {
            await mockKnxClient.Object.SendCommandAsync("1/2/3", CancellationToken.None);
        });

        // Assert
        callCount.Should().Be(3);
        callTimestamps.Should().HaveCount(3);

        // Verify jitter is applied (delays should vary)
        if (callTimestamps.Count >= 2)
        {
            var delay1 = callTimestamps[1] - callTimestamps[0];
            var delay2 = callTimestamps[2] - callTimestamps[1];

            // With jitter, delays should not be exactly the same
            Math.Abs(delay1.TotalMilliseconds - delay2.TotalMilliseconds)
                .Should()
                .BeGreaterThan(5, "jitter should cause delay variation");
        }
    }

    #endregion

    #region Timeout Handling Tests

    [Fact]
    public async Task MqttOperation_WithTimeout_ShouldCancelLongRunningOperations()
    {
        // Arrange
        var config = CreateResilienceConfig(
            maxRetries: 1,
            retryDelayMs: 10,
            timeoutSeconds: 1 // 1 second timeout
        );
        var pipeline = ResiliencePolicyFactory.CreateOperationPipeline(config, "MQTT");

        var mockMqttClient = new Mock<IMqttClient>();
        mockMqttClient
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(
                async (string topic, string payload, CancellationToken ct) =>
                {
                    // Simulate long-running operation
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            pipeline
                .ExecuteAsync(async ct =>
                {
                    await mockMqttClient.Object.PublishAsync("test/topic", "payload", ct);
                })
                .AsTask()
        );

        exception.Should().NotBeNull();
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void ResiliencePolicyFactory_WithInvalidConfig_ShouldNormalizeValues()
    {
        // Arrange
        var invalidConfig = new PolicyConfig
        {
            MaxRetries = -5, // Invalid: negative
            RetryDelayMs = 50, // Invalid: too low
            BackoffType = "InvalidType", // Invalid: unknown type
            TimeoutSeconds = 500, // Invalid: too high
            JitterPercentage = 150, // Invalid: over 100%
        };

        // Act
        var normalizedConfig = ResiliencePolicyFactory.ValidateAndNormalize(invalidConfig);

        // Assert
        normalizedConfig.MaxRetries.Should().Be(0, "negative retries should be normalized to 0");
        normalizedConfig.RetryDelayMs.Should().Be(100, "too low delay should be normalized to minimum");
        normalizedConfig.BackoffType.Should().Be("Exponential", "invalid backoff should default to Exponential");
        normalizedConfig.TimeoutSeconds.Should().Be(300, "too high timeout should be capped at maximum");
        normalizedConfig.JitterPercentage.Should().Be(100, "over 100% jitter should be capped at 100%");
    }

    #endregion

    #region Multi-Service Scenario Tests

    [Fact]
    public async Task MultiServiceFailover_WhenPrimaryFails_ShouldRetryAndFallback()
    {
        // Arrange - Simulate a scenario where Snapcast fails but MQTT succeeds
        var snapcastConfig = CreateResilienceConfig(maxRetries: 2, retryDelayMs: 50);
        var mqttConfig = CreateResilienceConfig(maxRetries: 3, retryDelayMs: 30);

        var snapcastPipeline = ResiliencePolicyFactory.CreateOperationPipeline(snapcastConfig, "Snapcast");
        var mqttPipeline = ResiliencePolicyFactory.CreateOperationPipeline(mqttConfig, "MQTT");

        var mockSnapcastClient = new Mock<ISnapcastClient>();
        var mockMqttClient = new Mock<IMqttClient>();

        // Snapcast always fails
        mockSnapcastClient
            .Setup(x => x.SetVolumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Snapcast server unreachable"));

        // MQTT succeeds after 1 retry
        var mqttCallCount = 0;
        mockMqttClient
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                mqttCallCount++;
                if (mqttCallCount == 1)
                {
                    throw new HttpRequestException("MQTT broker busy");
                }

                return Task.CompletedTask;
            });

        // Act
        var snapcastResult = await TryExecuteAsync<bool>(() =>
            snapcastPipeline
                .ExecuteAsync(async _ =>
                {
                    await mockSnapcastClient.Object.SetVolumeAsync("client1", 50, CancellationToken.None);
                    return true;
                })
                .AsTask()
        );

        var mqttResult = await TryExecuteAsync<bool>(() =>
            mqttPipeline
                .ExecuteAsync(async _ =>
                {
                    await mockMqttClient.Object.PublishAsync("volume/client1", "50", CancellationToken.None);
                    return true;
                })
                .AsTask()
        );

        // Assert
        snapcastResult.Success.Should().BeFalse("Snapcast should fail after retries");
        mqttResult.Success.Should().BeTrue("MQTT should succeed after retry");
        mqttCallCount.Should().Be(2, "MQTT should retry once before succeeding");
    }

    #endregion

    #region Helper Methods

    private static ResilienceConfig CreateResilienceConfig(
        int maxRetries = 3,
        int retryDelayMs = 1000,
        string backoffType = "Exponential",
        bool useJitter = false,
        int timeoutSeconds = 30,
        int jitterPercentage = 25
    )
    {
        var policyConfig = new PolicyConfig
        {
            MaxRetries = maxRetries,
            RetryDelayMs = retryDelayMs,
            BackoffType = backoffType,
            UseJitter = useJitter,
            TimeoutSeconds = timeoutSeconds,
            JitterPercentage = jitterPercentage,
        };

        return new ResilienceConfig { Connection = policyConfig, Operation = policyConfig };
    }

    private static async Task<(bool Success, Exception? Exception)> TryExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            await operation();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    #endregion

    #region Mock Interfaces

    public interface IKnxClient
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SendCommandAsync(string address, CancellationToken cancellationToken);
    }

    public interface IMqttClient
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task PublishAsync(string topic, string payload, CancellationToken cancellationToken);
    }

    public interface ISnapcastClient
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SetVolumeAsync(string clientId, int volume, CancellationToken cancellationToken);
    }

    public interface ISubsonicClient
    {
        Task<List<string>> GetPlaylistsAsync(CancellationToken cancellationToken);
    }

    #endregion
}
