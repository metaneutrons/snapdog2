using System.Net.Http;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace SnapDog2.Tests.Integration.Fixtures;

/// <summary>
/// Starts the SnapDog2 app inside a .NET SDK container with proper bind mounts and working directory
/// so that dotnet watch/dotnet run can find and launch the project. Exposes an HttpClient for tests.
/// </summary>
public class AppContainerFixture : IAsyncLifetime
{
    private IContainer? _container;
    private int _hostPort;

    public HttpClient HttpClient { get; private set; } = new();
    public Uri BaseAddress => new($"http://localhost:{_hostPort}");

    public async Task InitializeAsync()
    {
        // Resolve repo root by walking up from the test directory until SnapDog2.sln is found
        var repoRoot =
            FindRepoRoot() ?? throw new InvalidOperationException("Could not locate repo root (SnapDog2.sln)");

        // Choose the app port via config (Kestrel binds to SNAPDOG_API_PORT, not ASPNETCORE_URLS)
        const int appPort = 5000;

        var builder = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/dotnet/sdk:9.0")
            .WithWorkingDirectory("/app")
            .WithBindMount(repoRoot, "/app", AccessMode.ReadWrite)
            .WithPortBinding(0, appPort) // map to random host port for parallel test safety
            .WithEnvironment(
                new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                    ["SNAPDOG_API_ENABLED"] = "true",
                    ["SNAPDOG_API_PORT"] = appPort.ToString(),
                    ["SNAPDOG_API_AUTH_ENABLED"] = "false",
                }
            )
            // Use absolute project path so dotnet run can resolve and launch the project deterministically
            .WithCommand("dotnet", "run", "--project", "/app/SnapDog2", "-c", "Debug")
            .WithWaitStrategy(
                // Rely on port availability only to avoid log-capture flakiness
                Wait.ForUnixContainer().UntilPortIsAvailable(appPort)
            );

        _container = builder.Build();
        await _container.StartAsync();

        _hostPort = _container.GetMappedPublicPort(appPort);

        // Configure HttpClient for tests
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_hostPort}"),
            Timeout = TimeSpan.FromSeconds(15),
        };
    }

    public async Task DisposeAsync()
    {
        try
        {
            HttpClient.Dispose();
        }
        finally
        {
            if (_container is not null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
            }
        }
    }

    private static string? FindRepoRoot()
    {
        // Start from the current directory and walk upwards
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "SnapDog2.sln");
            if (File.Exists(sln))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}

/// <summary>
/// XUnit collection to ensure a single app container is started for dependent tests.
/// </summary>
[CollectionDefinition("AppContainer")]
public class AppContainerCollection : ICollectionFixture<AppContainerFixture> { }
