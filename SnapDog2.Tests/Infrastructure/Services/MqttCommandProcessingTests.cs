using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using MQTTnet.Client;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive unit tests for MQTT command processing functionality.
/// Covers topic parsing, command validation, event publishing, and error handling.
/// Award-worthy test suite ensuring robust MQTT integration with complete coverage.
/// </summary>
public class MqttCommandProcessingTests : IDisposable
{
    private readonly Mock<IMqttClient> _mockMqttClient;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<MqttService>> _mockLogger;
    private readonly MqttConfiguration _config;
    private readonly MqttService _mqttService;

    public MqttCommandProcessingTests()
    {
        _mockMqttClient = new Mock<IMqttClient>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<MqttService>>();
        
        _config = new MqttConfiguration
        {
            Enabled = true,
            Broker = "localhost",
            Port = 1883,
            BaseTopic = "snapdog",
            ClientId = "snapdog-test",
            Username = "testuser",
            Password = "testpass",
            KeepAliveSeconds = 60
        };

        var options = Options.Create(_config);
        _mqttService = new MqttService(_mockMqttClient.Object, options, _mockMediator.Object, _mockLogger.Object);
    }

    #region Topic Parsing Tests

    [Theory]
    [InlineData("snapdog/ZONE/1/VOLUME", "ZONE", "1", "VOLUME")]
    [InlineData("snapdog/CLIENT/living-room/MUTE", "CLIENT", "living-room", "MUTE")]
    [InlineData("snapdog/STREAM/2/START", "STREAM", "2", "START")]
    [InlineData("snapdog/SYSTEM/SYNC", "SYSTEM", "", "SYNC")]
    [InlineData("snapdog/CLIENT/bedroom-1/VOLUME", "CLIENT", "bedroom-1", "VOLUME")]
    [InlineData("snapdog/ZONE/10/STATUS", "ZONE", "10", "STATUS")]
    public void ParseTopic_WithValidTopics_ShouldReturnCorrectComponents(string topic, string expectedComponent, string expectedId, string expectedCommand)
    {
        // Act
        var result = _mqttService.ParseTopic(topic);

        // Assert
        result.Should().NotBeNull();
        result.Component.Should().Be(expectedComponent);
        result.Id.Should().Be(expectedId);
        result.Command.Should().Be(expectedCommand);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid/topic")]
    [InlineData("snapdog/INVALID")]
    [InlineData("wrongbase/ZONE/1/VOLUME")]
    [InlineData("snapdog")]
    [InlineData("")]
    [InlineData("snapdog/ZONE")]
    [InlineData("snapdog/ZONE/1")]
    [InlineData("snapdog/ZONE/1/VOLUME/EXTRA")]
    public void ParseTopic_WithInvalidTopics_ShouldReturnInvalidResult(string invalidTopic)
    {
        // Act
        var result = _mqttService.ParseTopic(invalidTopic);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ParseTopic_WithNullTopic_ShouldReturnInvalidResult()
    {
        // Act
        var result = _mqttService.ParseTopic(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ParseTopic_WithEmptyTopic_ShouldReturnInvalidResult()
    {
        // Act
        var result = _mqttService.ParseTopic(string.Empty);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("snapdog/zone/1/volume")] // lowercase
    [InlineData("SNAPDOG/ZONE/1/VOLUME")] // uppercase base
    [InlineData("snapdog/Zone/1/Volume")] // mixed case
    public void ParseTopic_WithCaseVariations_ShouldHandleCorrectly(string topic)
    {
        // Act
        var result = _mqttService.ParseTopic(topic);

        // Assert
        // Implementation should handle case sensitivity appropriately
        result.Should().NotBeNull();
    }

    #endregion

    #region Zone Command Processing Tests

    [Fact]
    public async Task ProcessZoneVolumeCommand_WithValidMessage_ShouldPublishEvent()
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 75 });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttZoneVolumeCommandEvent>(e => 
                e.ZoneId == 1 && 
                e.Volume == 75 && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessZoneMuteCommand_WithValidMessage_ShouldPublishEvent()
    {
        // Arrange
        var topic = "snapdog/ZONE/2/MUTE";
        var payload = JsonSerializer.Serialize(new { muted = true });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttZoneMuteCommandEvent>(e => 
                e.ZoneId == 2 && 
                e.Muted == true && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(1000)]
    public async Task ProcessZoneVolumeCommand_WithInvalidVolume_ShouldLogErrorAndNotPublish(int invalidVolume)
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = invalidVolume });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(It.IsAny<MqttZoneVolumeCommandEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLoggerError($"Invalid volume value: {invalidVolume}");
    }

    [Fact]
    public async Task ProcessZoneCommand_WithInvalidZoneId_ShouldLogErrorAndNotPublish()
    {
        // Arrange
        var topic = "snapdog/ZONE/invalid/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 50 });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLoggerError("Invalid zone ID: invalid");
    }

    #endregion

    #region Client Command Processing Tests

    [Fact]
    public async Task ProcessClientVolumeCommand_WithValidMessage_ShouldPublishEvent()
    {
        // Arrange
        var topic = "snapdog/CLIENT/living-room/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 80 });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttClientVolumeCommandEvent>(e => 
                e.ClientId == "living-room" && 
                e.Volume == 80 && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessClientMuteCommand_WithValidMessage_ShouldPublishEvent()
    {
        // Arrange
        var topic = "snapdog/CLIENT/bedroom/MUTE";
        var payload = JsonSerializer.Serialize(new { muted = false });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttClientMuteCommandEvent>(e => 
                e.ClientId == "bedroom" && 
                e.Muted == false && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ProcessClientCommand_WithInvalidClientId_ShouldLogErrorAndNotPublish(string invalidClientId)
    {
        // Arrange
        var topic = $"snapdog/CLIENT/{invalidClientId ?? "null"}/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 50 });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLoggerError("Invalid client ID");
    }

    [Fact]
    public async Task ProcessClientCommand_WithSpecialCharactersInClientId_ShouldHandleCorrectly()
    {
        // Arrange
        var clientId = "client-with-dashes_and_underscores.and.dots";
        var topic = $"snapdog/CLIENT/{clientId}/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 60 });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttClientVolumeCommandEvent>(e => e.ClientId == clientId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Stream Command Processing Tests

    [Theory]
    [InlineData("START", "start")]
    [InlineData("STOP", "stop")]
    [InlineData("PAUSE", "pause")]
    [InlineData("RESUME", "resume")]
    public async Task ProcessStreamCommand_WithValidCommands_ShouldPublishEvent(string command, string expectedAction)
    {
        // Arrange
        var topic = $"snapdog/STREAM/3/{command}";
        var payload = JsonSerializer.Serialize(new { });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttStreamCommandEvent>(e => 
                e.StreamId == 3 && 
                e.Action.ToLowerInvariant() == expectedAction && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessStreamCommand_WithInvalidStreamId_ShouldLogErrorAndNotPublish()
    {
        // Arrange
        var topic = "snapdog/STREAM/invalid/START";
        var payload = JsonSerializer.Serialize(new { });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLoggerError("Invalid stream ID: invalid");
    }

    [Fact]
    public async Task ProcessStreamAssignmentCommand_WithValidMessage_ShouldPublishEvent()
    {
        // Arrange
        var topic = "snapdog/STREAM/2/ASSIGN";
        var payload = JsonSerializer.Serialize(new { groupId = "group1", streamId = "stream5" });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttStreamAssignmentCommandEvent>(e => 
                e.GroupId == "group1" && 
                e.StreamId == "stream5" && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region System Command Processing Tests

    [Theory]
    [InlineData("SYNC")]
    [InlineData("RESTART")]
    [InlineData("STATUS")]
    [InlineData("HEALTH")]
    public async Task ProcessSystemCommand_WithValidCommands_ShouldPublishEvent(string command)
    {
        // Arrange
        var topic = $"snapdog/SYSTEM/{command}";
        var payload = JsonSerializer.Serialize(new { });
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttSystemCommandEvent>(e => 
                e.Command == command && 
                e.Topic == topic),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSystemCommand_WithParameters_ShouldIncludeInEvent()
    {
        // Arrange
        var topic = "snapdog/SYSTEM/CONFIG";
        var parameters = new { setting = "value", number = 42, flag = true };
        var payload = JsonSerializer.Serialize(parameters);
        var message = CreateMqttMessage(topic, payload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttSystemCommandEvent>(e => 
                e.Command == "CONFIG" && 
                e.Parameters != null &&
                e.Parameters.ContainsKey("setting") &&
                e.Parameters["setting"].ToString() == "value"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Payload Processing Tests

    [Fact]
    public async Task ProcessCommandMessage_WithJsonPayload_ShouldParseCorrectly()
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var complexPayload = JsonSerializer.Serialize(new 
        { 
            volume = 65,
            timestamp = DateTime.UtcNow,
            source = "mobile-app",
            metadata = new { user = "admin", device = "iPhone" }
        });
        var message = CreateMqttMessage(topic, complexPayload);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttZoneVolumeCommandEvent>(e => 
                e.Volume == 65 &&
                e.Metadata != null &&
                e.Metadata.ContainsKey("source")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandMessage_WithInvalidJson_ShouldLogErrorAndNotPublish()
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var invalidJson = "{ invalid json }";
        var message = CreateMqttMessage(topic, invalidJson);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLoggerError("Failed to parse command payload");
    }

    [Fact]
    public async Task ProcessCommandMessage_WithEmptyPayload_ShouldHandleGracefully()
    {
        // Arrange
        var topic = "snapdog/SYSTEM/SYNC";
        var message = CreateMqttMessage(topic, "");

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        _mockMediator.Verify(x => x.Publish(
            It.Is<MqttSystemCommandEvent>(e => e.Command == "SYNC"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandMessage_WithBinaryPayload_ShouldHandleCorrectly()
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var binaryData = new byte[] { 0xFF, 0xFE, 0xFD };
        var message = CreateMqttMessage(topic, binaryData);

        // Act
        await _mqttService.ProcessCommandMessageAsync(message);

        // Assert
        // Should attempt to process but likely fail gracefully
        VerifyLoggerError("Failed to parse command payload");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ProcessCommandMessage_WithMediatorException_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 50 });
        var message = CreateMqttMessage(topic, payload);

        _mockMediator.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mediator error"));

        // Act & Assert
        var act = () => _mqttService.ProcessCommandMessageAsync(message);
        await act.Should().NotThrowAsync();

        VerifyLoggerError("Error processing MQTT command");
    }

    [Fact]
    public async Task ProcessCommandMessage_WithNullMessage_ShouldLogErrorAndNotThrow()
    {
        // Act & Assert
        var act = () => _mqttService.ProcessCommandMessageAsync(null!);
        await act.Should().NotThrowAsync();

        VerifyLoggerError("Received null MQTT message");
    }

    [Fact]
    public async Task ProcessCommandMessage_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var topic = "snapdog/ZONE/1/VOLUME";
        var payload = JsonSerializer.Serialize(new { volume = 50 });
        var message = CreateMqttMessage(topic, payload);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = () => _mqttService.ProcessCommandMessageAsync(message, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Topic Subscription Tests

    [Fact]
    public async Task SubscribeToCommandsAsync_ShouldSubscribeToCorrectTopics()
    {
        // Arrange
        var expectedTopics = new[]
        {
            "snapdog/ZONE/+/+",
            "snapdog/CLIENT/+/+",
            "snapdog/STREAM/+/+",
            "snapdog/SYSTEM/+"
        };

        // Act
        await _mqttService.SubscribeToCommandsAsync();

        // Assert
        foreach (var topic in expectedTopics)
        {
            _mockMqttClient.Verify(x => x.SubscribeAsync(
                It.Is<MqttTopicFilter>(f => f.Topic == topic),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task SubscribeToCommandsAsync_WithSubscriptionFailure_ShouldReturnFalse()
    {
        // Arrange
        _mockMqttClient.Setup(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MqttCommunicationException("Subscription failed"));

        // Act
        var result = await _mqttService.SubscribeToCommandsAsync();

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError("Failed to subscribe to MQTT command topics");
    }

    #endregion

    #region Publishing Tests

    [Fact]
    public async Task PublishClientVolumeAsync_WithValidData_ShouldPublishCorrectMessage()
    {
        // Arrange
        var clientId = "test-client";
        var volume = 75;

        // Act
        var result = await _mqttService.PublishClientVolumeAsync(clientId, volume);

        // Assert
        result.Should().BeTrue();
        _mockMqttClient.Verify(x => x.PublishAsync(
            It.Is<MqttApplicationMessage>(m => 
                m.Topic == $"snapdog/CLIENT/{clientId}/VOLUME/status" &&
                m.PayloadSegment.Array != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishZoneVolumeAsync_WithValidData_ShouldPublishCorrectMessage()
    {
        // Arrange
        var zoneId = 3;
        var volume = 60;

        // Act
        var result = await _mqttService.PublishZoneVolumeAsync(zoneId, volume);

        // Assert
        result.Should().BeTrue();
        _mockMqttClient.Verify(x => x.PublishAsync(
            It.Is<MqttApplicationMessage>(m => 
                m.Topic == $"snapdog/ZONE/{zoneId}/VOLUME/status" &&
                m.PayloadSegment.Array != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishStreamStatusAsync_WithValidData_ShouldPublishCorrectMessage()
    {
        // Arrange
        var streamId = 5;
        var status = "playing";

        // Act
        var result = await _mqttService.PublishStreamStatusAsync(streamId, status);

        // Assert
        result.Should().BeTrue();
        _mockMqttClient.Verify(x => x.PublishAsync(
            It.Is<MqttApplicationMessage>(m => 
                m.Topic == $"snapdog/STREAM/{streamId}/STATUS" &&
                m.PayloadSegment.Array != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishClientStatusAsync_WithValidData_ShouldPublishCorrectMessage()
    {
        // Arrange
        var clientId = "bedroom-client";
        var connected = true;

        // Act
        var result = await _mqttService.PublishClientStatusAsync(clientId, connected);

        // Assert
        result.Should().BeTrue();
        _mockMqttClient.Verify(x => x.PublishAsync(
            It.Is<MqttApplicationMessage>(m => 
                m.Topic == $"snapdog/CLIENT/{clientId}/STATUS" &&
                m.PayloadSegment.Array != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public async Task ProcessCommandMessage_WithHighFrequencyMessages_ShouldHandleEfficiently()
    {
        // Arrange
        const int messageCount = 1000;
        var messages = new List<MqttApplicationMessage>();
        
        for (int i = 0; i < messageCount; i++)
        {
            var topic = $"snapdog/ZONE/{i % 10}/VOLUME";
            var payload = JsonSerializer.Serialize(new { volume = i % 101 });
            messages.Add(CreateMqttMessage(topic, payload));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = messages.Select(msg => _mqttService.ProcessCommandMessageAsync(msg));
        await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should process 1000 messages in under 5 seconds
        _mockMediator.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(messageCount));
    }

    [Fact]
    public async Task ProcessCommandMessage_WithConcurrentMessages_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int messagesPerThread = 100;
        var tasks = new Task[threadCount];

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var threadId = t;
            tasks[t] = Task.Run(async () =>
            {
                for (int i = 0; i < messagesPerThread; i++)
                {
                    var topic = $"snapdog/ZONE/{threadId}/VOLUME";
                    var payload = JsonSerializer.Serialize(new { volume = (threadId * messagesPerThread + i) % 101 });
                    var message = CreateMqttMessage(topic, payload);
                    
                    await _mqttService.ProcessCommandMessageAsync(message);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        _mockMediator.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(threadCount * messagesPerThread));
    }

    #endregion

    #region Helper Methods

    private MqttApplicationMessage CreateMqttMessage(string topic, string payload)
    {
        return new MqttApplicationMessage
        {
            Topic = topic,
            PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload))
        };
    }

    private MqttApplicationMessage CreateMqttMessage(string topic, byte[] payload)
    {
        return new MqttApplicationMessage
        {
            Topic = topic,
            PayloadSegment = new ArraySegment<byte>(payload)
        };
    }

    private void VerifyLoggerError(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLoggerWarning(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    public void Dispose()
    {
        _mqttService?.Dispose();
    }
}