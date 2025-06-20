using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Api;
using SnapDog2.Api.Models;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Mqtt.Commands;
using SnapDog2.Server.Features.Mqtt.Queries;
using Xunit;
using FluentValidation; // For ValidationException
using FluentValidation.Results; // For ValidationFailure

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for MqttController API endpoints.
/// Tests authentication, validation, and proper MediatR integration.
/// </summary>
[Trait("Category", "Integration")]
public class MqttControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<SnapDog2.Api.Program>>
{
    private readonly TestWebApplicationFactory<SnapDog2.Api.Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IMqttService> _mockMqttService;
    private readonly Mock<IMediator> _mockMediator;

    public MqttControllerIntegrationTests(TestWebApplicationFactory<SnapDog2.Api.Program> factory)
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
        var expectedStatus = new MqttConnectionStatusResponse
        {
            IsConnected = true,
            BrokerHost = "localhost",
            BrokerPort = 1883,
            ClientId = "test-client",
        };
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetMqttConnectionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.GetAsync("/api/mqtt/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
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
            .Setup(m => m.Send(It.IsAny<PublishMqttMessageCommand>(), It.IsAny<CancellationToken>()))
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
            m =>
                m.Send(
                    It.Is<PublishMqttMessageCommand>(c =>
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
            .Setup(m => m.Send(It.Is<PublishMqttMessageCommand>(cmd => cmd.Topic == ""), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation failed", new[]
            {
                new ValidationFailure("Topic", "Topic is required")
            }));

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
            .Setup(m => m.Send(It.IsAny<SubscribeToMqttTopicCommand>(), It.IsAny<CancellationToken>()))
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
            m =>
                m.Send(
                    It.Is<SubscribeToMqttTopicCommand>(c => c.TopicPattern == "sensors/+/temperature"),
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
            .Setup(m => m.Send(It.IsAny<UnsubscribeFromMqttTopicCommand>(), It.IsAny<CancellationToken>()))
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
            m =>
                m.Send(
                    It.Is<UnsubscribeFromMqttTopicCommand>(c => c.TopicPattern == "sensors/+/temperature"),
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
            .ThrowsAsync(new OperationCanceledException());

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => // HttpClient throws TaskCanceledException for timeouts/cancellations
        {
            await _client.GetAsync("/api/mqtt/status", cts.Token);
        });
    }
}
