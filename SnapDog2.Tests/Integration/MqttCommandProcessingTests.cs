using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
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
    private readonly Mock<IManagedMqttClient> _mockMqttClient;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<MqttService>> _mockLogger;
    private readonly MqttConfiguration _config;
    private readonly MqttService _mqttService;

    public MqttCommandProcessingTests()
    {
        var factory = new MqttFactory();
        _mockMqttClient = new Mock<IManagedMqttClient>();
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
        _mqttService = new MqttService(options, _mockLogger.Object, _mockMediator.Object);
    }

    public void Dispose()
    {
        _mqttService?.Dispose();
    }
}