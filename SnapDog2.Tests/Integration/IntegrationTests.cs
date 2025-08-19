using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Infrastructure.Integrations.Knx;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Tests.Fixtures.Containers;
using SnapDog2.Tests.Integration.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Integration tests using the comprehensive test fixture with MQTT, KNX, and Snapcast services.
/// These tests validate that the application infrastructure works correctly with real dependencies.
/// </summary>
[Collection("Integration")]
public class IntegrationTests
{
    private readonly DockerComposeTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public IntegrationTests(DockerComposeTestFixture fixture, ITestOutputHelper output)
    {
        this._fixture = fixture;
        this._output = output;
    }

    [Fact]
    public async Task Application_Should_StartSuccessfully()
    {
        // Arrange & Act - Application startup happens in fixture

        // Assert
        this._fixture.HttpClient.Should().NotBeNull();
        this._fixture.ServiceProvider.Should().NotBeNull();

        var response = await this._fixture.HttpClient.GetAsync("/health");

        this._output.WriteLine($"Health check response: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            this._output.WriteLine($"Health check content: {content}");
        }

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public void Services_Should_BeRegisteredInDI()
    {
        // Assert - Verify core service interfaces are registered
        this._fixture.AssertServiceIsRunning<IMqttService>();
        this._fixture.AssertServiceIsRunning<IKnxService>();
        this._fixture.AssertServiceIsRunning<ISnapcastService>();
        // Note: Don't check concrete classes as they're registered as interfaces
    }

    [Fact]
    public void Configuration_Should_BeValid()
    {
        // Assert - Verify configuration is properly loaded
        this._fixture.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task MqttService_Should_ConnectSuccessfully()
    {
        // Arrange
        using var scope = this._fixture.ServiceProvider.CreateScope();
        var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();

        // Act
        var result = await mqttService.InitializeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        mqttService.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task KnxService_Should_ConnectSuccessfully()
    {
        // Arrange
        using var scope = this._fixture.ServiceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetRequiredService<IKnxService>();

        // Act
        var result = await knxService.InitializeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        knxService.IsConnected.Should().BeTrue();
        knxService.Status.Should().Be(ServiceStatus.Running);
    }

    [Fact]
    public async Task MqttService_Should_PublishAndReceiveMessages()
    {
        // Arrange
        using var scope = this._fixture.ServiceProvider.CreateScope();
        var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();
        await mqttService.InitializeAsync();

        var testTopic = "test/topic";
        var testPayload = "test message";
        var messageReceived = false;
        string? receivedPayload = null;

        mqttService.MessageReceived += (sender, args) =>
        {
            if (args.Topic == testTopic)
            {
                receivedPayload = args.Payload;
                messageReceived = true;
            }
        };

        // Subscribe to test topic
        await mqttService.SubscribeAsync([testTopic]);

        // Act
        var publishResult = await mqttService.PublishAsync(testTopic, testPayload);

        // Wait for message to be received
        await this._fixture.WaitForConditionAsync(
            () => Task.FromResult(messageReceived),
            TimeSpan.FromSeconds(5),
            "MQTT message to be received"
        );

        // Assert
        publishResult.IsSuccess.Should().BeTrue();
        messageReceived.Should().BeTrue();
        receivedPayload.Should().Be(testPayload);
    }

    [Fact]
    public async Task KnxService_Should_WriteAndReadGroupValues()
    {
        // Arrange
        using var scope = this._fixture.ServiceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetRequiredService<IKnxService>();
        await knxService.InitializeAsync();

        var testGroupAddress = "1/1/1";
        var testValue = true;

        // Act
        var writeResult = await knxService.WriteGroupValueAsync(testGroupAddress, testValue);

        // Give some time for the value to be written
        await Task.Delay(100);

        var readResult = await knxService.ReadGroupValueAsync(testGroupAddress);

        // Assert
        writeResult.IsSuccess.Should().BeTrue();
        readResult.IsSuccess.Should().BeTrue();
        readResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task TestClients_Should_ConnectToServices()
    {
        // Act & Assert - Test MQTT client connection
        var mqttResult = await this._fixture.SendMqttCommandAsync("test/command", new { action = "test" });
        mqttResult.IsSuccess.Should().BeTrue();

        // Act & Assert - Test KNX client connection
        var knxResult = await this._fixture.SendKnxCommandAsync("1/1/1", true);
        knxResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Integration_Should_HandleZoneStateRequests()
    {
        // Act
        var zoneState = await this._fixture.GetZoneStateAsync(1);

        // Assert
        zoneState.Should().NotBeNull();
        // Note: Actual zone state validation would depend on the application's current state
        // This test validates that the HTTP endpoint is working and returns valid data
    }

    [Fact]
    public async Task Services_Should_HandleConcurrentOperations()
    {
        // Arrange
        using var scope = this._fixture.ServiceProvider.CreateScope();
        var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();
        var knxService = scope.ServiceProvider.GetRequiredService<IKnxService>();

        await mqttService.InitializeAsync();
        await knxService.InitializeAsync();

        // Act - Perform concurrent operations
        var tasks = new List<Task<bool>>();

        for (int i = 0; i < 5; i++)
        {
            var index = i;
            tasks.Add(
                Task.Run(async () =>
                {
                    var mqttResult = await mqttService.PublishAsync($"test/concurrent/{index}", $"message {index}");
                    var knxResult = await knxService.WriteGroupValueAsync("1/1/1", index % 2 == 0);
                    return mqttResult.IsSuccess && knxResult.IsSuccess;
                })
            );
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
    }
}

/// <summary>
/// Test collection for integration tests to ensure proper fixture sharing.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<DockerComposeTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
