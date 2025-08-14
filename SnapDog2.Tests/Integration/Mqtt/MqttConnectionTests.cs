namespace SnapDog2.Tests.Integration.Mqtt;

using System.Net.Sockets;
using FluentAssertions;
using SnapDog2.Tests.Integration.Fixtures;

[Collection("IntegrationContainers")]
public class MqttConnectionTests
{
    private readonly TestcontainersFixture _fixture;

    public MqttConnectionTests(TestcontainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Broker_ShouldAcceptTcpConnections()
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(_fixture.MqttHost, _fixture.MqttPort);
        var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(connectTask, "MQTT broker should accept TCP connections on the mapped port");
        client.Connected.Should().BeTrue();
    }
}
