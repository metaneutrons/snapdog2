using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace SnapDog2.Tests.Integration.Fixtures;

public class KnxdFixture : IAsyncLifetime
{
    private IContainer? _tcpContainer;

    public string KnxHost { get; private set; } = "localhost";
    public int KnxTcpPort { get; private set; }

    public async Task InitializeAsync()
    {
        // Provide a simple TCP listener on 6720 to satisfy connectivity and health checks.
        // Using Alpine + socat for a lightweight TCP server.
        const int knxPort = 6720;

        _tcpContainer = new ContainerBuilder()
            .WithImage("alpine:3")
            .WithCommand("/bin/sh", "-c", "apk add --no-cache socat && socat -v tcp-listen:6720,fork,reuseaddr -")
            .WithPortBinding(0, knxPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(knxPort))
            .Build();

        await _tcpContainer.StartAsync();

        KnxTcpPort = _tcpContainer.GetMappedPublicPort(knxPort);
        KnxHost = "localhost";

        // Expose for factories/tests that read env vars
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_KNX_HOST", KnxHost);
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_KNX_TCP_PORT", KnxTcpPort.ToString());
    }

    public async Task DisposeAsync()
    {
        try { }
        finally
        {
            if (_tcpContainer is not null)
            {
                await _tcpContainer.StopAsync();
                await _tcpContainer.DisposeAsync();
            }
        }
    }
}

[CollectionDefinition("KnxdContainer")]
public class KnxdContainerCollection : ICollectionFixture<KnxdFixture> { }
