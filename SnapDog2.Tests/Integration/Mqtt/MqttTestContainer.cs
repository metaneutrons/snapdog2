using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SnapDog2.Tests.Integration.Mqtt;

public class MqttTestContainer
{
    public IContainer Container { get; }

    public MqttTestContainer()
    {
        Container = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:latest")
            .WithExposedPort(1883)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
    }
}
