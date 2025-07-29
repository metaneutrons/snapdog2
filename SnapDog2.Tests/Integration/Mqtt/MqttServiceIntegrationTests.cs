using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Integration.Mqtt;

[Trait("Category", "Integration")]
public class MqttServiceIntegrationTests
{
    [Fact]
    public async Task PublishAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var mqttTestContainer = new MqttTestContainer();
        await mqttTestContainer.InitializeAsync();

        var logger = new Mock<ILogger<MqttService>>().Object;
        var connectionString = $"{mqttTestContainer.Container.Hostname}:{mqttTestContainer.Container.GetMappedPublicPort(1883)}";
        var config = new MqttConfiguration { Broker = connectionString.Split(':')[0], Port = int.Parse(connectionString.Split(':')[1]) };
        var options = Options.Create(config);
        var mqttService = new MqttService(options, logger);
        await mqttService.ConnectAsync();
        var topic = "test/topic";
        var payload = "Hello, World!";

        // Act
        var result = await mqttService.PublishAsync(topic, payload);

        // Assert
        Assert.True(result);

        await mqttTestContainer.DisposeAsync();
    }
}
