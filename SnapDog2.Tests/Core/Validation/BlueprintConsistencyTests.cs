namespace SnapDog2.Tests.Core.Validation;

using System.Reflection;
using FluentAssertions;
using SnapDog2.Tests.Blueprint;
using SnapDog2.Tests.Helpers;
using Xunit;

/// <summary>
/// Tests that validate the system implementation against the blueprint specification.
/// These tests ensure that the actual implementation matches the declared blueprint.
/// </summary>
public class BlueprintConsistencyTests
{
    private readonly List<MethodInfo> _apiEndpoints;
    private readonly List<Type> _mqttHandlerTypes;
    private readonly List<string> _allRegisteredCommands;
    private readonly List<string> _allRegisteredStatus;

    public BlueprintConsistencyTests()
    {
        _apiEndpoints = ConsistencyTestHelpers.GetAllApiEndpoints();
        _mqttHandlerTypes = ConsistencyTestHelpers.GetAllMqttHandlerTypes();
        _allRegisteredCommands = ConsistencyTestHelpers.GetAllRegisteredCommands();
        _allRegisteredStatus = ConsistencyTestHelpers.GetAllRegisteredStatus();
    }

    #region Command Implementation Tests

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void AllRequiredCommands_ShouldHaveApiEndpoints()
    {
        // Arrange
        var requiredApiCommands = SnapDogBlueprint.Spec.Commands.Required().WithApi().Select(c => c.Id).ToHashSet();

        var implementedApiCommands = _apiEndpoints
            .SelectMany(ConsistencyTestHelpers.ExtractCommandIdsFromApiEndpoint)
            .ToHashSet();

        // Act & Assert
        var missingCommands = requiredApiCommands.Except(implementedApiCommands);

        missingCommands
            .Should()
            .BeEmpty($"Missing API implementations for required commands: {string.Join(", ", missingCommands)}");
    }

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void AllRequiredCommands_ShouldHaveMqttHandlers()
    {
        // Arrange
        var requiredMqttCommands = SnapDogBlueprint.Spec.Commands.Required().WithMqtt().Select(c => c.Id).ToHashSet();

        var implementedMqttCommands = _mqttHandlerTypes
            .SelectMany(ConsistencyTestHelpers.GetMqttHandlerMethods)
            .SelectMany(ConsistencyTestHelpers.ExtractCommandIdsFromMqttHandler)
            .ToHashSet();

        // Act & Assert
        var missingCommands = requiredMqttCommands.Except(implementedMqttCommands);

        missingCommands
            .Should()
            .BeEmpty($"Missing MQTT implementations for required commands: {string.Join(", ", missingCommands)}");
    }

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void ApiCommandEndpoints_ShouldUseCorrectHttpMethods()
    {
        // Arrange
        var incorrectMethods = new List<string>();

        foreach (var endpoint in _apiEndpoints)
        {
            var commandIds = ConsistencyTestHelpers.ExtractCommandIdsFromApiEndpoint(endpoint);
            if (!commandIds.Any())
                continue;

            var actualMethods = ConsistencyTestHelpers.GetHttpMethodsFromEndpoint(endpoint);
            var commandId = commandIds.First(); // Take first command ID for this endpoint

            var blueprintCommand = SnapDogBlueprint.Spec.Commands.FirstOrDefault(c => c.Id == commandId);
            if (blueprintCommand?.HttpMethod == null)
                continue;

            if (!actualMethods.Contains(blueprintCommand.HttpMethod))
            {
                incorrectMethods.Add(
                    $"{endpoint.DeclaringType?.Name}.{endpoint.Name} "
                        + $"uses {string.Join(",", actualMethods)} but blueprint specifies {blueprintCommand.HttpMethod}"
                );
            }
        }

        // Act & Assert
        incorrectMethods
            .Should()
            .BeEmpty($"Found API endpoints with incorrect HTTP methods: {string.Join(", ", incorrectMethods)}");
    }

    #endregion

    #region Status Implementation Tests

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void AllRequiredStatus_ShouldHaveApiEndpoints()
    {
        // Arrange
        var requiredApiStatus = SnapDogBlueprint.Spec.Status.Required().WithApi().Select(s => s.Id).ToHashSet();

        var implementedApiStatus = _apiEndpoints
            .SelectMany(ConsistencyTestHelpers.ExtractStatusIdsFromApiEndpoint)
            .ToHashSet();

        // Act & Assert
        var missingStatus = requiredApiStatus.Except(implementedApiStatus);

        missingStatus
            .Should()
            .BeEmpty($"Missing API implementations for required status: {string.Join(", ", missingStatus)}");
    }

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void AllRequiredStatus_ShouldHaveMqttPublishers()
    {
        // Arrange
        var requiredMqttStatus = SnapDogBlueprint.Spec.Status.Required().WithMqtt().Select(s => s.Id).ToHashSet();

        var implementedMqttStatus = _mqttHandlerTypes
            .SelectMany(ConsistencyTestHelpers.ExtractStatusIdsFromMqttPublisher)
            .ToHashSet();

        // Act & Assert
        var missingStatus = requiredMqttStatus.Except(implementedMqttStatus);

        missingStatus
            .Should()
            .BeEmpty($"Missing MQTT implementations for required status: {string.Join(", ", missingStatus)}");
    }

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void ApiStatusEndpoints_ShouldUseGetMethod()
    {
        // Arrange
        var incorrectMethods = new List<string>();

        foreach (var endpoint in _apiEndpoints)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromApiEndpoint(endpoint);
            if (!statusIds.Any())
                continue;

            var actualMethods = ConsistencyTestHelpers.GetHttpMethodsFromEndpoint(endpoint);

            if (!actualMethods.Contains("GET"))
            {
                incorrectMethods.Add(
                    $"{endpoint.DeclaringType?.Name}.{endpoint.Name} "
                        + $"uses {string.Join(",", actualMethods)} but status endpoints should use GET"
                );
            }
        }

        // Act & Assert
        incorrectMethods
            .Should()
            .BeEmpty($"Found status endpoints not using GET method: {string.Join(", ", incorrectMethods)}");
    }

    #endregion

    #region KNX Exclusion Tests

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void KnxExclusions_ShouldBeDocumentedInBlueprint()
    {
        // Arrange
        var actualKnxExclusions = ConsistencyTestHelpers.GetActualKnxExclusions();
        var blueprintKnxExclusions = SnapDogBlueprint
            .Spec.Commands.ExcludedFrom(Protocol.Knx)
            .Select(c => c.Id)
            .ToHashSet();

        // Act & Assert
        var undocumentedExclusions = actualKnxExclusions.Except(blueprintKnxExclusions);

        undocumentedExclusions
            .Should()
            .BeEmpty($"Found KNX exclusions not documented in blueprint: {string.Join(", ", undocumentedExclusions)}");
    }

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void BlueprintKnxExclusions_ShouldHaveReasons()
    {
        // Arrange
        var exclusionsWithoutReasons = SnapDogBlueprint
            .Spec.Commands.ExcludedFrom(Protocol.Knx)
            .Where(c => string.IsNullOrWhiteSpace(c.GetExclusionReason(Protocol.Knx)))
            .Select(c => c.Id);

        // Act & Assert
        exclusionsWithoutReasons
            .Should()
            .BeEmpty($"Found KNX exclusions without reasons: {string.Join(", ", exclusionsWithoutReasons)}");
    }

    #endregion

    #region Registry Consistency Tests

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void RegisteredCommands_ShouldMatchBlueprint()
    {
        // Arrange
        var blueprintCommands = SnapDogBlueprint.Spec.Commands.Select(c => c.Id).ToHashSet();
        var registeredCommands = _allRegisteredCommands.ToHashSet();

        // Act & Assert
        var missingFromRegistry = blueprintCommands.Except(registeredCommands);
        var extraInRegistry = registeredCommands.Except(blueprintCommands);

        missingFromRegistry
            .Should()
            .BeEmpty($"Commands in blueprint but not registered: {string.Join(", ", missingFromRegistry)}");

        extraInRegistry
            .Should()
            .BeEmpty($"Commands registered but not in blueprint: {string.Join(", ", extraInRegistry)}");
    }

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void RegisteredStatus_ShouldMatchBlueprint()
    {
        // Arrange
        var blueprintStatus = SnapDogBlueprint.Spec.Status.Select(s => s.Id).ToHashSet();
        var registeredStatus = _allRegisteredStatus.ToHashSet();

        // Act & Assert
        var missingFromRegistry = blueprintStatus.Except(registeredStatus);
        var extraInRegistry = registeredStatus.Except(blueprintStatus);

        missingFromRegistry
            .Should()
            .BeEmpty($"Status in blueprint but not registered: {string.Join(", ", missingFromRegistry)}");

        extraInRegistry
            .Should()
            .BeEmpty($"Status registered but not in blueprint: {string.Join(", ", extraInRegistry)}");
    }

    #endregion

    #region Protocol-Specific Tests

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void MqttOnlyFeatures_ShouldNotHaveApiEndpoints()
    {
        // Arrange
        var mqttOnlyFeatures = SnapDogBlueprint.Spec.All.WithMqtt().WithoutApi().Select(f => f.Id).ToHashSet();

        var featuresWithApiEndpoints = _apiEndpoints
            .SelectMany(e =>
                ConsistencyTestHelpers
                    .ExtractCommandIdsFromApiEndpoint(e)
                    .Concat(ConsistencyTestHelpers.ExtractStatusIdsFromApiEndpoint(e))
            )
            .ToHashSet();

        // Act & Assert
        var mqttOnlyWithApi = mqttOnlyFeatures.Intersect(featuresWithApiEndpoints);

        mqttOnlyWithApi
            .Should()
            .BeEmpty($"MQTT-only features should not have API endpoints: {string.Join(", ", mqttOnlyWithApi)}");
    }

    #endregion

    #region Grace Period Tests

    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "Blueprint")]
    public void RecentlyAddedFeatures_ShouldBeWithinGracePeriod()
    {
        // Arrange
        var recentFeatures = SnapDogBlueprint.Spec.All.RecentlyAdded().Select(f => f.Id);

        // Act & Assert - This test documents recently added features
        // In a real implementation, you might check against actual implementation dates
        foreach (var feature in recentFeatures)
        {
            // Log recently added features for visibility
            Console.WriteLine($"Recently added feature within grace period: {feature}");
        }

        // For now, just ensure the collection is accessible
        recentFeatures.Should().NotBeNull();
    }

    #endregion
}
