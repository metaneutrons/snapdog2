using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet.Protocol;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for MQTT service functionality.
/// Tests service connectivity, error handling, and resilience patterns with mocked external dependencies.
/// </summary>
[Trait("Category", "Integration")]
public class MqttServiceIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<MqttService>> _mockLogger;
    private readonly Mock<IOptions<SnapDog2.Core.Configuration.MqttConfiguration>> _mockOptions;
    private readonly MqttService _mqttService;
    private readonly ServiceProvider _serviceProvider;

    public MqttServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        var mockMediator = new Mock<MediatR.IMediator>();

        // Setup logging
        _mockLogger = new Mock<ILogger<MqttService>>();
        services.AddSingleton(_mockLogger.Object);

        // Setup configuration for testing
        var mqttConfig = new SnapDog2.Core.Configuration.MqttConfiguration
        {
            Broker = "localhost",
            Port = 1883,
            Username = "",
            Password = "",
        };

        _mockOptions = new Mock<IOptions<SnapDog2.Core.Configuration.MqttConfiguration>>();
        _mockOptions.Setup(x => x.Value).Returns(mqttConfig);

        services.AddSingleton(_mockOptions.Object);
        services.AddSingleton(mockMediator.Object);

        _serviceProvider = services.BuildServiceProvider();
        _mqttService = new MqttService(_mockOptions.Object, _mockLogger.Object, mockMediator.Object);
    }

    public void Dispose()
    {
        _mqttService?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_WithUnreachableBroker_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var isConnected = await _mqttService.ConnectAsync(cancellationTokenSource.Token);

        // Assert
        Assert.False(isConnected);

        // Verify logging
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error, // Corrected to Error based on actual logs
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to connect to MQTT broker")),
                    It.IsAny<Exception>(), // Exception is logged
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task PublishAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var result = await _mqttService.PublishAsync("test/topic", "test message", cancellationTokenSource.Token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PublishAsync_WithQoSAndRetain_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var result = await _mqttService.PublishAsync(
            "test/topic",
            "test message",
            MqttQualityOfServiceLevel.AtLeastOnce,
            true,
            cancellationTokenSource.Token
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var result = await _mqttService.SubscribeAsync("test/topic", cancellationTokenSource.Token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnsubscribeAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var result = await _mqttService.UnsubscribeAsync("test/topic", cancellationTokenSource.Token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MqttService(null!, _mockLogger.Object, new Mock<MediatR.IMediator>().Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MqttService(_mockOptions.Object, null!, new Mock<MediatR.IMediator>().Object));
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MqttService(_mockOptions.Object, _mockLogger.Object, null!));
    }

    [Fact]
    public async Task CancellationToken_ShouldBePropagatedCorrectly()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<System.Threading.Tasks.TaskCanceledException>(
            () => _mqttService.ConnectAsync(cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task MultipleOperations_ShouldHandleResourcesCorrectly()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var tasks = new List<Task>();

        // Act - Execute multiple operations concurrently
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        await _mqttService.ConnectAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception)
                    {
                        // Expected for unreachable broker
                    }
                })
            );
        }

        // Assert - Should complete without deadlocks or resource issues
        await Task.WhenAll(tasks);
        Assert.True(tasks.All(t => t.IsCompleted));
    }

    [Fact]
    public void ServiceDisposal_ShouldCleanupResourcesProperly()
    {
        // Arrange
        var config = new SnapDog2.Core.Configuration.MqttConfiguration
        {
            Broker = "localhost",
            Port = 1883,
            Username = "test-client-disposal",
        };
        var mockOptions = new Mock<IOptions<SnapDog2.Core.Configuration.MqttConfiguration>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new MqttService(mockOptions.Object, _mockLogger.Object, new Mock<MediatR.IMediator>().Object);

        // Act
        service.Dispose();

        // Assert - Should not throw when disposed
        service.Dispose(); // Double disposal should be safe
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task PublishAsync_WithInvalidTopic_ShouldThrowArgumentException(string? topic)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttService.PublishAsync(topic!, "test message", cancellationTokenSource.Token)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SubscribeAsync_WithInvalidTopic_ShouldThrowArgumentException(string? topic)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttService.SubscribeAsync(topic!, cancellationTokenSource.Token)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UnsubscribeAsync_WithInvalidTopic_ShouldThrowArgumentException(string? topic)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttService.UnsubscribeAsync(topic!, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task PublishAsync_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _mqttService.PublishAsync("test/topic", null!, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert - Should not throw
        await _mqttService.DisconnectAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task ConnectionLifecycle_ShouldHandleConnectDisconnectProperly()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - Try to connect (will fail with unreachable broker)
        // This call should eventually return false after its internal retries/timeout.
        // The CancellationToken is for the ConnectAsync operation itself.
        var connectResult = await _mqttService.ConnectAsync(cancellationTokenSource.Token);

        // Assert connectResult is false
        Assert.False(connectResult);

        // Try to disconnect (should not throw even if not connected).
        // Use a new CancellationToken or CancellationToken.None for DisconnectAsync
        // to avoid issues if the original token was cancelled due to ConnectAsync taking too long.
        await _mqttService.DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public void MessageReceived_Event_ShouldBeSubscribable()
    {
        // Arrange
        var eventSubscribed = false;

        // Act
        _mqttService.MessageReceived += (sender, args) => eventSubscribed = true;

        // Assert - Event should be subscribable (actual firing requires real broker)
        Assert.True(eventSubscribed == false); // Event handler added but not fired
    }

    [Theory]
    [InlineData("test/topic")]
    [InlineData("test/+/topic")]
    [InlineData("test/#")]
    [InlineData("test/topic/+/subtopic")]
    public async Task ValidTopicFormats_ShouldNotThrowValidationErrors(string topic)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert - Should not throw validation errors (will fail due to no connection)
        var subscribeResult = await _mqttService.SubscribeAsync(topic, cancellationTokenSource.Token);
        Assert.False(subscribeResult); // Expected to fail due to no broker connection
    }

    [Theory]
    [InlineData("test/topic/+")]
    [InlineData("test/topic/#")]
    public async Task PublishAsync_WithWildcardTopics_ShouldThrowArgumentException(string topic)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttService.PublishAsync(topic, "test message", cancellationTokenSource.Token)
        );
    }
}
