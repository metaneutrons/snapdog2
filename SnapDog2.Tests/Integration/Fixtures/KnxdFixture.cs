namespace SnapDog2.Tests.Integration.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Xunit;

public class KnxdFixture : IAsyncLifetime
{
    private IContainer? _knxdContainer;
    private IFutureDockerImage? _image;

    public string KnxHost { get; private set; } = "localhost";
    public int KnxTcpPort { get; private set; } = 0; // 6720 inside container

    public async Task InitializeAsync()
    {
        // Build the knxd image from devcontainer/knxd to keep in sync with dev setup
        var repoRoot = GetRepoRoot();
        var knxdDir = Path.Combine(repoRoot, "devcontainer", "knxd");

        _image = new ImageFromDockerfileBuilder()
            .WithName($"snapdog-knxd-tests:{Guid.NewGuid():N}")
            .WithDockerfileDirectory(knxdDir)
            .WithDockerfile("Dockerfile")
            .Build();

        await _image.CreateAsync();

        // Start knxd container, map TCP 6720 to random host port; UDP 3671 optional
        _knxdContainer = new ContainerBuilder()
            .WithImage(_image)
            .WithName($"snapdog-tests-knxd-{Guid.NewGuid():N}")
            .WithPortBinding(6720, true) // TCP control port
            .WithEnvironment("ADDRESS", "1.1.128")
            .WithEnvironment("CLIENT_ADDRESS", "1.1.129:8")
            .WithEnvironment("DEBUG_LEVEL", "info")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6720))
            .Build();

        await _knxdContainer.StartAsync();

        KnxTcpPort = _knxdContainer.GetMappedPublicPort(6720);
        KnxHost = "localhost";

        // Secondary readiness probe: attempt TCP connect to mapped 6720 for up to ~20s
        var ready = false;
        for (int i = 0; i < 20 && !ready; i++)
        {
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient();
                var connectTask = tcp.ConnectAsync(KnxHost, KnxTcpPort);
                var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(1)));
                if (completed == connectTask && tcp.Connected)
                {
                    ready = true;
                    break;
                }
            }
            catch
            {
                // ignore and retry
            }
        }

        if (!ready)
        {
            throw new TimeoutException($"knxd did not accept TCP connections on {KnxHost}:{KnxTcpPort} within timeout");
        }

        // Expose to app factory via environment variables for tests
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_KNX_HOST", KnxHost);
        Environment.SetEnvironmentVariable("SNAPDOG_TEST_KNX_TCP_PORT", KnxTcpPort.ToString());
    }

    public async Task DisposeAsync()
    {
        if (_knxdContainer is not null)
        {
            try
            {
                await _knxdContainer.StopAsync();
            }
            catch
            { /* ignore */
            }
            await _knxdContainer.DisposeAsync();
        }
    }

    private static string GetRepoRoot()
    {
        // SnapDog2.sln sits at the repo root; resolve upwards from current directory
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "SnapDog2.sln")))
            {
                return dir;
            }
            var parent = Directory.GetParent(dir)?.FullName;
            if (string.Equals(parent, dir, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            dir = parent ?? string.Empty;
        }
        // Fallback to current working directory
        return Directory.GetCurrentDirectory();
    }
}

[CollectionDefinition("KnxdContainer")]
public class KnxdContainerCollection : ICollectionFixture<KnxdFixture> { }
