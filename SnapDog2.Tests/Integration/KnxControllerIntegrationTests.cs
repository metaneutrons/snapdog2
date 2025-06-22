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
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Infrastructure.Services.Models;
using SnapDog2.Server.Features.Knx.Commands;
using SnapDog2.Server.Features.Knx.Queries;
using Xunit;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests for KnxController API endpoints.
/// Tests authentication, validation, and proper MediatR integration.
/// </summary>
[Trait("Category", "Integration")]
public class KnxControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IKnxService> _mockKnxService;
    private readonly Mock<IMediator> _mockMediator;

    public KnxControllerIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.ResetMocks(); // Reset mocks before each test run (or rather, before the class is used by tests)
        _mockKnxService = factory.MockKnxService;
        _mockMediator = factory.MockMediator;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetConnectionStatus_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConnectionStatus_WithValidAuthentication_ShouldReturnOk()
    {
        // Arrange
        var expectedStatus = new KnxConnectionStatus
        {
            IsConnected = true,
            Gateway = "192.168.1.100",
            Port = 3671,
            ConnectionState = "Connected",
            LastSuccessfulConnection = DateTime.UtcNow,
        };
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetKnxConnectionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Since ApiResponse wrapper was removed, we expect direct JSON content
        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetDevices_WithValidAuthentication_ShouldReturnOk()
    {
        // Arrange
        var expectedDevices = new List<KnxDeviceInfo>
        {
            new KnxDeviceInfo
            {
                IndividualAddress = "1.1.1",
                DeviceType = "Light Switch",
                ManufacturerId = 123,
                IsProgrammingMode = false,
            },
        };
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetKnxDevicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDevices);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.GetAsync("/api/knx/devices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Since ApiResponse wrapper was removed, we expect direct JSON content
        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task WriteGroupValue_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var requestDto = new WriteKnxValueRequest
        {
            Address = "1/1/1",
            Value = Convert.ToBase64String(new byte[] { 0x01 }),
            Description = "Turn on light",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<WriteGroupValueCommand>(c =>
                        c.Address.ToString() == "1/1/1" && c.Value.SequenceEqual(new byte[] { 0x01 })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            m =>
                m.Send(
                    It.Is<WriteGroupValueCommand>(c =>
                        c.Address.ToString() == "1/1/1"
                        && c.Value.SequenceEqual(new byte[] { 0x01 })
                        && c.Description == "Turn on light"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ReadGroupValue_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var requestDto = new ReadKnxValueRequest // Changed from ReadGroupValueCommand
        {
            Address = "1/1/1", // Address as string
            Description = "Read light status",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<ReadGroupValueCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new byte[] { 0x01 });

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto); // Serialize the DTO
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/read", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            m =>
                m.Send(
                    It.Is<ReadGroupValueCommand>(c =>
                        c.Address.ToString() == "1/1/1" && c.Description == "Read light status"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task WriteGroupValue_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange - This will fail validation due to invalid address format
        var invalidJson = """{"Address": "invalid", "Value": "AQ==", "Description": "Test"}"""; // Value: [1] -> "AQ=="
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        var malformedJson = new StringContent("{invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/write", malformedJson);

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
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetKnxConnectionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => // HttpClient throws TaskCanceledException for timeouts/cancellations
        {
            await _client.GetAsync("/api/knx/status", cts.Token);
        });
    }

    #region Missing Endpoint Coverage - Subscribe/Unsubscribe Operations

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SubscribeToGroup_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var requestDto = new SubscribeKnxRequest // Changed from SubscribeToGroupCommand
        {
            Address = "1/1/1", // Address as string
            Description = "Subscribe to light switch",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<SubscribeToGroupCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto); // Serialize the DTO
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/subscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            m =>
                m.Send(
                    It.Is<SubscribeToGroupCommand>(c =>
                        c.Address.ToString() == "1/1/1" && c.Description == "Subscribe to light switch"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SubscribeToGroup_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange - This will fail validation due to invalid address format
        var invalidJson = """{"Address": "invalid", "Description": "Test"}""";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/subscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SubscribeToGroup_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var requestDto = new SubscribeKnxRequest // Changed from SubscribeToGroupCommand
        {
            Address = "1/1/1", // Address as string
            Description = "Subscribe to light switch",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<SubscribeToGroupCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("KNX service unavailable"));

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto); // Serialize the DTO
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/subscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnsubscribeFromGroup_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var requestDto = new UnsubscribeKnxRequest // Changed from UnsubscribeFromGroupCommand
        {
            Address = "1/1/1", // Address as string
            Description = "Unsubscribe from light switch",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<UnsubscribeFromGroupCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto); // Serialize the DTO
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/unsubscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(
            m =>
                m.Send(
                    It.Is<UnsubscribeFromGroupCommand>(c =>
                        c.Address.ToString() == "1/1/1" && c.Description == "Unsubscribe from light switch"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnsubscribeFromGroup_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange - This will fail validation due to invalid address format
        var invalidJson = """{"Address": "invalid", "Description": "Test"}""";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/unsubscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnsubscribeFromGroup_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var requestDto = new UnsubscribeKnxRequest // Changed from UnsubscribeFromGroupCommand
        {
            Address = "1/1/1", // Address as string
            Description = "Unsubscribe from light switch",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<UnsubscribeFromGroupCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("KNX service unavailable"));

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto); // Serialize the DTO
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/unsubscribe", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Validation Boundary Condition Tests

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("-1/1/1")] // Invalid main group (below range)
    [InlineData("32/1/1")] // Invalid main group (above range)
    [InlineData("1/-1/1")] // Invalid middle group (below range)
    [InlineData("1/8/1")] // Invalid middle group (above range)
    [InlineData("1/1/-1")] // Invalid sub group (below range)
    [InlineData("1/1/256")] // Invalid sub group (above range)
    public async Task WriteGroupValue_WithInvalidAddressRange_ShouldReturnBadRequest(string address)
    {
        // Arrange
        var invalidJson = $$"""{"Address": "{{address}}", "Value": "AQ==", "Description": "Test"}"""; // Value: [1] -> "AQ=="
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteGroupValue_WithEmptyValue_ShouldReturnBadRequest()
    {
        // Arrange
        var requestDto = new WriteKnxValueRequest
        {
            Address = "1/1/1",
            Value = "", // This will result in an empty byte[] after Convert.FromBase64String
            Description = "Test empty value",
        };

        // Simulate FluentValidation failure for a command with an empty byte[] Value
        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<WriteGroupValueCommand>(cmd =>
                        cmd.Address.ToString() == "1/1/1" && cmd.Value != null && cmd.Value.Length == 0
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(
                new FluentValidation.ValidationException(
                    "Validation failed",
                    new[] { new ValidationFailure("Value", "Value cannot be empty.") }
                )
            );

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteGroupValue_WithOversizedValue_ShouldReturnBadRequest()
    {
        // Arrange - Create a value larger than the typical KNX payload max (e.g., 15 bytes if max is 14, or 16 if max is 15)
        // KnxService uses MaxKnxPayloadSize = 15 and throws if value.Length > MaxKnxPayloadSize. So, 16 bytes is oversized.
        var oversizedValue = new byte[16];
        for (int i = 0; i < oversizedValue.Length; i++)
        {
            oversizedValue[i] = (byte)i; // Populate with some data
        }

        var requestDto = new WriteKnxValueRequest
        {
            Address = "1/1/1",
            Value = Convert.ToBase64String(oversizedValue),
            Description = "Test oversized value",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<WriteGroupValueCommand>(cmd =>
                        cmd.Address.ToString() == "1/1/1" && cmd.Value.SequenceEqual(oversizedValue)
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ArgumentOutOfRangeException("value", "Value size exceeds maximum KNX payload size."));

        var json = JsonSerializer.Serialize(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteGroupValue_WithMaxValidValue_ShouldReturnOk()
    {
        // Arrange - Create exactly 14 bytes (KNX maximum allowed)
        var maxValidValue = new byte[14];
        for (int i = 0; i < maxValidValue.Length; i++)
        {
            maxValidValue[i] = (byte)(i + 1);
        }

        var requestDto = new WriteKnxValueRequest
        {
            Address = "1/1/1",
            Value = Convert.ToBase64String(maxValidValue),
            Description = "Test max valid value",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<WriteGroupValueCommand>(c =>
                        c.Address.ToString() == "1/1/1" && c.Value.SequenceEqual(maxValidValue)
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteGroupValue_WithNullValue_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = """{"Address": "1/1/1", "Value": null, "Description": "Test"}""";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteGroupValue_WithMissingRequiredField_ShouldReturnBadRequest()
    {
        // Arrange - Missing Address field
        var invalidJson = """{"Value": "AQ==", "Description": "Test"}"""; // Value: [1] -> "AQ=="
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Service Integration Failure Scenarios

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetConnectionStatus_WhenKnxServiceThrows_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetKnxConnectionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("KNX service connection failed"));

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteGroupValue_WhenKnxServiceUnavailable_ShouldReturnInternalServerError()
    {
        // Arrange
        var requestDto = new WriteKnxValueRequest
        {
            Address = "1/1/1",
            Value = Convert.ToBase64String(new byte[] { 0x01 }),
            Description = "Test write operation",
        };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<WriteGroupValueCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("KNX service unavailable"));

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/write", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadGroupValue_WhenOperationTimesOut_ShouldReturnInternalServerError()
    {
        // Arrange
        var requestDto = new ReadKnxValueRequest { Address = "1/1/1", Description = "Test read operation" };

        _mockMediator
            .Setup(m =>
                m.Send(
                    It.Is<ReadGroupValueCommand>(c => c.Address.ToString() == "1/1/1"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new TimeoutException("KNX read operation timed out"));

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var json = JsonSerializer.Serialize(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/knx/read", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetDevices_WhenKnxServiceThrows_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetKnxDevicesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to enumerate KNX devices"));

        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await _client.GetAsync("/api/knx/devices");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Authentication & Authorization Edge Cases

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApiEndpoints_WithEmptyApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "");

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApiEndpoints_WithWhitespaceOnlyApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", "   ");

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApiEndpoints_WithMalformedAuthorizationHeader_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApiEndpoints_WithMissingAuthorizationScheme_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "test-api-key"); // Missing "ApiKey" scheme

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApiEndpoints_WithNullApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", null);

        // Act
        var response = await _client.GetAsync("/api/knx/status");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
