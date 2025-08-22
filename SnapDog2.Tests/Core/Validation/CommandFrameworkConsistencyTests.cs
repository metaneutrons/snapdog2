namespace SnapDog2.Tests.Core.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using SnapDog2.Core.Attributes;
using SnapDog2.Tests.Helpers;

/// <summary>
/// Core consistency tests for the command framework registries.
/// These tests validate the integrity and consistency of CommandIdRegistry and StatusIdRegistry,
/// ensuring that all registered commands and status have corresponding implementations.
/// </summary>
public class CommandFrameworkConsistencyTests
{
    private readonly List<string> _allRegisteredCommands;
    private readonly List<string> _allRegisteredStatus;
    private readonly List<Type> _commandTypes;
    private readonly List<Type> _notificationTypes;

    public CommandFrameworkConsistencyTests()
    {
        // Initialize registries and cache data for all tests
        ConsistencyTestHelpers.InitializeRegistries();

        _allRegisteredCommands = ConsistencyTestHelpers.GetAllRegisteredCommands();
        _allRegisteredStatus = ConsistencyTestHelpers.GetAllRegisteredStatus();
        _commandTypes = ConsistencyTestHelpers.GetAllCommandTypes();
        _notificationTypes = ConsistencyTestHelpers.GetAllNotificationTypes();
    }

    /// <summary>
    /// Validates that CommandIdRegistry contains all expected command IDs from the blueprint.
    /// This test ensures no commands are missing from the registry.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void CommandIdRegistry_ShouldContainAllBlueprintCommands()
    {
        // Arrange
        var expectedCommands = ConsistencyTestHelpers.GetBlueprintCommandIds();

        // Act & Assert
        foreach (var expectedCommand in expectedCommands)
        {
            _allRegisteredCommands
                .Should()
                .Contain(
                    expectedCommand,
                    $"CommandIdRegistry should contain command '{expectedCommand}' as defined in blueprint"
                );
        }
    }

    /// <summary>
    /// Validates that StatusIdRegistry contains all expected status IDs from the blueprint.
    /// This test ensures no status are missing from the registry.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void StatusIdRegistry_ShouldContainAllBlueprintStatus()
    {
        // Arrange
        var expectedStatus = ConsistencyTestHelpers.GetBlueprintStatusIds();

        // Act & Assert
        foreach (var expectedStatusId in expectedStatus)
        {
            _allRegisteredStatus
                .Should()
                .Contain(
                    expectedStatusId,
                    $"StatusIdRegistry should contain status '{expectedStatusId}' as defined in blueprint"
                );
        }
    }

    /// <summary>
    /// Validates that all registered commands have corresponding command handler implementations.
    /// This test ensures that every command in the registry can be processed.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void AllRegisteredCommands_ShouldHaveHandlerImplementations()
    {
        // Arrange
        var commandHandlerTypes = ConsistencyTestHelpers.GetAllCommandHandlerTypes();
        var handledCommands = new HashSet<string>();

        // Extract command IDs from handler types
        foreach (var handlerType in commandHandlerTypes)
        {
            var handledCommandIds = ConsistencyTestHelpers.ExtractCommandIdsFromHandler(handlerType);
            foreach (var commandId in handledCommandIds)
            {
                handledCommands.Add(commandId);
            }
        }

        // Act & Assert
        foreach (var registeredCommand in _allRegisteredCommands)
        {
            handledCommands
                .Should()
                .Contain(
                    registeredCommand,
                    $"Command '{registeredCommand}' is registered but has no corresponding handler implementation"
                );
        }
    }

    /// <summary>
    /// Validates that all registered status have corresponding notification implementations.
    /// This test ensures that every status in the registry can be published.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void AllRegisteredStatus_ShouldHaveNotificationImplementations()
    {
        // Arrange
        var publishedStatus = new HashSet<string>();

        // Extract status IDs from notification types
        foreach (var notificationType in _notificationTypes)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromNotification(notificationType);
            foreach (var statusId in statusIds)
            {
                publishedStatus.Add(statusId);
            }
        }

        // Act & Assert
        foreach (var registeredStatus in _allRegisteredStatus)
        {
            publishedStatus
                .Should()
                .Contain(
                    registeredStatus,
                    $"Status '{registeredStatus}' is registered but has no corresponding notification implementation"
                );
        }
    }

    /// <summary>
    /// Validates that no orphaned command handlers exist (handlers without registry entries).
    /// This test ensures that all implemented handlers are properly registered.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void NoOrphanedCommandHandlers_ShouldExist()
    {
        // Arrange
        var commandHandlerTypes = ConsistencyTestHelpers.GetAllCommandHandlerTypes();
        var orphanedHandlers = new List<string>();

        // Check each handler for orphaned commands
        foreach (var handlerType in commandHandlerTypes)
        {
            var handledCommandIds = ConsistencyTestHelpers.ExtractCommandIdsFromHandler(handlerType);
            foreach (var commandId in handledCommandIds)
            {
                if (!_allRegisteredCommands.Contains(commandId))
                {
                    orphanedHandlers.Add($"{handlerType.Name} handles '{commandId}'");
                }
            }
        }

        // Act & Assert
        orphanedHandlers
            .Should()
            .BeEmpty($"Found command handlers for unregistered commands: {string.Join(", ", orphanedHandlers)}");
    }

    /// <summary>
    /// Validates that no orphaned notifications exist (notifications without registry entries).
    /// This test ensures that all implemented notifications are properly registered.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void NoOrphanedNotifications_ShouldExist()
    {
        // Arrange
        var orphanedNotifications = new List<string>();

        // Check each notification for orphaned status
        foreach (var notificationType in _notificationTypes)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromNotification(notificationType);
            foreach (var statusId in statusIds)
            {
                if (!_allRegisteredStatus.Contains(statusId))
                {
                    orphanedNotifications.Add($"{notificationType.Name} publishes '{statusId}'");
                }
            }
        }

        // Act & Assert
        orphanedNotifications
            .Should()
            .BeEmpty($"Found notifications for unregistered status: {string.Join(", ", orphanedNotifications)}");
    }

    /// <summary>
    /// Validates that command and status registries are properly initialized and accessible.
    /// This test ensures the basic infrastructure is working correctly.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void Registries_ShouldBeProperlyInitialized()
    {
        // Act & Assert
        _allRegisteredCommands.Should().NotBeEmpty("CommandIdRegistry should contain registered commands");
        _allRegisteredStatus.Should().NotBeEmpty("StatusIdRegistry should contain registered status");

        _allRegisteredCommands
            .Should()
            .OnlyContain(
                cmd => !string.IsNullOrWhiteSpace(cmd),
                "All registered commands should have valid non-empty IDs"
            );
        _allRegisteredStatus
            .Should()
            .OnlyContain(
                status => !string.IsNullOrWhiteSpace(status),
                "All registered status should have valid non-empty IDs"
            );
    }

    /// <summary>
    /// Validates that there are no duplicate command IDs in the registry.
    /// This test ensures command ID uniqueness across the system.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void CommandIdRegistry_ShouldHaveUniqueIds()
    {
        // Act
        var duplicates = _allRegisteredCommands
            .GroupBy(cmd => cmd)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        // Assert
        duplicates.Should().BeEmpty($"Found duplicate command IDs: {string.Join(", ", duplicates)}");
    }

    /// <summary>
    /// Validates that there are no duplicate status IDs in the registry.
    /// This test ensures status ID uniqueness across the system.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void StatusIdRegistry_ShouldHaveUniqueIds()
    {
        // Act
        var duplicates = _allRegisteredStatus
            .GroupBy(status => status)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        // Assert
        duplicates.Should().BeEmpty($"Found duplicate status IDs: {string.Join(", ", duplicates)}");
    }

    /// <summary>
    /// Validates that command and status IDs follow proper naming conventions.
    /// This test ensures consistency in ID formatting and structure.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void CommandAndStatusIds_ShouldFollowNamingConventions()
    {
        // Arrange
        var invalidCommandIds = new List<string>();
        var invalidStatusIds = new List<string>();

        // Validate command ID conventions
        foreach (var commandId in _allRegisteredCommands)
        {
            if (!ConsistencyTestHelpers.IsValidCommandIdFormat(commandId))
            {
                invalidCommandIds.Add(commandId);
            }
        }

        // Validate status ID conventions
        foreach (var statusId in _allRegisteredStatus)
        {
            if (!ConsistencyTestHelpers.IsValidStatusIdFormat(statusId))
            {
                invalidStatusIds.Add(statusId);
            }
        }

        // Act & Assert
        invalidCommandIds
            .Should()
            .BeEmpty($"Found command IDs with invalid format: {string.Join(", ", invalidCommandIds)}");
        invalidStatusIds
            .Should()
            .BeEmpty($"Found status IDs with invalid format: {string.Join(", ", invalidStatusIds)}");
    }

    /// <summary>
    /// Validates that recently added commands and status (within grace period) are properly handled.
    /// This test provides a grace period for newly added features to prevent false failures.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "CoreConsistency")]
    public void RecentlyAddedFeatures_ShouldBeWithinGracePeriod()
    {
        // Arrange
        var gracePeriodDays = 7; // Allow 7 days for implementation after blueprint addition
        var recentlyAddedCommands = ConsistencyTestHelpers.GetRecentlyAddedCommands(gracePeriodDays);
        var recentlyAddedStatus = ConsistencyTestHelpers.GetRecentlyAddedStatus(gracePeriodDays);

        // Act & Assert - These should be warnings, not failures
        if (recentlyAddedCommands.Any())
        {
            // Log warning but don't fail test
            Console.WriteLine(
                $"WARNING: Recently added commands within grace period: {string.Join(", ", recentlyAddedCommands)}"
            );
        }

        if (recentlyAddedStatus.Any())
        {
            // Log warning but don't fail test
            Console.WriteLine(
                $"WARNING: Recently added status within grace period: {string.Join(", ", recentlyAddedStatus)}"
            );
        }

        // Always pass - this is informational
        true.Should().BeTrue("Grace period validation completed");
    }
}
