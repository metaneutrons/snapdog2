namespace SnapDog2.Tests.Core.Validation;

using FluentAssertions;
using SnapDog2.Tests.Blueprint;
using SnapDog2.Tests.Helpers;
using Xunit;

/// <summary>
/// Simplified blueprint validation tests.
/// Focuses on the most important validation: API endpoint coverage.
/// Uses static analysis to compare blueprint specification with actual controller implementations.
/// </summary>
public class BlueprintTests
{
    [Fact]
    [Trait("Category", "Blueprint")]
    public void Blueprint_Commands_ShouldHaveMatchingApiEndpoints()
    {
        // Act
        var (missingCommands, _) = StaticApiAnalyzer.CompareCommandImplementation();

        // Assert
        missingCommands
            .Should()
            .BeEmpty($"Missing API implementations for required commands: {string.Join(", ", missingCommands)}");
    }

    [Fact]
    [Trait("Category", "Blueprint")]
    public void Blueprint_Status_ShouldHaveMatchingApiEndpoints()
    {
        // Act
        var (missingStatus, _) = StaticApiAnalyzer.CompareStatusImplementation();

        // Assert
        missingStatus
            .Should()
            .BeEmpty($"Missing API implementations for required status: {string.Join(", ", missingStatus)}");
    }

    [Fact]
    [Trait("Category", "Blueprint")]
    public void Blueprint_ShouldBeWellFormed()
    {
        // Arrange & Act
        var commands = SnapDogBlueprint.Spec.Commands.ToList();
        var status = SnapDogBlueprint.Spec.Status.ToList();

        // Assert
        commands.Should().NotBeEmpty("Blueprint should define commands");
        status.Should().NotBeEmpty("Blueprint should define status");

        // Ensure all API commands have paths and methods
        var apiCommands = commands.Where(c => c.HasApi).ToList();
        foreach (var cmd in apiCommands)
        {
            cmd.ApiPath.Should().NotBeNullOrEmpty($"Command {cmd.Id} should have an API path");
            cmd.HttpMethod.Should().NotBeNullOrEmpty($"Command {cmd.Id} should have an HTTP method");
        }

        // Ensure all API status have paths and methods
        var apiStatus = status.Where(s => s.HasApi).ToList();
        foreach (var stat in apiStatus)
        {
            stat.ApiPath.Should().NotBeNullOrEmpty($"Status {stat.Id} should have an API path");
            stat.HttpMethod.Should().NotBeNullOrEmpty($"Status {stat.Id} should have an HTTP method");
        }
    }
}
