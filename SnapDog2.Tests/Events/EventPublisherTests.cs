using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the InMemoryEventPublisher class.
/// Tests event publishing, subscription, and async handling capabilities.
/// </summary>
public class EventPublisherTests : IDisposable
{
    private readonly Mock<ILogger<InMemoryEventPublisher>> _mockLogger;
    private readonly InMemoryEventPublisher _eventPublisher;

    public EventPublisherTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryEventPublisher>>();
        _eventPublisher = new InMemoryEventPublisher(_mockLogger.Object);
    }

    public void Dispose()
    {
        _eventPublisher?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateEventPublisher()
    {
        // Arrange & Act
        using var eventPublisher = new InMemoryEventPublisher(_mockLogger.Object);

        // Assert
        Assert.NotNull(eventPublisher);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InMemoryEventPublisher(null!));
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldPublishSuccessfully()
    {
        // Arrange
        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act & Assert - Should not throw
        await _eventPublisher.PublishAsync(testEvent);
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _eventPublisher.PublishAsync(null!));
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEvents_ShouldPublishAllEvents()
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

        // Act & Assert - Should not throw
        await _eventPublisher.PublishAsync(events);
    }

    [Fact]
    public async Task PublishAsync_WithEmptyEventCollection_ShouldNotThrow()
    {
        // Arrange
        var events = new List<IDomainEvent>();

        // Act & Assert - Should not throw
        await _eventPublisher.PublishAsync(events);
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
    public async Task PublishAndWaitAsync_WithValidEvent_ShouldPublishAndWait()
    {
        // Arrange
        var testEvent = VolumeChangedEvent.CreateForClient("test-client", "Test Client", 50, 75);

        // Act & Assert - Should not throw
        await _eventPublisher.PublishAndWaitAsync(testEvent);
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _eventPublisher.PublishAndWaitAsync(null!));
    }

    [Fact]
    public async Task Subscribe_WithValidHandler_ShouldSubscribeSuccessfully()
    {
        // Arrange
        var eventsReceived = new List<ClientConnectedEvent>();

        void EventHandler(ClientConnectedEvent evt)
        {
            eventsReceived.Add(evt);
        }

        // Act
        _eventPublisher.Subscribe<ClientConnectedEvent>(EventHandler);

        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        await _eventPublisher.PublishAsync(testEvent);

        // Give some time for async processing
        await Task.Delay(100);

        // Assert
        Assert.Single(eventsReceived);
        Assert.Equal("test-client", eventsReceived[0].ClientId);
    }

    [Fact]
    public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _eventPublisher.Subscribe<ClientConnectedEvent>(null!));
    }

    [Fact]
    public async Task Unsubscribe_WithValidHandler_ShouldUnsubscribeSuccessfully()
    {
        // Arrange
        var eventsReceived = new List<ClientConnectedEvent>();

        void EventHandler(ClientConnectedEvent evt)
        {
            eventsReceived.Add(evt);
        }

        _eventPublisher.Subscribe<ClientConnectedEvent>(EventHandler);

        // Act
        _eventPublisher.Unsubscribe<ClientConnectedEvent>(EventHandler);

        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        await _eventPublisher.PublishAsync(testEvent);
        await Task.Delay(100);

        // Assert
        Assert.Empty(eventsReceived);
    }

    [Fact]
    public void Unsubscribe_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _eventPublisher.Unsubscribe<ClientConnectedEvent>(null!));
    }

    [Fact]
    public async Task EventPublisher_WithMultipleSubscribers_ShouldNotifyAllSubscribers()
    {
        // Arrange
        var subscriber1Events = new List<VolumeChangedEvent>();
        var subscriber2Events = new List<VolumeChangedEvent>();

        void Handler1(VolumeChangedEvent evt) => subscriber1Events.Add(evt);
        void Handler2(VolumeChangedEvent evt) => subscriber2Events.Add(evt);

        _eventPublisher.Subscribe<VolumeChangedEvent>(Handler1);
        _eventPublisher.Subscribe<VolumeChangedEvent>(Handler2);

        var testEvent = VolumeChangedEvent.CreateForClient("test-client", "Test Client", 50, 75);

        // Act
        await _eventPublisher.PublishAsync(testEvent);
        await Task.Delay(100);

        // Assert
        Assert.Single(subscriber1Events);
        Assert.Single(subscriber2Events);
        Assert.Equal("test-client", subscriber1Events[0].EntityId);
        Assert.Equal("test-client", subscriber2Events[0].EntityId);
    }

    [Fact]
    public async Task EventPublisher_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var eventPublisher = new InMemoryEventPublisher(_mockLogger.Object);
        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        eventPublisher.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => eventPublisher.PublishAsync(testEvent));
    }

    [Fact]
    public void EventPublisher_DoubleDispose_ShouldNotThrow()
    {
        // Arrange
        var eventPublisher = new InMemoryEventPublisher(_mockLogger.Object);

        // Act & Assert - Should not throw
        eventPublisher.Dispose();
        eventPublisher.Dispose();
    }

    [Fact]
    public async Task EventPublisher_ConcurrentPublishing_ShouldHandleCorrectly()
    {
        // Arrange
        var eventsReceived = new List<ClientConnectedEvent>();
        var lockObject = new object();

        void EventHandler(ClientConnectedEvent evt)
        {
            lock (lockObject)
            {
                eventsReceived.Add(evt);
            }
        }

        _eventPublisher.Subscribe<ClientConnectedEvent>(EventHandler);

        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int clientIndex = i;
            tasks.Add(
                Task.Run(async () =>
                {
                    var evt = ClientConnectedEvent.Create(
                        $"client-{clientIndex}",
                        $"Client {clientIndex}",
                        new MacAddress($"AA:BB:CC:DD:EE:{clientIndex:X2}"),
                        new IpAddress($"192.168.1.{100 + clientIndex}")
                    );
                    await _eventPublisher.PublishAsync(evt);
                })
            );
        }

        // Act
        await Task.WhenAll(tasks);
        await Task.Delay(200); // Allow time for event processing

        // Assert
        Assert.Equal(10, eventsReceived.Count);
        Assert.Equal(10, eventsReceived.Select(e => e.ClientId).Distinct().Count());
    }

    [Fact]
    public async Task EventPublisher_WithEventCorrelation_ShouldMaintainCorrelationIds()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var eventsReceived = new List<ClientConnectedEvent>();

        void EventHandler(ClientConnectedEvent evt)
        {
            eventsReceived.Add(evt);
        }

        _eventPublisher.Subscribe<ClientConnectedEvent>(EventHandler);

        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            correlationId: correlationId
        );

        // Act
        await _eventPublisher.PublishAsync(testEvent);
        await Task.Delay(100);

        // Assert
        Assert.Single(eventsReceived);
        Assert.Equal(correlationId, eventsReceived[0].CorrelationId);
    }

    [Fact]
    public async Task EventPublisher_WithExceptionInHandler_ShouldContinueProcessing()
    {
        // Arrange
        var successfulEvents = new List<ClientConnectedEvent>();
        var exceptionThrown = false;

        void ThrowingHandler(ClientConnectedEvent evt)
        {
            exceptionThrown = true;
            throw new InvalidOperationException("Test exception");
        }

        void SuccessfulHandler(ClientConnectedEvent evt)
        {
            successfulEvents.Add(evt);
        }

        _eventPublisher.Subscribe<ClientConnectedEvent>(ThrowingHandler);
        _eventPublisher.Subscribe<ClientConnectedEvent>(SuccessfulHandler);

        var testEvent = ClientConnectedEvent.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act
        await _eventPublisher.PublishAsync(testEvent);
        await Task.Delay(100);

        // Assert
        Assert.True(exceptionThrown);
        Assert.Single(successfulEvents); // Successful handler should still receive the event
    }
}
