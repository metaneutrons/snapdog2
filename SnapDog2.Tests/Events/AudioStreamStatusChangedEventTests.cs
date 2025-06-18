using SnapDog2.Core.Events;
using SnapDog2.Core.Models.Enums;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the AudioStreamStatusChangedEvent class.
/// Tests stream status event creation, properties, and status transition logic.
/// </summary>
public class AudioStreamStatusChangedEventTests
{
    private const string ValidStreamId = "stream-1";
    private const string ValidStreamName = "Test Stream";
    private const string ValidStreamUrl = "http://example.com/stream";
    private const string ValidReason = "User action";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateEvent()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing
        );

        // Assert
        Assert.Equal(ValidStreamId, evt.StreamId);
        Assert.Equal(ValidStreamName, evt.StreamName);
        Assert.Equal(StreamStatus.Stopped, evt.PreviousStatus);
        Assert.Equal(StreamStatus.Playing, evt.NewStatus);
        Assert.Null(evt.StreamUrl);
        Assert.Null(evt.Reason);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Starting,
            StreamStatus.Playing,
            ValidStreamUrl,
            ValidReason,
            correlationId
        );

        // Assert
        Assert.Equal(ValidStreamId, evt.StreamId);
        Assert.Equal(ValidStreamName, evt.StreamName);
        Assert.Equal(StreamStatus.Starting, evt.PreviousStatus);
        Assert.Equal(StreamStatus.Playing, evt.NewStatus);
        Assert.Equal(ValidStreamUrl, evt.StreamUrl);
        Assert.Equal(ValidReason, evt.Reason);
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void IsTransitionToPlaying_WithTransitionToPlaying_ShouldReturnTrue()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing
        );

        // Assert
        Assert.True(evt.IsTransitionToPlaying);
    }

    [Fact]
    public void IsTransitionToPlaying_WithTransitionFromPlayingToPlaying_ShouldReturnFalse()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Playing,
            StreamStatus.Playing
        );

        // Assert
        Assert.False(evt.IsTransitionToPlaying);
    }

    [Fact]
    public void IsTransitionToPlaying_WithTransitionFromPlaying_ShouldReturnFalse()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Playing,
            StreamStatus.Stopped
        );

        // Assert
        Assert.False(evt.IsTransitionToPlaying);
    }

    [Fact]
    public void IsTransitionToStopped_WithTransitionToStopped_ShouldReturnTrue()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Playing,
            StreamStatus.Stopped
        );

        // Assert
        Assert.True(evt.IsTransitionToStopped);
    }

    [Fact]
    public void IsTransitionToStopped_WithTransitionFromStoppedToStopped_ShouldReturnFalse()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Stopped
        );

        // Assert
        Assert.False(evt.IsTransitionToStopped);
    }

    [Fact]
    public void IsTransitionToError_WithTransitionToError_ShouldReturnTrue()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Playing,
            StreamStatus.Error
        );

        // Assert
        Assert.True(evt.IsTransitionToError);
    }

    [Fact]
    public void IsTransitionToError_WithTransitionFromErrorToError_ShouldReturnFalse()
    {
        // Act
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Error,
            StreamStatus.Error
        );

        // Assert
        Assert.False(evt.IsTransitionToError);
    }

    [Fact]
    public void StatusTransitions_ShouldWorkForAllCombinations()
    {
        var statuses = Enum.GetValues<StreamStatus>();

        foreach (var previousStatus in statuses)
        {
            foreach (var newStatus in statuses)
            {
                // Act
                var evt = AudioStreamStatusChangedEvent.Create(
                    ValidStreamId,
                    ValidStreamName,
                    previousStatus,
                    newStatus
                );

                // Assert transitions
                var expectedToPlaying = newStatus == StreamStatus.Playing && previousStatus != StreamStatus.Playing;
                var expectedToStopped = newStatus == StreamStatus.Stopped && previousStatus != StreamStatus.Stopped;
                var expectedToError = newStatus == StreamStatus.Error && previousStatus != StreamStatus.Error;

                Assert.Equal(expectedToPlaying, evt.IsTransitionToPlaying);
                Assert.Equal(expectedToStopped, evt.IsTransitionToStopped);
                Assert.Equal(expectedToError, evt.IsTransitionToError);
            }
        }
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateEventWithDefaults()
    {
        // Act
        var evt = new AudioStreamStatusChangedEvent
        {
            StreamId = ValidStreamId,
            StreamName = ValidStreamName,
            PreviousStatus = StreamStatus.Stopped,
            NewStatus = StreamStatus.Playing,
        };

        // Assert
        Assert.Equal(ValidStreamId, evt.StreamId);
        Assert.Equal(ValidStreamName, evt.StreamName);
        Assert.Equal(StreamStatus.Stopped, evt.PreviousStatus);
        Assert.Equal(StreamStatus.Playing, evt.NewStatus);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new AudioStreamStatusChangedEvent(correlationId)
        {
            StreamId = ValidStreamId,
            StreamName = ValidStreamName,
            PreviousStatus = StreamStatus.Stopped,
            NewStatus = StreamStatus.Playing,
        };

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Event_ShouldImplementIDomainEvent()
    {
        // Arrange
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing
        );

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void Event_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing
        );

        // Act & Assert
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var evt1 = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing,
            ValidStreamUrl,
            ValidReason
        );

        var evt2 = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing,
            ValidStreamUrl,
            ValidReason
        );

        // Act & Assert
        Assert.NotEqual(evt1, evt2); // Different EventIds and timestamps
        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing
        );

        // Act & Assert
        Assert.Equal(evt, evt);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var evt = AudioStreamStatusChangedEvent.Create(
            ValidStreamId,
            ValidStreamName,
            StreamStatus.Stopped,
            StreamStatus.Playing,
            ValidStreamUrl,
            ValidReason
        );

        // Act
        var stringRepresentation = evt.ToString();

        // Assert
        Assert.Contains(ValidStreamId, stringRepresentation);
        Assert.Contains(ValidStreamName, stringRepresentation);
        Assert.Contains("Stopped", stringRepresentation);
        Assert.Contains("Playing", stringRepresentation);
    }

    [Fact]
    public void CommonStatusTransitions_ShouldHaveCorrectTransitionFlags()
    {
        // Test common streaming scenarios
        var scenarios = new[]
        {
            new
            {
                From = StreamStatus.Stopped,
                To = StreamStatus.Starting,
                ToPlaying = false,
                ToStopped = false,
                ToError = false,
            },
            new
            {
                From = StreamStatus.Starting,
                To = StreamStatus.Playing,
                ToPlaying = true,
                ToStopped = false,
                ToError = false,
            },
            new
            {
                From = StreamStatus.Playing,
                To = StreamStatus.Stopped,
                ToPlaying = false,
                ToStopped = true,
                ToError = false,
            },
            new
            {
                From = StreamStatus.Playing,
                To = StreamStatus.Error,
                ToPlaying = false,
                ToStopped = false,
                ToError = true,
            },
            new
            {
                From = StreamStatus.Error,
                To = StreamStatus.Stopped,
                ToPlaying = false,
                ToStopped = true,
                ToError = false,
            },
            new
            {
                From = StreamStatus.Stopped,
                To = StreamStatus.Playing,
                ToPlaying = true,
                ToStopped = false,
                ToError = false,
            },
        };

        foreach (var scenario in scenarios)
        {
            // Act
            var evt = AudioStreamStatusChangedEvent.Create(ValidStreamId, ValidStreamName, scenario.From, scenario.To);

            // Assert
            Assert.Equal(scenario.ToPlaying, evt.IsTransitionToPlaying);
            Assert.Equal(scenario.ToStopped, evt.IsTransitionToStopped);
            Assert.Equal(scenario.ToError, evt.IsTransitionToError);
        }
    }
}
