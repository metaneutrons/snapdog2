namespace SnapDog2.Tests.Api;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

[Collection("ApiDisabled")]
public class ApiConfigurationTests : IClassFixture<ApiDisabledWebApplicationFactory>
{
    private readonly ApiDisabledWebApplicationFactory _factory;

    public ApiConfigurationTests(ApiDisabledWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DisabledApi_ShouldNotHaveControllersRegistered()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act & Assert
        // When API is disabled, no controllers should be registered
        // This should result in either 404 or 503 depending on the test server behavior
        var response = await client.GetAsync("/api/health");

        // The exact status code may vary, but it should not be a successful response
        response.IsSuccessStatusCode.Should().BeFalse();

        // It should be either 404 (Not Found) or 503 (Service Unavailable)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable);
    }
}

[Collection("ApiEnabled")]
public class ApiEnabledConfigurationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiEnabledConfigurationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task EnabledApi_ShouldHaveControllersRegistered()
    {
        // Arrange - Check if API and health checks are enabled
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<SnapDog2.Core.Configuration.SnapDogConfiguration>();

        // Skip test if API or health checks are disabled
        if (!config.Api.Enabled)
        {
            return; // Skip test - API is disabled, controllers not registered
        }

        if (!config.System.HealthChecksEnabled)
        {
            return; // Skip test - Health checks are disabled
        }

        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        // When API is enabled, controllers should be registered and health endpoint should work
        // We expect either OK (200) or ServiceUnavailable (503) depending on health check results
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        // It should NOT be NotFound (404) since the controller should be registered
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}

public class ApiDisabledWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _originalValues = new();
    private static readonly object _lock = new object();

    public ApiDisabledWebApplicationFactory()
    {
        lock (_lock)
        {
            // Store original values and set new ones
            SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_LEVEL", "Warning");
            SetEnvironmentVariable("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED", "false");
            SetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_FILE", "");
            SetEnvironmentVariable("SNAPDOG_API_ENABLED", "false"); // DISABLE API for this test
            SetEnvironmentVariable("SNAPDOG_API_PORT", "0");
            SetEnvironmentVariable("SNAPDOG_API_AUTH_ENABLED", "false");
            SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS", "localhost");
            SetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_JSONRPC_PORT", "1704");
            SetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_ENABLED", "false");
            SetEnvironmentVariable("SNAPDOG_SERVICES_KNX_ENABLED", "false");
            SetEnvironmentVariable("SNAPDOG_SERVICES_SUBSONIC_ENABLED", "false");
        }
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        _originalValues[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to bypass command-line parsing
        builder.UseEnvironment("Testing");

        // No additional configuration needed - EnvoyConfig will read from environment variables
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                // Restore original environment variable values
                foreach (var kvp in _originalValues)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
                _originalValues.Clear();
            }
        }
        base.Dispose(disposing);
    }
}
