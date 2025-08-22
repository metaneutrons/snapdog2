namespace SnapDog2.Tests.Core.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SnapDog2.Tests.Helpers;

/// <summary>
/// Cross-protocol consistency tests that validate feature parity and consistency
/// across API, MQTT, and KNX protocol implementations.
/// </summary>
public class CrossProtocolConsistencyTests
{
    private readonly List<string> _allRegisteredCommands;
    private readonly List<string> _allRegisteredStatus;
    private readonly HashSet<string> _apiCommands;
    private readonly HashSet<string> _mqttCommands;
    private readonly HashSet<string> _knxCommands;
    private readonly HashSet<string> _apiStatus;
    private readonly HashSet<string> _mqttStatus;
    private readonly HashSet<string> _knxStatus;

    public CrossProtocolConsistencyTests()
    {
        // Initialize test data
        ConsistencyTestHelpers.InitializeRegistries();

        _allRegisteredCommands = ConsistencyTestHelpers.GetAllRegisteredCommands();
        _allRegisteredStatus = ConsistencyTestHelpers.GetAllRegisteredStatus();

        // Extract protocol-specific implementations
        _apiCommands = ConsistencyTestHelpers.GetApiImplementedCommands();
        _mqttCommands = ConsistencyTestHelpers.GetMqttImplementedCommands();
        _knxCommands = ConsistencyTestHelpers.GetKnxImplementedCommands();

        _apiStatus = ConsistencyTestHelpers.GetApiImplementedStatus();
        _mqttStatus = ConsistencyTestHelpers.GetMqttImplementedStatus();
        _knxStatus = ConsistencyTestHelpers.GetKnxImplementedStatus();
    }

    /// <summary>
    /// Validates that API and MQTT protocols have feature parity for commands.
    /// This test ensures that commands available via API are also available via MQTT.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void ApiAndMqtt_ShouldHaveCommandParity()
    {
        // Arrange
        var apiOnlyCommands = _apiCommands.Except(_mqttCommands).ToList();
        var mqttOnlyCommands = _mqttCommands.Except(_apiCommands).ToList();

        // Act & Assert
        apiOnlyCommands.Should().BeEmpty($"Found commands only in API protocol: {string.Join(", ", apiOnlyCommands)}");
        mqttOnlyCommands
            .Should()
            .BeEmpty($"Found commands only in MQTT protocol: {string.Join(", ", mqttOnlyCommands)}");
    }

    /// <summary>
    /// Validates that API and MQTT protocols have feature parity for status.
    /// This test ensures that status available via API are also available via MQTT.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void ApiAndMqtt_ShouldHaveStatusParity()
    {
        // Arrange
        var apiOnlyStatus = _apiStatus.Except(_mqttStatus).ToList();
        var mqttOnlyStatus = _mqttStatus.Except(_apiStatus).ToList();

        // Act & Assert
        apiOnlyStatus.Should().BeEmpty($"Found status only in API protocol: {string.Join(", ", apiOnlyStatus)}");
        mqttOnlyStatus.Should().BeEmpty($"Found status only in MQTT protocol: {string.Join(", ", mqttOnlyStatus)}");
    }

    /// <summary>
    /// Validates KNX protocol exclusions are properly documented and justified.
    /// This test ensures that KNX limitations are explicitly handled and documented.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void Knx_ShouldHaveDocumentedExclusions()
    {
        // Arrange
        var knxExcludedCommands = _allRegisteredCommands.Except(_knxCommands).ToList();
        var knxExcludedStatus = _allRegisteredStatus.Except(_knxStatus).ToList();
        var documentedExclusions = ConsistencyTestHelpers.GetDocumentedKnxExclusions();

        // Act & Assert
        foreach (var excludedCommand in knxExcludedCommands)
        {
            documentedExclusions
                .Should()
                .Contain(
                    excludedCommand,
                    $"Command '{excludedCommand}' is excluded from KNX but not documented as an exclusion"
                );
        }

        foreach (var excludedStatus in knxExcludedStatus)
        {
            documentedExclusions
                .Should()
                .Contain(
                    excludedStatus,
                    $"Status '{excludedStatus}' is excluded from KNX but not documented as an exclusion"
                );
        }
    }

    /// <summary>
    /// Validates that KNX exclusions are justified by protocol limitations.
    /// This test ensures that KNX exclusions are based on valid technical reasons.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void Knx_ExclusionsShouldBeJustified()
    {
        // Arrange
        var knxExcludedCommands = _allRegisteredCommands.Except(_knxCommands).ToList();
        var knxExcludedStatus = _allRegisteredStatus.Except(_knxStatus).ToList();
        var unjustifiedExclusions = new List<string>();

        // Check if exclusions are justified
        foreach (var excludedCommand in knxExcludedCommands)
        {
            if (!ConsistencyTestHelpers.IsKnxExclusionJustified(excludedCommand))
            {
                unjustifiedExclusions.Add($"Command: {excludedCommand}");
            }
        }

        foreach (var excludedStatus in knxExcludedStatus)
        {
            if (!ConsistencyTestHelpers.IsKnxExclusionJustified(excludedStatus))
            {
                unjustifiedExclusions.Add($"Status: {excludedStatus}");
            }
        }

        // Act & Assert
        unjustifiedExclusions
            .Should()
            .BeEmpty($"Found unjustified KNX exclusions: {string.Join(", ", unjustifiedExclusions)}");
    }

    /// <summary>
    /// Validates that protocol-specific implementations maintain semantic consistency.
    /// This test ensures that the same command/status behaves consistently across protocols.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void CrossProtocol_ShouldMaintainSemanticConsistency()
    {
        // Arrange
        var semanticInconsistencies = new List<string>();
        var commonCommands = _apiCommands.Intersect(_mqttCommands).Intersect(_knxCommands);
        var commonStatus = _apiStatus.Intersect(_mqttStatus).Intersect(_knxStatus);

        // Check semantic consistency for commands
        foreach (var command in commonCommands)
        {
            if (!ConsistencyTestHelpers.HasConsistentCommandSemantics(command))
            {
                semanticInconsistencies.Add($"Command '{command}' has inconsistent semantics across protocols");
            }
        }

        // Check semantic consistency for status
        foreach (var status in commonStatus)
        {
            if (!ConsistencyTestHelpers.HasConsistentStatusSemantics(status))
            {
                semanticInconsistencies.Add($"Status '{status}' has inconsistent semantics across protocols");
            }
        }

        // Act & Assert
        semanticInconsistencies
            .Should()
            .BeEmpty($"Found semantic inconsistencies: {string.Join(", ", semanticInconsistencies)}");
    }

    /// <summary>
    /// Validates that error handling is consistent across protocols.
    /// This test ensures that similar errors are handled similarly across all protocols.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void CrossProtocol_ShouldHaveConsistentErrorHandling()
    {
        // Arrange
        var errorHandlingInconsistencies = new List<string>();
        var commonCommands = _apiCommands.Intersect(_mqttCommands).ToList();

        // Check error handling consistency
        foreach (var command in commonCommands)
        {
            var apiErrorHandling = ConsistencyTestHelpers.GetApiErrorHandlingPattern(command);
            var mqttErrorHandling = ConsistencyTestHelpers.GetMqttErrorHandlingPattern(command);

            if (!ConsistencyTestHelpers.AreErrorHandlingPatternsConsistent(apiErrorHandling, mqttErrorHandling))
            {
                errorHandlingInconsistencies.Add(
                    $"Command '{command}' has inconsistent error handling between API and MQTT"
                );
            }
        }

        // Act & Assert
        errorHandlingInconsistencies
            .Should()
            .BeEmpty($"Found error handling inconsistencies: {string.Join(", ", errorHandlingInconsistencies)}");
    }

    /// <summary>
    /// Validates that data validation is consistent across protocols.
    /// This test ensures that input validation rules are applied consistently.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void CrossProtocol_ShouldHaveConsistentDataValidation()
    {
        // Arrange
        var validationInconsistencies = new List<string>();
        var commonCommands = _apiCommands.Intersect(_mqttCommands).ToList();

        // Check validation consistency
        foreach (var command in commonCommands)
        {
            var apiValidation = ConsistencyTestHelpers.GetApiValidationRules(command);
            var mqttValidation = ConsistencyTestHelpers.GetMqttValidationRules(command);

            if (!ConsistencyTestHelpers.AreValidationRulesConsistent(apiValidation, mqttValidation))
            {
                validationInconsistencies.Add($"Command '{command}' has inconsistent validation between API and MQTT");
            }
        }

        // Act & Assert
        validationInconsistencies
            .Should()
            .BeEmpty($"Found validation inconsistencies: {string.Join(", ", validationInconsistencies)}");
    }

    /// <summary>
    /// Validates that response formats are consistent across protocols.
    /// This test ensures that similar data is formatted consistently across protocols.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void CrossProtocol_ShouldHaveConsistentResponseFormats()
    {
        // Arrange
        var formatInconsistencies = new List<string>();
        var commonStatus = _apiStatus.Intersect(_mqttStatus).ToList();

        // Check response format consistency
        foreach (var status in commonStatus)
        {
            var apiFormat = ConsistencyTestHelpers.GetApiResponseFormat(status);
            var mqttFormat = ConsistencyTestHelpers.GetMqttResponseFormat(status);

            if (!ConsistencyTestHelpers.AreResponseFormatsConsistent(apiFormat, mqttFormat))
            {
                formatInconsistencies.Add($"Status '{status}' has inconsistent response formats between API and MQTT");
            }
        }

        // Act & Assert
        formatInconsistencies
            .Should()
            .BeEmpty($"Found response format inconsistencies: {string.Join(", ", formatInconsistencies)}");
    }

    /// <summary>
    /// Validates that protocol-specific optimizations don't break consistency.
    /// This test ensures that performance optimizations maintain functional consistency.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void ProtocolOptimizations_ShouldMaintainConsistency()
    {
        // Arrange
        var optimizationInconsistencies = new List<string>();
        var optimizedFeatures = ConsistencyTestHelpers.GetProtocolOptimizedFeatures();

        // Check that optimizations don't break consistency
        foreach (var feature in optimizedFeatures)
        {
            if (!ConsistencyTestHelpers.DoesOptimizationMaintainConsistency(feature))
            {
                optimizationInconsistencies.Add(
                    $"Feature '{feature.FeatureName}' optimization in {feature.Protocol} breaks consistency"
                );
            }
        }

        // Act & Assert
        optimizationInconsistencies
            .Should()
            .BeEmpty($"Found optimization inconsistencies: {string.Join(", ", optimizationInconsistencies)}");
    }

    /// <summary>
    /// Validates that all protocols properly implement the command framework contracts.
    /// This test ensures that protocol implementations adhere to the framework interfaces.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CrossProtocol")]
    public void AllProtocols_ShouldImplementFrameworkContracts()
    {
        // Arrange
        var contractViolations = new List<string>();

        // Check API contract compliance
        var apiViolations = ConsistencyTestHelpers.GetApiContractViolations();
        contractViolations.AddRange(apiViolations.Select(v => $"API: {v}"));

        // Check MQTT contract compliance
        var mqttViolations = ConsistencyTestHelpers.GetMqttContractViolations();
        contractViolations.AddRange(mqttViolations.Select(v => $"MQTT: {v}"));

        // Check KNX contract compliance
        var knxViolations = ConsistencyTestHelpers.GetKnxContractViolations();
        contractViolations.AddRange(knxViolations.Select(v => $"KNX: {v}"));

        // Act & Assert
        contractViolations
            .Should()
            .BeEmpty($"Found framework contract violations: {string.Join(", ", contractViolations)}");
    }
}
