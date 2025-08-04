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
                        ["SNAPDOG_SYSTEM_LOGLEVEL"] = "Warning",
                        ["SNAPDOG_SYSTEM_HEALTHCHECKSENABLED"] = "true",
                        ["SNAPDOG_SYSTEM_LOGFILE"] = "",
                        ["SNAPDOG_API_ENABLED"] = "true", // Enable API for tests
                        ["SNAPDOG_API_PORT"] = "0", // Use any available port for tests
                        ["SNAPDOG_API_AUTH_ENABLED"] = "false", // Disable auth for tests
                        ["SNAPDOG_SERVICES_SNAPCAST_ADDRESS"] = "localhost",
                        ["SNAPDOG_SERVICES_SNAPCAST_JSONRPCPORT"] = "1704",
                        ["SNAPDOG_SERVICES_MQTT_ENABLED"] = "false",
                        ["SNAPDOG_SERVICES_KNX_ENABLED"] = "false",
                        ["SNAPDOG_SERVICES_SUBSONIC_ENABLED"] = "false",
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
