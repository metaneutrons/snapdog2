namespace SnapDog2.Tests.Core.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using SnapDog2.Tests.Helpers;

/// <summary>
/// Consistency tests for MQTT protocol implementation.
/// These tests validate that all registered commands and status have corresponding
/// MQTT handlers and that the MQTT layer properly implements the command framework.
/// </summary>
public class MqttProtocolConsistencyTests
{
    private readonly List<string> _allRegisteredCommands;
    private readonly List<string> _allRegisteredStatus;
    private readonly List<Type> _mqttHandlerTypes;
    private readonly List<MethodInfo> _mqttHandlerMethods;

    public MqttProtocolConsistencyTests()
    {
        // Initialize test data
        ConsistencyTestHelpers.InitializeRegistries();

        _allRegisteredCommands = ConsistencyTestHelpers.GetAllRegisteredCommands();
        _allRegisteredStatus = ConsistencyTestHelpers.GetAllRegisteredStatus();
        _mqttHandlerTypes = ConsistencyTestHelpers.GetAllMqttHandlerTypes();
        _mqttHandlerMethods = ConsistencyTestHelpers.GetAllMqttHandlerMethods();
    }

    /// <summary>
    /// Validates that all registered commands have corresponding MQTT handlers.
    /// This test ensures that every command can be invoked via MQTT messages.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void AllRegisteredCommands_ShouldHaveMqttHandlers()
    {
        // Arrange
        var commandsWithHandlers = new HashSet<string>();

        // Extract command IDs from MQTT handlers
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            var commandIds = ConsistencyTestHelpers.ExtractCommandIdsFromMqttHandler(handlerMethod);
            foreach (var commandId in commandIds)
            {
                commandsWithHandlers.Add(commandId);
            }
        }

        // Act & Assert
        foreach (var registeredCommand in _allRegisteredCommands)
        {
            commandsWithHandlers
                .Should()
                .Contain(
                    registeredCommand,
                    $"Command '{registeredCommand}' is registered but has no corresponding MQTT handler"
                );
        }
    }

    /// <summary>
    /// Validates that all registered status have corresponding MQTT publishers.
    /// This test ensures that every status can be published via MQTT messages.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void AllRegisteredStatus_ShouldHaveMqttPublishers()
    {
        // Arrange
        var statusWithPublishers = new HashSet<string>();

        // Extract status IDs from MQTT publishers
        foreach (var handlerType in _mqttHandlerTypes)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromMqttPublisher(handlerType);
            foreach (var statusId in statusIds)
            {
                statusWithPublishers.Add(statusId);
            }
        }

        // Act & Assert
        foreach (var registeredStatus in _allRegisteredStatus)
        {
            statusWithPublishers
                .Should()
                .Contain(
                    registeredStatus,
                    $"Status '{registeredStatus}' is registered but has no corresponding MQTT publisher"
                );
        }
    }

    /// <summary>
    /// Validates that MQTT handlers follow proper topic naming conventions.
    /// This test ensures consistency in MQTT topic structure and organization.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttHandlers_ShouldFollowTopicNamingConventions()
    {
        // Arrange
        var handlersWithInvalidTopics = new List<string>();

        // Validate MQTT topic naming conventions
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            var topics = ConsistencyTestHelpers.ExtractMqttTopicsFromHandler(handlerMethod);
            foreach (var topic in topics)
            {
                if (!ConsistencyTestHelpers.IsValidMqttTopicFormat(topic))
                {
                    handlersWithInvalidTopics.Add(
                        $"{handlerMethod.DeclaringType?.Name}.{handlerMethod.Name} -> {topic}"
                    );
                }
            }
        }

        // Act & Assert
        handlersWithInvalidTopics
            .Should()
            .BeEmpty($"Found MQTT handlers with invalid topic naming: {string.Join(", ", handlersWithInvalidTopics)}");
    }

    /// <summary>
    /// Validates that MQTT handlers have proper message serialization/deserialization.
    /// This test ensures that all MQTT messages can be properly processed.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttHandlers_ShouldHaveProperSerialization()
    {
        // Arrange
        var handlersWithoutSerialization = new List<string>();

        // Check handlers for proper serialization support
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            if (!ConsistencyTestHelpers.HasProperMqttSerialization(handlerMethod))
            {
                handlersWithoutSerialization.Add($"{handlerMethod.DeclaringType?.Name}.{handlerMethod.Name}");
            }
        }

        // Act & Assert
        handlersWithoutSerialization
            .Should()
            .BeEmpty(
                $"Found MQTT handlers without proper serialization: {string.Join(", ", handlersWithoutSerialization)}"
            );
    }

    /// <summary>
    /// Validates that MQTT command handlers use CommandFactory appropriately.
    /// This test ensures that command creation follows the established patterns.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttCommandHandlers_ShouldUseCommandFactory()
    {
        // Arrange
        var handlersNotUsingFactory = new List<string>();

        // Check command handlers for CommandFactory usage
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            var commandIds = ConsistencyTestHelpers.ExtractCommandIdsFromMqttHandler(handlerMethod);
            if (commandIds.Any())
            {
                if (!ConsistencyTestHelpers.UsesCommandFactory(handlerMethod))
                {
                    handlersNotUsingFactory.Add($"{handlerMethod.DeclaringType?.Name}.{handlerMethod.Name}");
                }
            }
        }

        // Act & Assert
        handlersNotUsingFactory
            .Should()
            .BeEmpty(
                $"Found MQTT command handlers not using CommandFactory: {string.Join(", ", handlersNotUsingFactory)}"
            );
    }

    /// <summary>
    /// Validates that MQTT status publishers use StatusIdAttribute appropriately.
    /// This test ensures that status publishing follows the established patterns.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttStatusPublishers_ShouldUseStatusIdAttribute()
    {
        // Arrange
        var publishersNotUsingAttribute = new List<string>();

        // Check status publishers for StatusIdAttribute usage
        foreach (var handlerType in _mqttHandlerTypes)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromMqttPublisher(handlerType);
            if (statusIds.Any())
            {
                if (!ConsistencyTestHelpers.UsesStatusIdAttribute(handlerType))
                {
                    publishersNotUsingAttribute.Add(handlerType.Name);
                }
            }
        }

        // Act & Assert
        publishersNotUsingAttribute
            .Should()
            .BeEmpty(
                $"Found MQTT status publishers not using StatusIdAttribute: {string.Join(", ", publishersNotUsingAttribute)}"
            );
    }

    /// <summary>
    /// Validates that MQTT handlers have proper error handling and logging.
    /// This test ensures that MQTT operations handle failures gracefully.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttHandlers_ShouldHaveProperErrorHandling()
    {
        // Arrange
        var handlersWithoutErrorHandling = new List<string>();

        // Check handlers for error handling patterns
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            if (!ConsistencyTestHelpers.HasProperMqttErrorHandling(handlerMethod))
            {
                handlersWithoutErrorHandling.Add($"{handlerMethod.DeclaringType?.Name}.{handlerMethod.Name}");
            }
        }

        // Act & Assert
        handlersWithoutErrorHandling
            .Should()
            .BeEmpty(
                $"Found MQTT handlers without proper error handling: {string.Join(", ", handlersWithoutErrorHandling)}"
            );
    }

    /// <summary>
    /// Validates that MQTT topic subscriptions are properly configured.
    /// This test ensures that all necessary topics are subscribed to for message handling.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttSubscriptions_ShouldBeProperlyConfigured()
    {
        // Arrange
        var requiredTopics = new HashSet<string>();
        var configuredTopics = ConsistencyTestHelpers.GetConfiguredMqttTopics();

        // Extract required topics from handlers
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            var topics = ConsistencyTestHelpers.ExtractMqttTopicsFromHandler(handlerMethod);
            foreach (var topic in topics)
            {
                requiredTopics.Add(topic);
            }
        }

        // Act & Assert
        foreach (var requiredTopic in requiredTopics)
        {
            configuredTopics
                .Should()
                .Contain(
                    requiredTopic,
                    $"MQTT topic '{requiredTopic}' is required by handlers but not configured for subscription"
                );
        }
    }

    /// <summary>
    /// Validates that MQTT message QoS levels are appropriately configured.
    /// This test ensures that message delivery guarantees match the requirements.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttMessages_ShouldHaveAppropriateQoSLevels()
    {
        // Arrange
        var messagesWithInappropriateQoS = new List<string>();

        // Check QoS levels for different message types
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            var qosLevel = ConsistencyTestHelpers.GetMqttQoSLevel(handlerMethod);
            var messageType = ConsistencyTestHelpers.GetMqttMessageType(handlerMethod);

            if (!ConsistencyTestHelpers.IsAppropriateQoSLevel(messageType, qosLevel))
            {
                messagesWithInappropriateQoS.Add(
                    $"{handlerMethod.DeclaringType?.Name}.{handlerMethod.Name} ({messageType}: QoS {qosLevel})"
                );
            }
        }

        // Act & Assert
        messagesWithInappropriateQoS
            .Should()
            .BeEmpty(
                $"Found MQTT messages with inappropriate QoS levels: {string.Join(", ", messagesWithInappropriateQoS)}"
            );
    }

    /// <summary>
    /// Validates that MQTT retained message settings are properly configured.
    /// This test ensures that message retention follows the appropriate patterns.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "MqttConsistency")]
    public void MqttMessages_ShouldHaveProperRetentionSettings()
    {
        // Arrange
        var messagesWithImproperRetention = new List<string>();

        // Check retention settings for different message types
        foreach (var handlerMethod in _mqttHandlerMethods)
        {
            var isRetained = ConsistencyTestHelpers.IsMqttMessageRetained(handlerMethod);
            var messageType = ConsistencyTestHelpers.GetMqttMessageType(handlerMethod);

            if (!ConsistencyTestHelpers.IsAppropriateRetentionSetting(messageType, isRetained))
            {
                messagesWithImproperRetention.Add(
                    $"{handlerMethod.DeclaringType?.Name}.{handlerMethod.Name} ({messageType}: Retained={isRetained})"
                );
            }
        }

        // Act & Assert
        messagesWithImproperRetention
            .Should()
            .BeEmpty(
                $"Found MQTT messages with improper retention settings: {string.Join(", ", messagesWithImproperRetention)}"
            );
    }
}
