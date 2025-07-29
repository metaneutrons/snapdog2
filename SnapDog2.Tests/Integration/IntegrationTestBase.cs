using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace SnapDog2.Tests.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    public IContainer Container { get; }

    protected IntegrationTestBase(IContainer container)
    {
        Container = container;
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
