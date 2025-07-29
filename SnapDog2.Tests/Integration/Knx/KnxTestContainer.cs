using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace SnapDog2.Tests.Integration.Knx;

public class KnxTestContainer : IAsyncLifetime
{
    private readonly IContainer _container;

    public KnxTestContainer()
    {
        _container = new ContainerBuilder()
            .WithImage("michelde/knxd")
            .WithCommand("--eibacc", "ipt:127.0.0.1")
            .WithExposedPort(3671)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(3671))
            .Build();
    }

    public string ConnectionString => $"ipt:{_container.Hostname}:{_container.GetMappedPublicPort(3671)}";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
