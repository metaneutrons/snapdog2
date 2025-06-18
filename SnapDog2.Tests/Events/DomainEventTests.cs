using SnapDog2.Core.Events;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the DomainEvent base class.
/// Tests base event functionality, correlation IDs, and timestamps.
/// </summary>
public class DomainEventTests
{
    /// <summary>
    /// Test implementation of DomainEvent for testing purposes.
    /// </summary>
    private sealed record TestDomainEvent : DomainEvent
    {
        public required string TestProperty { get; init; }

        public TestDomainEvent() { }

        public TestDomainEvent(string? correlationId)
            : base(correlationId) { }

        public static TestDomainEvent Create(string testProperty, string? correlationId = null)
        {
            return new TestDomainEvent(correlationId) { TestProperty = testProperty };
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var domainEvent = TestDomainEvent.Create("test-value");

        // Assert
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
        Assert.True(domainEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(domainEvent.OccurredAt >= DateTime.UtcNow.AddSeconds(-1)); // Within last second
        Assert.Null(domainEvent.CorrelationId);
        Assert.Equal("test-value", domainEvent.TestProperty);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var domainEvent = TestDomainEvent.Create("test-value", correlationId);

        // Assert
        Assert.Equal(correlationId, domainEvent.CorrelationId);
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
        Assert.True(domainEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void EventId_ShouldBeUniqueForEachInstance()
    {
        // Act
        var event1 = TestDomainEvent.Create("test-1");
        var event2 = TestDomainEvent.Create("test-2");

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    [Fact]
    public void OccurredAt_ShouldBeSetToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = TestDomainEvent.Create("test-value");

        // Arrange
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(domainEvent.OccurredAt >= beforeCreation);
        Assert.True(domainEvent.OccurredAt <= afterCreation);
        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredAt.Kind);
    }

    [Fact]
    public void CorrelationId_ShouldAcceptNullValue()
    {
        // Act
        var domainEvent = TestDomainEvent.Create("test-value", null);

        // Assert
        Assert.Null(domainEvent.CorrelationId);
    }

    [Fact]
    public void CorrelationId_ShouldAcceptEmptyString()
    {
        // Act
        var domainEvent = TestDomainEvent.Create("test-value", "");

        // Assert
        Assert.Equal("", domainEvent.CorrelationId);
    }

    [Fact]
    public void CorrelationId_ShouldAcceptValidGuid()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var domainEvent = TestDomainEvent.Create("test-value", correlationId);

        // Assert
        Assert.Equal(correlationId, domainEvent.CorrelationId);
    }

    [Fact]
    public void CorrelationId_ShouldAcceptCustomString()
    {
        // Arrange
        var correlationId = "custom-correlation-id-123";

        // Act
        var domainEvent = TestDomainEvent.Create("test-value", correlationId);

        // Assert
        Assert.Equal(correlationId, domainEvent.CorrelationId);
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var correlationId = "test-correlation";
        var event1 = TestDomainEvent.Create("test-value", correlationId);
        var event2 = TestDomainEvent.Create("test-value", correlationId);

        // Act & Assert
        Assert.NotEqual(event1, event2); // Different EventIds and OccurredAt times
        Assert.Equal(event1.TestProperty, event2.TestProperty);
        Assert.Equal(event1.CorrelationId, event2.CorrelationId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var domainEvent = TestDomainEvent.Create("test-value");

        // Act & Assert
        Assert.Equal(domainEvent, domainEvent);
    }

    [Fact]
    public void RecordEquality_WithIdenticalData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var testProperty = "test-value";
        var correlationId = "test-correlation";

        // Create events with identical data
        var event1 = new TestDomainEvent(correlationId) { TestProperty = testProperty };
        var event2 = new TestDomainEvent(correlationId) { TestProperty = testProperty };

        // Act & Assert
        // They should not be equal because EventId is auto-generated and different
        Assert.NotEqual(event1, event2);
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    [Fact]
    public void ToString_ShouldContainEventInformation()
    {
        // Arrange
        var correlationId = "test-correlation";
        var domainEvent = TestDomainEvent.Create("test-value", correlationId);

        // Act
        var stringRepresentation = domainEvent.ToString();

        // Assert
        Assert.Contains("TestDomainEvent", stringRepresentation);
        Assert.Contains("test-value", stringRepresentation);
        Assert.Contains(correlationId, stringRepresentation);
    }

    [Fact]
    public void GetHashCode_ShouldBeDifferentForDifferentEvents()
    {
        // Arrange
        var event1 = TestDomainEvent.Create("test-1");
        var event2 = TestDomainEvent.Create("test-2");

        // Act
        var hash1 = event1.GetHashCode();
        var hash2 = event2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void EventProperties_ShouldBeImmutable()
    {
        // Arrange
        var originalCorrelationId = "original-correlation";
        var domainEvent = TestDomainEvent.Create("test-value", originalCorrelationId);

        // Store original values
        var originalEventId = domainEvent.EventId;
        var originalOccurredAt = domainEvent.OccurredAt;
        var originalCorrelation = domainEvent.CorrelationId;

        // Act - Create a new instance with modified correlation (using with expression)
        var modifiedEvent = domainEvent with
        {
            CorrelationId = "modified-correlation",
        };

        // Assert - Original event should remain unchanged
        Assert.Equal(originalEventId, domainEvent.EventId);
        Assert.Equal(originalOccurredAt, domainEvent.OccurredAt);
        Assert.Equal(originalCorrelation, domainEvent.CorrelationId);

        // Modified event should have changes
        Assert.Equal(originalEventId, modifiedEvent.EventId); // EventId stays the same with 'with'
        Assert.Equal(originalOccurredAt, modifiedEvent.OccurredAt); // OccurredAt stays the same with 'with'
        Assert.Equal("modified-correlation", modifiedEvent.CorrelationId);
    }

    [Fact]
    public void MultipleEvents_ShouldHaveChronologicalOrder()
    {
        // Arrange
        var events = new List<TestDomainEvent>();

        // Act - Create multiple events with small delays
        for (int i = 0; i < 5; i++)
        {
            events.Add(TestDomainEvent.Create($"test-{i}"));
            Thread.Sleep(1); // Small delay to ensure different timestamps
        }

        // Assert - Events should be in chronological order
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(
                events[i].OccurredAt >= events[i - 1].OccurredAt,
                $"Event {i} should have occurred after event {i - 1}"
            );
        }
    }

    [Fact]
    public void CorrelationId_MaxLength_ShouldBeAccepted()
    {
        // Arrange - Create a very long correlation ID
        var longCorrelationId = new string('x', 1000);

        // Act
        var domainEvent = TestDomainEvent.Create("test-value", longCorrelationId);

        // Assert
        Assert.Equal(longCorrelationId, domainEvent.CorrelationId);
    }

    [Fact]
    public void IDomainEvent_Interface_ShouldBeImplemented()
    {
        // Arrange
        var domainEvent = TestDomainEvent.Create("test-value");

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(domainEvent);

        // Verify interface properties are accessible
        IDomainEvent interfaceEvent = domainEvent;
        Assert.Equal(domainEvent.EventId, interfaceEvent.EventId);
        Assert.Equal(domainEvent.OccurredAt, interfaceEvent.OccurredAt);
        Assert.Equal(domainEvent.CorrelationId, interfaceEvent.CorrelationId);
    }
}
