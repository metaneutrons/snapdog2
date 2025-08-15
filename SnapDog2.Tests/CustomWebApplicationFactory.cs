namespace SnapDog2.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _originalValues = new();
    private static readonly object _lock = new object();

    public CustomWebApplicationFactory()
    {
        lock (_lock)
        {
            // Store original values and set new ones
            this.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            this.SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_LEVEL", "Warning");
            this.SetEnvironmentVariable("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "true");
            this.SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_FILE", ""); // Disable log file to avoid permission issues
            this.SetEnvironmentVariable("SNAPDOG_API_ENABLED", "true"); // ENABLE API for tests
            this.SetEnvironmentVariable("SNAPDOG_API_PORT", "0");
            this.SetEnvironmentVariable("SNAPDOG_API_AUTH_ENABLED", "false");
            this.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", "localhost");
            this.SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", "1704");
            this.SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_ENABLED", "false");
            this.SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_ENABLED", "false");
            this.SetEnvironmentVariable("SNAPDOG_SERVICES_SUBSONIC_ENABLED", "false");
        }
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        this._originalValues[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to bypass command-line parsing
        builder.UseEnvironment("Testing");

        // Configure services for testing
        builder.ConfigureServices(services =>
        {
            // Configure minimal logging for tests
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            // Remove hosted services that cause issues in tests
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                // Restore original environment variable values
                foreach (var kvp in this._originalValues)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }

                this._originalValues.Clear();
            }
        }
        base.Dispose(disposing);
    }
}
