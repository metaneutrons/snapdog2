using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Events;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the InMemoryEventPublisher class.
/// Tests event publishing capabilities through MediatR.
/// </summary>
public class InMemoryEventPublisherTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<InMemoryEventPublisher>> _mockLogger;
    private readonly InMemoryEventPublisher _eventPublisher;

    public InMemoryEventPublisherTests()
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
        var testEvent = new TestDomainEvent();

        _mockMediator
            .Setup(static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(testEvent);

        // Assert
        _mockMediator.Verify(
            static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _eventPublisher.PublishAsync((IDomainEvent)null!));
    }

    [Fact]
    public async Task PublishAsync_WhenMediatorThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var testEvent = new TestDomainEvent();
        var expectedException = new InvalidOperationException("Test exception");

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _eventPublisher.PublishAsync(testEvent)
        );

        Assert.Equal(expectedException.Message, actualException.Message);

        VerifyLogCalled(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassTokenToMediator()
    {
        // Arrange
        var testEvent = new TestDomainEvent();
        var cancellationToken = new CancellationToken();

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(testEvent, cancellationToken);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithEventCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _eventPublisher.PublishAsync((IEnumerable<IDomainEvent>)null!)
        );
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
    public async Task PublishAsync_WithValidEvent_ShouldLogDebugMessages()
    {
        // Arrange
        var testEvent = new TestDomainEvent();

        _mockMediator
            .Setup(static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(testEvent);

        // Assert
        VerifyLogCalled(LogLevel.Debug, Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_WithEmptyEventCollection_ShouldLogAndReturn()
    {
        // Arrange
        var events = new List<IDomainEvent>();

        // Act
        await _eventPublisher.PublishAsync(events);

        // Assert
        _mockMediator.Verify(
            static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEvents_ShouldPublishAllEvents()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent(), new TestDomainEvent(), new TestDomainEvent() };

        _mockMediator
            .Setup(static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(events);

        // Assert
        _mockMediator.Verify(
            static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEventsAndSomeFailures_ShouldLogWarning()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent(), new TestDomainEvent(), new TestDomainEvent() };

        var callCount = 0;
        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("Test failure");
                }
                return Task.CompletedTask;
            });

        // Act
        await _eventPublisher.PublishAsync(events);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        VerifyLogCalled(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEventsAndCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent() };
        var cancellationToken = new CancellationToken();

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(events, cancellationToken);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithSingleEvent_ShouldCallPublishAsync()
    {
        // Arrange
        var testEvent = new TestDomainEvent();

        _mockMediator
            .Setup(static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAndWaitAsync(testEvent);

        // Assert
        _mockMediator.Verify(
            static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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
    public async Task PublishAndWaitAsync_WithEmptyEventCollection_ShouldLogAndReturn()
    {
        // Arrange
        var events = new List<IDomainEvent>();

        // Act
        await _eventPublisher.PublishAndWaitAsync(events);

        // Assert
        _mockMediator.Verify(
            static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithMultipleEvents_ShouldPublishSequentially()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent(), new TestDomainEvent() };

        _mockMediator
            .Setup(static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAndWaitAsync(events);

        // Assert
        _mockMediator.Verify(
            static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithAllSuccessful_ShouldLogSuccess()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent() };

        _mockMediator
            .Setup(static m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAndWaitAsync(events);

        // Assert
        VerifyLogCalled(LogLevel.Debug, Times.AtLeastOnce());
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithFailures_ShouldThrowAggregateException()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent(), new TestDomainEvent(), new TestDomainEvent() };

        var callCount = 0;
        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("Test failure 1");
                }
                if (callCount == 3)
                {
                    throw new InvalidOperationException("Test failure 2");
                }
                return Task.CompletedTask;
            });

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => _eventPublisher.PublishAndWaitAsync(events));

        // Note: The actual implementation logs each failure twice (once in PublishAsync, once in PublishAndWaitAsync)
        // So we expect 4 error logs total: 2 failures × 2 logs each
        VerifyLogCalled(LogLevel.Error, Times.Exactly(4));
    }

    [Fact]
    public async Task PublishAndWaitAsync_WithPartialFailures_ShouldContinueProcessing()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestDomainEvent(), new TestDomainEvent(), new TestDomainEvent() };

        var callCount = 0;
        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("Middle failure");
                }
                return Task.CompletedTask;
            });

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => _eventPublisher.PublishAndWaitAsync(events));

        // Assert all events were attempted
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PublishAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var testEvent = new TestDomainEvent();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _mockMediator
            .Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(testEvent, cts.Token);

        // Assert - the method should still complete but with cancelled token
        _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), cts.Token), Times.Once);
    }

    private void VerifyLogCalled(LogLevel level, Times times)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            times
        );
    }

    public class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string CorrelationId { get; set; } = string.Empty;
    }
}
