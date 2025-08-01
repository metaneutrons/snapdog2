using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Infrastructure.Services.Models;
using Xunit;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for Snapcast service functionality.
/// Tests service connectivity, error handling, and resilience patterns with mocked external dependencies.
/// </summary>
[Trait("Category", "Integration")]
public class SnapcastServiceIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<SnapcastService>> _mockLogger;
    private readonly Mock<IOptions<SnapDog2.Core.Configuration.SnapcastConfiguration>> _mockOptions;
    private readonly SnapcastService _snapcastService;
    private readonly ServiceProvider _serviceProvider;

    public SnapcastServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        var mockMediator = new Mock<MediatR.IMediator>();

        // Setup logging
        _mockLogger = new Mock<ILogger<SnapcastService>>();
        services.AddSingleton(_mockLogger.Object);

        // Setup configuration for testing
        var snapcastConfig = new SnapDog2.Core.Configuration.SnapcastConfiguration
        {
            Host = "localhost",
            Port = 1705,
            TimeoutSeconds = 5,
            ReconnectIntervalSeconds = 1,
            AutoReconnect = false, // Disable for testing
        };

        _mockOptions = new Mock<IOptions<SnapDog2.Core.Configuration.SnapcastConfiguration>>();
        _mockOptions.Setup(static x => x.Value).Returns(snapcastConfig);

        services.AddSingleton(_mockOptions.Object);
        services.AddSingleton(mockMediator.Object);

        _serviceProvider = services.BuildServiceProvider();
        _snapcastService = new SnapcastService(_mockOptions.Object, _mockLogger.Object, mockMediator.Object);
    }

    public void Dispose()
    {
        _snapcastService?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task IsServerAvailableAsync_WithUnreachableServer_ShouldReturnFalse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var isAvailable = await _snapcastService.IsServerAvailableAsync(cancellationTokenSource.Token);

        // Assert
        Assert.False(isAvailable);

        // Verify logging
        _mockLogger.Verify(
            static x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        static (v, t) => v.ToString()!.Contains("Snapcast server availability check failed")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task GetServerStatusAsync_WithUnreachableServer_ShouldThrowException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _snapcastService.GetServerStatusAsync(cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task GetGroupsAsync_WithUnreachableServer_ShouldThrowException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _snapcastService.GetGroupsAsync(cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task GetClientsAsync_WithUnreachableServer_ShouldThrowException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _snapcastService.GetClientsAsync(cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task SetClientVolumeAsync_WithUnreachableServer_ShouldThrowException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _snapcastService.SetClientVolumeAsync("test-client", 50, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task SetClientMuteAsync_WithUnreachableServer_ShouldThrowException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _snapcastService.SetClientMuteAsync("test-client", true, cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task SetGroupStreamAsync_WithUnreachableServer_ShouldThrowException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _snapcastService.SetGroupStreamAsync("test-group", "test-stream", cancellationTokenSource.Token)
        );
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new SnapcastService(null!, _mockLogger.Object, new Mock<MediatR.IMediator>().Object)
        );
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new SnapcastService(_mockOptions.Object, null!, new Mock<MediatR.IMediator>().Object)
        );
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SnapcastService(_mockOptions.Object, _mockLogger.Object, null!));
    }

    [Fact]
    public async Task CancellationToken_ShouldBePropagatedCorrectly()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _snapcastService.IsServerAvailableAsync(cancellationTokenSource.Token)
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
                        await _snapcastService.IsServerAvailableAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception)
                    {
                        // Expected for unreachable server
                    }
                })
            );
        }

        // Assert - Should complete without deadlocks or resource issues
        await Task.WhenAll(tasks);
        Assert.True(tasks.All(t => t.IsCompleted));
    }

    [Fact]
    public async Task ServiceDisposal_ShouldCleanupResourcesProperly()
    {
        // Arrange
        var config = new SnapDog2.Core.Configuration.SnapcastConfiguration
        {
            Host = "localhost",
            Port = 1705,
            TimeoutSeconds = 1,
        };
        var mockOptions = new Mock<IOptions<SnapDog2.Core.Configuration.SnapcastConfiguration>>();
        mockOptions.Setup(static x => x.Value).Returns(config);

        var service = new SnapcastService(mockOptions.Object, _mockLogger.Object, new Mock<MediatR.IMediator>().Object);

        // Act
        await service.DisposeAsync();

        // Assert - Should not throw when disposed
        await service.DisposeAsync(); // Double disposal should be safe
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SetClientVolumeAsync_WithInvalidClientId_ShouldThrowArgumentException(string? clientId)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>( // Changed from ArgumentNullException
            () => _snapcastService.SetClientVolumeAsync(clientId!, 50, cancellationTokenSource.Token)
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public async Task SetClientVolumeAsync_WithInvalidVolume_ShouldThrowArgumentOutOfRangeException(int volume)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _snapcastService.SetClientVolumeAsync("test-client", volume, cancellationTokenSource.Token)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SetGroupStreamAsync_WithInvalidGroupId_ShouldThrowArgumentException(string? groupId)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>( // Changed from ArgumentNullException
            () => _snapcastService.SetGroupStreamAsync(groupId!, "test-stream", cancellationTokenSource.Token)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SetGroupStreamAsync_WithInvalidStreamId_ShouldThrowArgumentException(string? streamId)
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>( // Changed from ArgumentNullException
            () => _snapcastService.SetGroupStreamAsync("test-group", streamId!, cancellationTokenSource.Token)
        );
    }
}
