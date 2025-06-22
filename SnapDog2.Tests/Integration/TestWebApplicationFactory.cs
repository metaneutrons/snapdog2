using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration tests that properly configures mock services
/// and ensures the test environment works correctly with authentication middleware.
/// </summary>
/// <typeparam name="TProgram">The program type to create the factory for</typeparam>
public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly Mock<IKnxService> _mockKnxService;
    private readonly Mock<IMqttService> _mockMqttService;
    private readonly Mock<ISnapcastService> _mockSnapcastService;
    private readonly Mock<IMediator> _mockMediator;

    public TestWebApplicationFactory()
    {
        _mockKnxService = new Mock<IKnxService>();
        _mockMqttService = new Mock<IMqttService>();
        _mockSnapcastService = new Mock<ISnapcastService>();
        _mockMediator = new Mock<IMediator>();
    }

    public Mock<IKnxService> MockKnxService => _mockKnxService;
    public Mock<IMqttService> MockMqttService => _mockMqttService;
    public Mock<ISnapcastService> MockSnapcastService => _mockSnapcastService;
    public Mock<IMediator> MockMediator => _mockMediator;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                // Override configuration for test environment
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ApiAuthentication:ValidApiKeys:0"] = "test-api-key",
                        ["ApiAuthentication:ValidApiKeys:1"] = "admin-key",
                        ["RateLimiting:Enabled"] = "false", // Disable rate limiting for tests
                        ["RequestResponseLogging:Enabled"] = "false", // Disable logging for cleaner test output
                        ["InputValidation:Enabled"] = "true", // Keep validation enabled for testing
                    }
                );
            }
        );

        builder.ConfigureServices(services =>
        {
            // Remove any existing service registrations that we want to mock
            RemoveService<IKnxService>(services);
            RemoveService<IMqttService>(services);
            RemoveService<ISnapcastService>(services);
            RemoveService<IMediator>(services);

            // Register our mocks
            services.AddSingleton(_mockKnxService.Object);
            services.AddSingleton(_mockMqttService.Object);
            services.AddSingleton(_mockSnapcastService.Object);
            services.AddSingleton(_mockMediator.Object);

            // Ensure we have the required authentication configuration
            var authConfig = new ApiConfiguration.ApiAuthSettings
            {
                ApiKeys = new List<string> { "test-api-key", "admin-key" },
            };
            services.RemoveAll<ApiConfiguration.ApiAuthSettings>();
            services.AddSingleton(authConfig);

            // Configure logging for tests
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce log noise in tests
                builder.AddFilter("Microsoft.AspNetCore", LogLevel.Error);
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error);
            });
        });

        builder.UseEnvironment("Test");
    }

    /// <summary>
    /// Removes a service registration from the service collection.
    /// </summary>
    /// <typeparam name="T">The service type to remove</typeparam>
    /// <param name="services">The service collection</param>
    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Resets all mocks to their default state.
    /// Call this method between tests to ensure clean state.
    /// </summary>
    public void ResetMocks()
    {
        _mockKnxService.Reset();
        _mockMqttService.Reset();
        _mockSnapcastService.Reset();
        _mockMediator.Reset();
    }
}
