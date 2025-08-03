namespace SnapDog2.Tests.Api;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    public ApiDisabledWebApplicationFactory()
    {
        // Set the environment variable before any host creation
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to bypass command-line parsing
        builder.UseEnvironment("Testing");

        // Override configuration with test values - API DISABLED
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["SNAPDOG_SYSTEM__LOGLEVEL"] = "Warning",
                        ["SNAPDOG_SYSTEM__HEALTHCHECKSENABLED"] = "false",
                        ["SNAPDOG_SYSTEM__LOGFILE"] = "",
                        ["SNAPDOG_API__ENABLED"] = "false", // DISABLE API for this test
                        ["SNAPDOG_API__PORT"] = "0",
                        ["SNAPDOG_API__AUTH_ENABLED"] = "false",
                        ["SNAPDOG_SERVICES__SNAPCAST__ADDRESS"] = "localhost",
                        ["SNAPDOG_SERVICES__SNAPCAST__JSONRPCPORT"] = "1704",
                        ["SNAPDOG_SERVICES__MQTT__ENABLED"] = "false",
                        ["SNAPDOG_SERVICES__KNX__ENABLED"] = "false",
                        ["SNAPDOG_SERVICES__SUBSONIC__ENABLED"] = "false",
                    }
                );
            }
        );

        // No additional service configuration needed for this test
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
