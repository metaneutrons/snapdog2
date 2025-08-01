using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentValidation; // For ValidationException
using FluentValidation.Results; // For ValidationFailure
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Mqtt.Commands;
using SnapDog2.Server.Features.Mqtt.Queries;
using Xunit;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for MqttController API endpoints.
/// Tests authentication, validation, and proper MediatR integration.
/// </summary>
[Trait("Category", "Integration")]
public class MqttControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IMqttService> _mockMqttService;
    private readonly Mock<IMediator> _mockMediator;

    public MqttControllerIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockMqttService = factory.MockMqttService;
        _mockMediator = factory.MockMediator;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetConnectionStatus_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/mqtt/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConnectionStatus_WithValidAuthentication_ShouldReturnOk()
    {
        // Arrange
        var expectedStatus = new MqttConnectionStatusResponse { IsConnected = true, BrokerHost = "localhost" };
        _mockMediator
            .Setup(static m => m.Send(It.IsAny<GetMqttConnectionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.GetAsync("/api/mqtt/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Since ApiResponse wrapper was removed, we expect direct JSON content
        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task PublishMessage_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var command = new PublishMqttMessageCommand
        {
            Topic = "test/topic",
            Payload = "Hello World",
            Retain = false,
        };

        _mockMediator
            .Setup(static m => m.Send(It.IsAny<PublishMqttMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/mqtt/publish", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            static m =>
                m.Send(
                    It.Is<PublishMqttMessageCommand>(static c =>
                        c.Topic == "test/topic" && c.Payload == "Hello World" && c.Retain == false
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task PublishMessage_WithEmptyTopic_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new PublishMqttMessageCommand
        {
            Topic = "", // Invalid empty topic
            Payload = "Hello World",
            Retain = false,
        };

        // Setup the mock Mediator to throw ValidationException for this command
        _mockMediator
            .Setup(static m =>
                m.Send(It.Is<PublishMqttMessageCommand>(static cmd => cmd.Topic == ""), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(
                new ValidationException(
                    "Validation failed",
                    new[] { new ValidationFailure("Topic", "Topic is required") }
                )
            );

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/mqtt/publish", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubscribeToTopic_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var command = new SubscribeToMqttTopicCommand { TopicPattern = "sensors/+/temperature" };

        _mockMediator
            .Setup(static m => m.Send(It.IsAny<SubscribeToMqttTopicCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/mqtt/subscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            static m =>
                m.Send(
                    It.Is<SubscribeToMqttTopicCommand>(static c => c.TopicPattern == "sensors/+/temperature"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromTopic_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var command = new UnsubscribeFromMqttTopicCommand { TopicPattern = "sensors/+/temperature" };

        _mockMediator
            .Setup(static m => m.Send(It.IsAny<UnsubscribeFromMqttTopicCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/mqtt/unsubscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            static m =>
                m.Send(
                    It.Is<UnsubscribeFromMqttTopicCommand>(static c => c.TopicPattern == "sensors/+/temperature"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ApiEndpoints_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        var malformedJson = new StringContent("{invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/mqtt/publish", malformedJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_WithInvalidApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");

        // Act
        var response = await _client.GetAsync("/api/mqtt/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetMqttConnectionStatusQuery>(), It.IsAny<CancellationToken>()))
            .Returns(
                async (GetMqttConnectionStatusQuery query, CancellationToken ct) =>
                {
                    // Simulate work that can be cancelled
                    await Task.Delay(TimeSpan.FromSeconds(5), ct); // Increased delay to ensure cancellation can occur
                    ct.ThrowIfCancellationRequested();
                    // If not cancelled, return a dummy status. This part might not be reached if cancellation is quick.
                    return new MqttConnectionStatusResponse
                    {
                        IsConnected = true,
                        BrokerHost = "simulated",
                        ClientId = "simulated-client",
                        BrokerPort = 1883,
                    };
                }
            );

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Shorten the CTS delay to ensure it cancels before the mock's Task.Delay completes
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)); // Cancel after 100ms

        // Act & Assert
        // HttpClient will throw TaskCanceledException when its own token is cancelled
        // or if the underlying connection is aborted due to OperationCanceledException from the server-side handler.
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await _client.GetAsync("/api/mqtt/status", cts.Token);
        });
    }
}
