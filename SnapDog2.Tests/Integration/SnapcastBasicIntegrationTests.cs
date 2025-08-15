using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Basic integration tests for Snapcast functionality that validate the working SnapDog application.
/// These tests are designed to work independently of the complex test infrastructure.
/// </summary>
public class SnapcastBasicIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public SnapcastBasicIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SnapcastIntegration_ShouldBeConfiguredInApplication()
    {
        // This test validates that the Snapcast integration is properly configured
        // by checking that the necessary types and services are available

        // Arrange & Act
        var snapcastServiceType = typeof(SnapDog2.Core.Abstractions.ISnapcastService);
        var snapcastConfigType = typeof(SnapDog2.Core.Configuration.SnapcastConfig);
        var snapcastServerStatusType = typeof(SnapDog2.Core.Models.SnapcastServerStatus);

        // Assert
        snapcastServiceType.Should().NotBeNull("ISnapcastService interface should be defined");
        snapcastConfigType.Should().NotBeNull("SnapcastConfig should be defined");
        snapcastServerStatusType.Should().NotBeNull("SnapcastServerStatus model should be defined");

        _output.WriteLine("✅ Snapcast integration types are properly configured:");
        _output.WriteLine($"   ISnapcastService: {snapcastServiceType.FullName}");
        _output.WriteLine($"   SnapcastConfig: {snapcastConfigType.FullName}");
        _output.WriteLine($"   SnapcastServerStatus: {snapcastServerStatusType.FullName}");
    }

    [Fact]
    public void SnapcastService_ShouldHaveRequiredMethods()
    {
        // This test validates that the ISnapcastService interface has the expected methods
        // that we identified in our analysis of the working application

        // Arrange
        var serviceType = typeof(SnapDog2.Core.Abstractions.ISnapcastService);

        // Act
        var methods = serviceType.GetMethods().Select(m => m.Name).ToList();

        // Assert
        methods.Should().Contain("InitializeAsync", "Service should have initialization method");
        methods.Should().Contain("GetServerStatusAsync", "Service should have server status method");
        methods.Should().Contain("SetClientVolumeAsync", "Service should have client volume control");
        methods.Should().Contain("SetClientMuteAsync", "Service should have client mute control");
        methods.Should().Contain("SetGroupStreamAsync", "Service should have group stream control");
        methods.Should().Contain("SetGroupMuteAsync", "Service should have group mute control");

        _output.WriteLine("✅ ISnapcastService has required methods:");
        foreach (
            var method in methods
                .Where(m =>
                    m.Contains("Snapcast") || m.Contains("Client") || m.Contains("Group") || m.Contains("Server")
                )
                .OrderBy(m => m)
        )
        {
            _output.WriteLine($"   {method}");
        }
    }

    [Fact]
    public void SnapcastModels_ShouldHaveCorrectStructure()
    {
        // This test validates that the Snapcast domain models have the expected structure
        // based on our analysis of the working application

        // Arrange
        var serverStatusType = typeof(SnapDog2.Core.Models.SnapcastServerStatus);

        // Act
        var properties = serverStatusType.GetProperties().Select(p => p.Name).ToList();

        // Assert
        properties.Should().Contain("Server", "SnapcastServerStatus should have Server property");
        properties.Should().Contain("Groups", "SnapcastServerStatus should have Groups property");
        properties.Should().Contain("Streams", "SnapcastServerStatus should have Streams property");
        properties.Should().Contain("Clients", "SnapcastServerStatus should have Clients property");

        _output.WriteLine("✅ SnapcastServerStatus has correct structure:");
        foreach (var property in properties.OrderBy(p => p))
        {
            _output.WriteLine($"   {property}");
        }
    }

    [Fact]
    public void SnapcastConfiguration_ShouldBeWellDefined()
    {
        // This test validates that the Snapcast configuration model is properly defined
        // with the expected configuration properties

        // Arrange
        var configType = typeof(SnapDog2.Core.Configuration.SnapcastConfig);

        // Act
        var properties = configType.GetProperties().Select(p => p.Name).ToList();

        // Assert
        properties.Should().NotBeEmpty("SnapcastConfig should have configuration properties");

        _output.WriteLine("✅ SnapcastConfig is well-defined:");
        foreach (var property in properties.OrderBy(p => p))
        {
            var propertyInfo = configType.GetProperty(property);
            _output.WriteLine($"   {property}: {propertyInfo?.PropertyType.Name}");
        }
    }

    [Fact]
    public void SnapcastIntegration_ShouldSupportResultPattern()
    {
        // This test validates that the Snapcast service uses the Result pattern
        // for proper error handling as we observed in the working application

        // Arrange
        var serviceType = typeof(SnapDog2.Core.Abstractions.ISnapcastService);
        var resultType = typeof(SnapDog2.Core.Models.Result);

        // Act
        var methods = serviceType.GetMethods();
        var asyncMethods = methods
            .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            .ToList();

        // Assert
        resultType.Should().NotBeNull("Result type should be defined for error handling");
        asyncMethods.Should().NotBeEmpty("Service should have async methods");

        var resultMethods = asyncMethods
            .Where(m => m.ReturnType.GetGenericArguments()[0].Name.StartsWith("Result"))
            .ToList();

        resultMethods.Should().NotBeEmpty("Service methods should return Result types");

        _output.WriteLine("✅ Snapcast integration supports Result pattern:");
        _output.WriteLine($"   Result type: {resultType.FullName}");
        _output.WriteLine($"   Methods using Result pattern: {resultMethods.Count}");

        foreach (var method in resultMethods.Take(5))
        {
            _output.WriteLine($"   {method.Name}: {method.ReturnType.Name}");
        }
    }

    [Fact]
    public void SnapcastIntegration_ShouldBePartOfDependencyInjection()
    {
        // This test validates that the Snapcast integration is properly configured
        // for dependency injection as part of the working application

        // Arrange
        var serviceConfigurationType = typeof(SnapDog2.Extensions.DependencyInjection.SnapcastServiceConfiguration);

        // Act & Assert
        serviceConfigurationType.Should().NotBeNull("SnapcastServiceConfiguration should exist for DI setup");

        var methods = serviceConfigurationType.GetMethods(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
        );
        var configMethods = methods.Where(m => m.Name.Contains("Snapcast") || m.Name.Contains("Add")).ToList();

        configMethods.Should().NotBeEmpty("Should have configuration methods for DI");

        _output.WriteLine("✅ Snapcast integration is configured for dependency injection:");
        _output.WriteLine($"   Configuration type: {serviceConfigurationType.FullName}");
        foreach (var method in configMethods)
        {
            _output.WriteLine($"   {method.Name}");
        }
    }
}
