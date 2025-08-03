namespace SnapDog2.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        // Set the environment variable before any host creation
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to bypass command-line parsing
        builder.UseEnvironment("Testing");

        // Override configuration with test values
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["SNAPDOG_SYSTEM__LOGLEVEL"] = "Warning",
                        ["SNAPDOG_SYSTEM__HEALTHCHECKSENABLED"] = "true",
                        ["SNAPDOG_SYSTEM__LOGFILE"] = "",
                        ["SNAPDOG_API__ENABLED"] = "true", // Enable API for tests
                        ["SNAPDOG_API__PORT"] = "0", // Use any available port for tests
                        ["SNAPDOG_API__AUTH_ENABLED"] = "false", // Disable auth for tests
                        ["SNAPDOG_SERVICES__SNAPCAST__ADDRESS"] = "localhost",
                        ["SNAPDOG_SERVICES__SNAPCAST__JSONRPCPORT"] = "1704",
                        ["SNAPDOG_SERVICES__MQTT__ENABLED"] = "false",
                        ["SNAPDOG_SERVICES__KNX__ENABLED"] = "false",
                        ["SNAPDOG_SERVICES__SUBSONIC__ENABLED"] = "false",
                    }
                );
            }
        );

        // Configure services for testing
        builder.ConfigureServices(services =>
        {
            // Configure minimal logging for tests
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up the environment variable
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
        base.Dispose(disposing);
    }
}
