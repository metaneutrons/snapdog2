using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Tests.Helpers.Extensions;

namespace SnapDog2.Tests.Fixtures.WebApp;

/// <summary>
/// Enterprise-grade web application factory for integration testing with full service stack.
/// Provides a realistic test environment with proper configuration and logging.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public CustomWebApplicationFactory() { }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                // Clear existing configuration
                config.Sources.Clear();

                // Add test-specific configuration
                config
                    .AddJsonFile("appsettings.Test.json", optional: true)
                    .AddInMemoryCollection(GetTestConfiguration()) // Base test configuration
                    .AddEnvironmentVariables("SNAPDOG_") // Regular SnapDog environment variables (higher priority)
                    .AddEnvironmentVariables("SNAPDOG_TEST_"); // Test-specific overrides (highest priority)
            }
        );

        builder.ConfigureServices(services =>
        {
            // Configure test-specific services
            ConfigureTestServices(services);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();

            logging.AddConsole();
            logging.AddDebug();

            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        builder.UseEnvironment("Test");
    }

    /// <summary>
    /// Configure test-specific services and overrides
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override services for testing as needed
        // This can be overridden in derived factories for specific test scenarios
    }

    /// <summary>
    /// Get test-specific configuration values
    /// </summary>
    protected virtual Dictionary<string, string?> GetTestConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["SNAPDOG_API_ENABLED"] = "true",
            ["SNAPDOG_API_AUTH_ENABLED"] = "false",
            ["SNAPDOG_SYSTEM_LOG_LEVEL"] = "Information",
            ["SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED"] = "true",
            ["SNAPDOG_SERVICES_KNX_ENABLED"] = "false",
            ["SNAPDOG_SERVICES_SUBSONIC_ENABLED"] = "false",
            ["SNAPDOG_TELEMETRY_ENABLED"] = "false",
        };
    }

    public virtual async Task InitializeAsync()
    {
        // Perform any async initialization
        await Task.CompletedTask;
    }

    // Explicit interface implementation for IAsyncLifetime
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        // Perform any async cleanup
        await base.DisposeAsync();
    }
}

/// <summary>
/// Specialized factory for testing with disabled API and minimal services
/// </summary>
public class ApiDisabledWebApplicationFactory : CustomWebApplicationFactory
{
    public ApiDisabledWebApplicationFactory() { }

    protected override Dictionary<string, string?> GetTestConfiguration()
    {
        var config = base.GetTestConfiguration();
        config["SNAPDOG_API_ENABLED"] = "false";
        config["SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED"] = "false";
        return config;
    }
}
