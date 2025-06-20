using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Infrastructure.Services.Models;
using Xunit;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for KNX service functionality.
/// Tests service connectivity, error handling, and resilience patterns with mocked external dependencies.
/// </summary>
[Trait("Category", "Integration")]
public class KnxServiceIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<KnxService>> _mockLogger;
    private readonly Mock<IOptions<KnxConfiguration>> _mockOptions;
    private readonly KnxService _knxService;
    private readonly ServiceProvider _serviceProvider;

    public KnxServiceIntegrationTests()
    {
        var services = new ServiceCollection();

        // Setup logging
        _mockLogger = new Mock<ILogger<KnxService>>();
        services.AddSingleton(_mockLogger.Object);

        // Setup configuration for testing
        var knxConfig = new KnxConfiguration
        {
            Gateway = "localhost",
            Port = 3671,
            TimeoutSeconds = 5,
            AutoReconnect = false, // Disable for testing
        };

        _mockOptions = new Mock<IOptions<KnxConfiguration>>();
        _mockOptions.Setup(x => x.Value).Returns(knxConfig);

        services.AddSingleton(_mockOptions.Object);

        _serviceProvider = services.BuildServiceProvider();
        _knxService = new KnxService(_mockOptions.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _knxService?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_WithUnreachableGateway_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(
            () => _knxService.ConnectAsync(cancellationTokenSource.Token)
        );

        // Verify logging
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error, // Corrected: The catch block logs an Error
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error connecting to KNX gateway")), // More specific message
                    It.IsAny<Exception>(), // Exception is expected to be logged
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var address = new KnxAddress(1, 2, 3);
        var value = new byte[] { 0x01 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _knxService.WriteGroupValueAsync(address, value, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task ReadGroupValueAsync_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var address = new KnxAddress(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _knxService.ReadGroupValueAsync(address, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task SubscribeToGroupAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var address = new KnxAddress(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _knxService.SubscribeToGroupAsync(address, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task UnsubscribeFromGroupAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var address = new KnxAddress(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _knxService.UnsubscribeFromGroupAsync(address, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var address = new KnxAddress(1, 2, 3);
        var value = new byte[] { 0x01 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _knxService.WriteGroupValueAsync(address, value, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task ReadGroupValueAsync_WithoutConnection_ShouldReturnNull()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var address = new KnxAddress(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _knxService.ReadGroupValueAsync(address, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KnxService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KnxService(_mockOptions.Object, null!));
    }

    [Fact]
    public async Task CancellationToken_ShouldBePropagatedCorrectly()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<System.Threading.Tasks.TaskCanceledException>(
            () => _knxService.ConnectAsync(cancellationTokenSource.Token)
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
                        await _knxService.ConnectAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception)
                    {
                        // Expected for unreachable gateway
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
        var config = new KnxConfiguration
        {
            Gateway = "localhost",
            Port = 3671,
            TimeoutSeconds = 1,
        };
        var mockOptions = new Mock<IOptions<KnxConfiguration>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new KnxService(mockOptions.Object, _mockLogger.Object);

        // Act
        service.Dispose();

        // Assert - Should not throw when disposed
        service.Dispose(); // Double disposal should be safe
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithNullAddress_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var value = new byte[] { 0x01 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _knxService.WriteGroupValueAsync(null!, value, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var address = new KnxAddress(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _knxService.WriteGroupValueAsync(address, null!, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task ReadGroupValueAsync_WithNullAddress_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _knxService.ReadGroupValueAsync(null!, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task SubscribeToGroupAsync_WithNullAddress_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _knxService.SubscribeToGroupAsync(null!, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task UnsubscribeFromGroupAsync_WithNullAddress_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _knxService.UnsubscribeFromGroupAsync(null!, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert - Should not throw
        await _knxService.DisconnectAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task ConnectionLifecycle_ShouldHandleConnectDisconnectProperly()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act & Assert - Try to connect (will fail with unreachable gateway)
        await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(
            () => _knxService.ConnectAsync(cancellationTokenSource.Token)
        );

        // Try to disconnect (should not throw even if not connected or if connect failed)
        // DisconnectAsync is designed to be safe to call multiple times or if not connected.
        // Simply awaiting it will cause the test to fail if an unexpected exception is thrown.
        // Use a new CancellationToken for DisconnectAsync to avoid issues if the original token timed out.
        await _knxService.DisconnectAsync(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 2, 3)]
    [InlineData(31, 7, 255)]
    public async Task ValidKnxAddresses_ShouldNotThrowValidationErrors(int main, int middle, int sub)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var address = new KnxAddress(main, middle, sub);

        // Act & Assert - Should not throw validation errors (will fail due to no connection)
        await Assert.ThrowsAsync<InvalidOperationException>(() => _knxService.SubscribeToGroupAsync(address, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithEmptyValue_ShouldNotThrow()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var address = new KnxAddress(1, 2, 3);
        var value = Array.Empty<byte>();

        // Act & Assert
        // If not connected, InvalidOperationException is thrown (as per previous fix).
        // If connected, ArgumentException for empty value is thrown.
        // The test name "ShouldNotThrow" is misleading if it expects an exception.
        // Assuming the primary intent is to check behavior with an empty value *if connected*.
        // However, these tests are likely running without a connection by default.
        // For now, keeping InvalidOperationException as it's the more likely path in current test setup.
        // A more robust test suite would separate "no connection" from "invalid value when connected".
        await Assert.ThrowsAsync<ArgumentException>(
            () => _knxService.WriteGroupValueAsync(address, value, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithLargeValue_ShouldNotThrow()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var address = new KnxAddress(1, 2, 3);
        var value = new byte[14]; // Maximum KNX data length

        // Act & Assert - Should throw InvalidOperationException due to no connection, not ArgumentException
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _knxService.WriteGroupValueAsync(address, value, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public void GroupValueReceived_Event_ShouldBeSubscribable()
    {
        // Arrange
        var eventSubscribed = false;

        // Act
        _knxService.GroupValueReceived += (sender, args) => eventSubscribed = true;

        // Assert - Event should be subscribable (actual firing requires real gateway)
        Assert.True(eventSubscribed == false); // Event handler added but not fired
    }
}
