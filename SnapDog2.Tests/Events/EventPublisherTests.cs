using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the InMemoryEventPublisher class.
/// Tests event publishing capabilities through MediatR.
/// </summary>
public class EventPublisherTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<InMemoryEventPublisher>> _mockLogger;
    private readonly InMemoryEventPublisher _eventPublisher;

    public EventPublisherTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<InMemoryEventPublisher>>();
        _eventPublisher = new InMemoryEventPublisher(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateEventPublisher()
    {
        // Arrange & Act
        var eventPublisher = new InMemoryEventPublisher(_mockMediator.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(eventPublisher);
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InMemoryEventPublisher(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InMemoryEventPublisher(_mockMediator.Object, null!));
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldCallMediatorPublish()
    {
        // Arrange
        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(testEvent);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _eventPublisher.PublishAsync((IDomainEvent)null!));
    }

    [Fact]
    public async Task PublishAsync_WithEventCollection_ShouldCallMediatorForEachEvent()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            ClientConnectedEvent.Create(
                "client-1",
                "Client 1",
                new MacAddress("AA:BB:CC:DD:EE:01"),
                new IpAddress("192.168.1.101")
            ),
            ClientConnectedEvent.Create(
                "client-2",
                "Client 2",
                new MacAddress("AA:BB:CC:DD:EE:02"),
                new IpAddress("192.168.1.102")
            ),
        };

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(events);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_WithEmptyEventCollection_ShouldNotCallMediator()
    {
        // Arrange
        var events = new List<IDomainEvent>();

        // Act
        await _eventPublisher.PublishAsync(events);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WithNullEventCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _eventPublisher.PublishAsync((IEnumerable<IDomainEvent>)null!)
        );
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithValidEvent_ShouldCallMediatorPublish()
    {
        // Arrange
        var testEvent = VolumeChangedEvent.CreateForClient("test-client", "Test Client", 50, 75);

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAndWaitAsync(testEvent);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _eventPublisher.PublishAndWaitAsync((IDomainEvent)null!));
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithEventCollection_ShouldCallMediatorForEachEvent()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            VolumeChangedEvent.CreateForClient("client-1", "Client 1", 50, 75),
            VolumeChangedEvent.CreateForClient("client-2", "Client 2", 60, 80),
        };

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAndWaitAsync(events);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithNullEventCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _eventPublisher.PublishAndWaitAsync((IEnumerable<IDomainEvent>)null!)
        );
    }

    [Fact]
    public async Task PublishAsync_WhenMediatorThrowsException_ShouldPropagateException()
    {
        // Arrange
        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        var expectedException = new InvalidOperationException("Test exception");
        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _eventPublisher.PublishAsync(testEvent)
        );

        Assert.Equal(expectedException.Message, actualException.Message);
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithCollectionAndException_ShouldThrowAggregateException()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            ClientConnectedEvent.Create(
                "client-1",
                "Client 1",
                new MacAddress("AA:BB:CC:DD:EE:01"),
                new IpAddress("192.168.1.101")
            ),
            ClientConnectedEvent.Create(
                "client-2",
                "Client 2",
                new MacAddress("AA:BB:CC:DD:EE:02"),
                new IpAddress("192.168.1.102")
            ),
        };

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => _eventPublisher.PublishAndWaitAsync(events));
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassTokenToMediator()
    {
        // Arrange
        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        var cancellationToken = new CancellationToken();
        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(testEvent, cancellationToken);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), cancellationToken), Times.Once);
    }
}
