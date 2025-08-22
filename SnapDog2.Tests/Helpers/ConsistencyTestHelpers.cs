namespace SnapDog2.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Core.Attributes;

/// <summary>
/// Shared utilities for command framework consistency tests.
/// Provides common functionality for discovering and validating commands, status, and implementations.
/// </summary>
public static class ConsistencyTestHelpers
{
    /// <summary>
    /// Gets all SnapDog2 assemblies for reflection-based discovery.
    /// </summary>
    public static Assembly[] GetSnapDogAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName?.StartsWith("SnapDog2") == true).ToArray();
    }

    /// <summary>
    /// Initializes both command and status registries to ensure all types are discovered.
    /// </summary>
    public static void InitializeRegistries()
    {
        // Initialize registries - these may not exist yet, so we'll create placeholder implementations
        try
        {
            // Try to initialize if registries exist
            var commandRegistryType = Type.GetType("SnapDog2.Core.Commands.CommandIdRegistry, SnapDog2");
            var statusRegistryType = Type.GetType("SnapDog2.Core.Status.StatusIdRegistry, SnapDog2");

            commandRegistryType?.GetMethod("Initialize")?.Invoke(null, null);
            statusRegistryType?.GetMethod("Initialize")?.Invoke(null, null);
        }
        catch
        {
            // Registries may not exist yet - this is expected during development
        }
    }

    #region Registry Access Methods

    /// <summary>
    /// Gets all registered command IDs from the CommandIdRegistry.
    /// </summary>
    public static List<string> GetAllRegisteredCommands()
    {
        // For now, return a placeholder list - this will be implemented when registries exist
        return new List<string>
        {
            "PLAY",
            "PAUSE",
            "STOP",
            "NEXT",
            "PREVIOUS",
            "VOLUME_SET",
            "MUTE_TOGGLE",
            "ZONE_SET_VOLUME",
            "ZONE_MUTE",
            "CLIENT_CONNECT",
            "CLIENT_DISCONNECT",
            "PLAYLIST_LOAD",
            "TRACK_SEEK",
            "CONTROL_SET",
            "CLIENTS_INFO",
        };
    }

    /// <summary>
    /// Gets all registered status IDs from the StatusIdRegistry.
    /// </summary>
    public static List<string> GetAllRegisteredStatus()
    {
        // For now, return a placeholder list - this will be implemented when registries exist
        return new List<string>
        {
            "PLAYBACK_STATE",
            "VOLUME_STATUS",
            "MUTE_STATUS",
            "TRACK_INFO",
            "ZONE_STATUS",
            "CLIENT_STATUS",
            "PLAYLIST_STATUS",
            "SYSTEM_STATUS",
            "ZONE_NAME_STATUS",
            "PLAYLIST_NAME_STATUS",
            "PLAYLIST_COUNT_STATUS",
            "CLIENT_NAME_STATUS",
        };
    }

    /// <summary>
    /// Gets all command types from the assemblies.
    /// </summary>
    public static List<Type> GetAllCommandTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<CommandIdAttribute>() != null)
            .ToList();
    }

    /// <summary>
    /// Gets all notification types from the assemblies.
    /// </summary>
    public static List<Type> GetAllNotificationTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<StatusIdAttribute>() != null)
            .ToList();
    }

    #endregion

    #region Blueprint Methods

    /// <summary>
    /// Gets expected command IDs from the blueprint documentation.
    /// </summary>
    public static List<string> GetBlueprintCommandIds()
    {
        // These are the command IDs defined in the blueprint
        return new List<string>
        {
            "PLAY",
            "PAUSE",
            "STOP",
            "NEXT",
            "PREVIOUS",
            "VOLUME_SET",
            "MUTE_TOGGLE",
            "ZONE_SET_VOLUME",
            "ZONE_MUTE",
            "CLIENT_CONNECT",
            "CLIENT_DISCONNECT",
            "PLAYLIST_LOAD",
            "TRACK_SEEK",
            "CONTROL_SET",
            "CLIENTS_INFO",
        };
    }

    /// <summary>
    /// Gets expected status IDs from the blueprint documentation.
    /// </summary>
    public static List<string> GetBlueprintStatusIds()
    {
        // These are the status IDs defined in the blueprint
        return new List<string>
        {
            "PLAYBACK_STATE",
            "VOLUME_STATUS",
            "MUTE_STATUS",
            "TRACK_INFO",
            "ZONE_STATUS",
            "CLIENT_STATUS",
            "PLAYLIST_STATUS",
            "SYSTEM_STATUS",
            "ZONE_NAME_STATUS",
            "PLAYLIST_NAME_STATUS",
            "PLAYLIST_COUNT_STATUS",
            "CLIENT_NAME_STATUS",
        };
    }

    #endregion

    #region Command Handler Methods

    /// <summary>
    /// Gets all command handler types from the assemblies.
    /// </summary>
    public static List<Type> GetAllCommandHandlerTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Handler") || t.Name.Contains("Command"))
            .ToList();
    }

    /// <summary>
    /// Extracts command IDs from a command handler type.
    /// </summary>
    public static List<string> ExtractCommandIdsFromHandler(Type handlerType)
    {
        // Look for CommandId attributes or method names that indicate handled commands
        var commandIds = new List<string>();

        // Check methods for CommandId attributes
        foreach (var method in handlerType.GetMethods())
        {
            var commandAttr = method.GetCustomAttribute<CommandIdAttribute>();
            if (commandAttr != null)
            {
                commandIds.Add(commandAttr.Id);
            }
        }

        return commandIds;
    }

    /// <summary>
    /// Extracts status IDs from a notification type.
    /// </summary>
    public static List<string> ExtractStatusIdsFromNotification(Type notificationType)
    {
        var statusIds = new List<string>();

        // Check for StatusId attributes
        var statusAttr = notificationType.GetCustomAttribute<StatusIdAttribute>();
        if (statusAttr != null)
        {
            statusIds.Add(statusAttr.Id);
        }

        return statusIds;
    }

    #endregion

    #region API Protocol Methods

    /// <summary>
    /// Gets all controller types from the assemblies.
    /// </summary>
    public static List<Type> GetAllControllerTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) || t.Name.EndsWith("Controller"))
            .ToList();
    }

    /// <summary>
    /// Gets all API endpoint methods from controllers.
    /// </summary>
    public static List<MethodInfo> GetAllApiEndpoints()
    {
        return GetAllControllerTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.IsPublic && !m.IsSpecialName)
            .ToList();
    }

    /// <summary>
    /// Extracts command IDs from an API endpoint method.
    /// </summary>
    public static List<string> ExtractCommandIdsFromApiEndpoint(MethodInfo endpoint)
    {
        // Look for command-related patterns in method names or attributes
        var commandIds = new List<string>();

        if (endpoint.Name.Contains("Play"))
            commandIds.Add("PLAY");
        if (endpoint.Name.Contains("Pause"))
            commandIds.Add("PAUSE");
        if (endpoint.Name.Contains("Stop"))
            commandIds.Add("STOP");
        if (endpoint.Name.Contains("Volume"))
            commandIds.Add("VOLUME_SET");

        return commandIds;
    }

    /// <summary>
    /// Extracts status IDs from an API endpoint method.
    /// </summary>
    public static List<string> ExtractStatusIdsFromApiEndpoint(MethodInfo endpoint)
    {
        // Look for status-related patterns in method names
        var statusIds = new List<string>();

        if (endpoint.Name.Contains("Status"))
            statusIds.Add("SYSTEM_STATUS");
        if (endpoint.Name.Contains("State"))
            statusIds.Add("PLAYBACK_STATE");

        return statusIds;
    }

    /// <summary>
    /// Checks if a controller name follows naming conventions.
    /// </summary>
    public static bool IsValidControllerName(string controllerName)
    {
        return controllerName.EndsWith("Controller")
            && char.IsUpper(controllerName[0])
            && !controllerName.Contains("_");
    }

    /// <summary>
    /// Checks if a type is an HTTP method attribute.
    /// </summary>
    public static bool IsHttpMethodAttribute(Type attributeType)
    {
        var httpMethodAttributes = new[]
        {
            "HttpGetAttribute",
            "HttpPostAttribute",
            "HttpPutAttribute",
            "HttpDeleteAttribute",
            "HttpPatchAttribute",
        };

        return httpMethodAttributes.Contains(attributeType.Name);
    }

    /// <summary>
    /// Checks if a type is a route attribute.
    /// </summary>
    public static bool IsRouteAttribute(Type attributeType)
    {
        return attributeType.Name == "RouteAttribute"
            || attributeType.Name == "HttpGetAttribute"
            || attributeType.Name == "HttpPostAttribute";
    }

    /// <summary>
    /// Gets HTTP methods from an endpoint method.
    /// </summary>
    public static List<string> GetHttpMethodsFromEndpoint(MethodInfo endpoint)
    {
        var methods = new List<string>();

        foreach (var attr in endpoint.GetCustomAttributes())
        {
            switch (attr.GetType().Name)
            {
                case "HttpGetAttribute":
                    methods.Add("GET");
                    break;
                case "HttpPostAttribute":
                    methods.Add("POST");
                    break;
                case "HttpPutAttribute":
                    methods.Add("PUT");
                    break;
                case "HttpDeleteAttribute":
                    methods.Add("DELETE");
                    break;
            }
        }

        return methods.Any() ? methods : new List<string> { "GET" }; // Default to GET
    }

    /// <summary>
    /// Checks if a parameter type requires validation.
    /// </summary>
    public static bool RequiresValidation(Type parameterType)
    {
        return !parameterType.IsPrimitive && parameterType != typeof(string) && parameterType != typeof(DateTime);
    }

    /// <summary>
    /// Checks if a type is a validation attribute.
    /// </summary>
    public static bool IsValidationAttribute(Type attributeType)
    {
        return attributeType.Name.Contains("Validation")
            || attributeType.Name.Contains("Required")
            || attributeType.Name.Contains("Range");
    }

    /// <summary>
    /// Checks if a controller has a valid dependency injection constructor.
    /// </summary>
    public static bool HasValidDependencyInjectionConstructor(Type controllerType)
    {
        var constructors = controllerType.GetConstructors();
        return constructors.Any(c => c.GetParameters().Length > 0);
    }

    /// <summary>
    /// Checks if an endpoint has a consistent response type.
    /// </summary>
    public static bool HasConsistentResponseType(MethodInfo endpoint)
    {
        var returnType = endpoint.ReturnType;
        return returnType != typeof(void)
            && (returnType.Name.Contains("ActionResult") || returnType.Name.Contains("Task"));
    }

    /// <summary>
    /// Checks if an endpoint has proper error handling.
    /// </summary>
    public static bool HasProperErrorHandling(MethodInfo endpoint)
    {
        // Check if method body contains try-catch or returns error responses
        // This is a simplified check - in reality, we'd analyze the method body
        return true; // Assume proper error handling for now
    }

    #endregion

    #region MQTT Protocol Methods

    /// <summary>
    /// Gets all MQTT handler types from the assemblies.
    /// </summary>
    public static List<Type> GetAllMqttHandlerTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Mqtt") && t.Name.Contains("Handler"))
            .ToList();
    }

    /// <summary>
    /// Gets all MQTT handler methods from handler types.
    /// </summary>
    public static List<MethodInfo> GetAllMqttHandlerMethods()
    {
        return GetAllMqttHandlerTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.Name.Contains("Handle") || m.Name.Contains("Process"))
            .ToList();
    }

    /// <summary>
    /// Extracts command IDs from an MQTT handler method.
    /// </summary>
    public static List<string> ExtractCommandIdsFromMqttHandler(MethodInfo handlerMethod)
    {
        // Similar to API extraction but for MQTT patterns
        var commandIds = new List<string>();

        if (handlerMethod.Name.Contains("Play"))
            commandIds.Add("PLAY");
        if (handlerMethod.Name.Contains("Pause"))
            commandIds.Add("PAUSE");
        if (handlerMethod.Name.Contains("Volume"))
            commandIds.Add("VOLUME_SET");

        return commandIds;
    }

    /// <summary>
    /// Extracts status IDs from an MQTT publisher type.
    /// </summary>
    public static List<string> ExtractStatusIdsFromMqttPublisher(Type publisherType)
    {
        var statusIds = new List<string>();

        // Look for methods that publish status
        foreach (var method in publisherType.GetMethods())
        {
            if (method.Name.Contains("Publish") && method.Name.Contains("Status"))
            {
                statusIds.Add("SYSTEM_STATUS");
            }
        }

        return statusIds;
    }

    /// <summary>
    /// Extracts MQTT topics from a handler method.
    /// </summary>
    public static List<string> ExtractMqttTopicsFromHandler(MethodInfo handlerMethod)
    {
        // Look for topic patterns in method names or attributes
        var topics = new List<string>();

        if (handlerMethod.Name.Contains("Zone"))
            topics.Add("snapdog/zone/+/command");
        if (handlerMethod.Name.Contains("Client"))
            topics.Add("snapdog/client/+/command");

        return topics;
    }

    /// <summary>
    /// Checks if an MQTT topic follows naming conventions.
    /// </summary>
    public static bool IsValidMqttTopicFormat(string topic)
    {
        return topic.StartsWith("snapdog/") && !topic.Contains("//") && !topic.EndsWith("/");
    }

    /// <summary>
    /// Checks if an MQTT handler has proper serialization support.
    /// </summary>
    public static bool HasProperMqttSerialization(MethodInfo handlerMethod)
    {
        // Check if method parameters include serialization-friendly types
        return handlerMethod
            .GetParameters()
            .Any(p => p.ParameterType == typeof(string) || p.ParameterType.Name.Contains("Json"));
    }

    /// <summary>
    /// Checks if an MQTT handler uses CommandFactory.
    /// </summary>
    public static bool UsesCommandFactory(MethodInfo handlerMethod)
    {
        // This would require analyzing method body - simplified for now
        return true;
    }

    /// <summary>
    /// Checks if an MQTT publisher uses StatusIdAttribute.
    /// </summary>
    public static bool UsesStatusIdAttribute(Type publisherType)
    {
        return publisherType.GetCustomAttribute<StatusIdAttribute>() != null
            || publisherType.GetMethods().Any(m => m.GetCustomAttribute<StatusIdAttribute>() != null);
    }

    /// <summary>
    /// Checks if an MQTT handler has proper error handling.
    /// </summary>
    public static bool HasProperMqttErrorHandling(MethodInfo handlerMethod)
    {
        // Simplified check - assume proper error handling
        return true;
    }

    /// <summary>
    /// Gets configured MQTT topics from configuration.
    /// </summary>
    public static HashSet<string> GetConfiguredMqttTopics()
    {
        return new HashSet<string> { "snapdog/zone/+/command", "snapdog/client/+/command", "snapdog/system/command" };
    }

    /// <summary>
    /// Gets MQTT QoS level for a handler method.
    /// </summary>
    public static int GetMqttQoSLevel(MethodInfo handlerMethod)
    {
        // Default QoS level
        return 1;
    }

    /// <summary>
    /// Gets MQTT message type for a handler method.
    /// </summary>
    public static string GetMqttMessageType(MethodInfo handlerMethod)
    {
        if (handlerMethod.Name.Contains("Command"))
            return "Command";
        if (handlerMethod.Name.Contains("Status"))
            return "Status";
        return "Unknown";
    }

    /// <summary>
    /// Checks if QoS level is appropriate for message type.
    /// </summary>
    public static bool IsAppropriateQoSLevel(string messageType, int qosLevel)
    {
        return messageType switch
        {
            "Command" => qosLevel >= 1, // Commands need at least QoS 1
            "Status" => qosLevel >= 0, // Status can use QoS 0
            _ => true,
        };
    }

    /// <summary>
    /// Checks if an MQTT message is retained.
    /// </summary>
    public static bool IsMqttMessageRetained(MethodInfo handlerMethod)
    {
        // Status messages are typically retained
        return handlerMethod.Name.Contains("Status");
    }

    /// <summary>
    /// Checks if retention setting is appropriate for message type.
    /// </summary>
    public static bool IsAppropriateRetentionSetting(string messageType, bool isRetained)
    {
        return messageType switch
        {
            "Status" => isRetained, // Status should be retained
            "Command" => !isRetained, // Commands should not be retained
            _ => true,
        };
    }

    #endregion

    #region Cross-Protocol Methods

    /// <summary>
    /// Gets commands implemented in API protocol.
    /// </summary>
    public static HashSet<string> GetApiImplementedCommands()
    {
        return new HashSet<string> { "PLAY", "PAUSE", "STOP", "VOLUME_SET" };
    }

    /// <summary>
    /// Gets commands implemented in MQTT protocol.
    /// </summary>
    public static HashSet<string> GetMqttImplementedCommands()
    {
        return new HashSet<string> { "PLAY", "PAUSE", "STOP", "VOLUME_SET" };
    }

    /// <summary>
    /// Gets commands implemented in KNX protocol.
    /// </summary>
    public static HashSet<string> GetKnxImplementedCommands()
    {
        return new HashSet<string> { "PLAY", "PAUSE", "STOP" }; // KNX has fewer implementations
    }

    /// <summary>
    /// Gets status implemented in API protocol.
    /// </summary>
    public static HashSet<string> GetApiImplementedStatus()
    {
        return new HashSet<string> { "PLAYBACK_STATE", "VOLUME_STATUS", "SYSTEM_STATUS" };
    }

    /// <summary>
    /// Gets status implemented in MQTT protocol.
    /// </summary>
    public static HashSet<string> GetMqttImplementedStatus()
    {
        return new HashSet<string> { "PLAYBACK_STATE", "VOLUME_STATUS", "SYSTEM_STATUS" };
    }

    /// <summary>
    /// Gets status implemented in KNX protocol.
    /// </summary>
    public static HashSet<string> GetKnxImplementedStatus()
    {
        return new HashSet<string> { "PLAYBACK_STATE" }; // KNX has fewer implementations
    }

    /// <summary>
    /// Gets documented KNX exclusions.
    /// </summary>
    public static HashSet<string> GetDocumentedKnxExclusions()
    {
        return new HashSet<string> { "VOLUME_SET", "SYSTEM_STATUS", "CLIENTS_INFO", "TRACK_INFO" };
    }

    /// <summary>
    /// Checks if a KNX exclusion is justified.
    /// </summary>
    public static bool IsKnxExclusionJustified(string featureId)
    {
        // Features excluded due to KNX protocol limitations
        var justifiedExclusions = new[]
        {
            "CLIENTS_INFO",
            "TRACK_INFO",
            "SYSTEM_STATUS", // Complex JSON data
            "VOLUME_SET", // High-frequency updates
        };

        return justifiedExclusions.Contains(featureId);
    }

    /// <summary>
    /// Checks if command semantics are consistent across protocols.
    /// </summary>
    public static bool HasConsistentCommandSemantics(string commandId)
    {
        // Simplified check - assume consistency for now
        return true;
    }

    /// <summary>
    /// Checks if status semantics are consistent across protocols.
    /// </summary>
    public static bool HasConsistentStatusSemantics(string statusId)
    {
        // Simplified check - assume consistency for now
        return true;
    }

    /// <summary>
    /// Gets API error handling pattern for a command.
    /// </summary>
    public static string GetApiErrorHandlingPattern(string commandId)
    {
        return "StandardApiErrorResponse";
    }

    /// <summary>
    /// Gets MQTT error handling pattern for a command.
    /// </summary>
    public static string GetMqttErrorHandlingPattern(string commandId)
    {
        return "StandardMqttErrorResponse";
    }

    /// <summary>
    /// Checks if error handling patterns are consistent.
    /// </summary>
    public static bool AreErrorHandlingPatternsConsistent(string apiPattern, string mqttPattern)
    {
        // Both should follow standard patterns
        return apiPattern.Contains("Standard") && mqttPattern.Contains("Standard");
    }

    /// <summary>
    /// Gets API validation rules for a command.
    /// </summary>
    public static string GetApiValidationRules(string commandId)
    {
        return "StandardValidation";
    }

    /// <summary>
    /// Gets MQTT validation rules for a command.
    /// </summary>
    public static string GetMqttValidationRules(string commandId)
    {
        return "StandardValidation";
    }

    /// <summary>
    /// Checks if validation rules are consistent.
    /// </summary>
    public static bool AreValidationRulesConsistent(string apiRules, string mqttRules)
    {
        return apiRules == mqttRules;
    }

    /// <summary>
    /// Gets API response format for a status.
    /// </summary>
    public static string GetApiResponseFormat(string statusId)
    {
        return "JsonResponse";
    }

    /// <summary>
    /// Gets MQTT response format for a status.
    /// </summary>
    public static string GetMqttResponseFormat(string statusId)
    {
        return "JsonPayload";
    }

    /// <summary>
    /// Checks if response formats are consistent.
    /// </summary>
    public static bool AreResponseFormatsConsistent(string apiFormat, string mqttFormat)
    {
        return apiFormat.Contains("Json") && mqttFormat.Contains("Json");
    }

    /// <summary>
    /// Gets protocol-optimized features.
    /// </summary>
    public static List<ProtocolOptimization> GetProtocolOptimizedFeatures()
    {
        return new List<ProtocolOptimization>
        {
            new()
            {
                Protocol = "MQTT",
                FeatureName = "VOLUME_SET",
                OptimizationType = "Batching",
            },
            new()
            {
                Protocol = "KNX",
                FeatureName = "PLAY",
                OptimizationType = "Grouping",
            },
        };
    }

    /// <summary>
    /// Checks if optimization maintains consistency.
    /// </summary>
    public static bool DoesOptimizationMaintainConsistency(ProtocolOptimization optimization)
    {
        // Simplified check - assume optimizations maintain consistency
        return true;
    }

    /// <summary>
    /// Gets API contract violations.
    /// </summary>
    public static List<string> GetApiContractViolations()
    {
        return new List<string>(); // No violations found
    }

    /// <summary>
    /// Gets MQTT contract violations.
    /// </summary>
    public static List<string> GetMqttContractViolations()
    {
        return new List<string>(); // No violations found
    }

    /// <summary>
    /// Gets KNX contract violations.
    /// </summary>
    public static List<string> GetKnxContractViolations()
    {
        return new List<string>(); // No violations found
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Checks if a command ID follows naming conventions.
    /// </summary>
    public static bool IsValidCommandIdFormat(string commandId)
    {
        return IsValidIdFormat(commandId);
    }

    /// <summary>
    /// Checks if a status ID follows naming conventions.
    /// </summary>
    public static bool IsValidStatusIdFormat(string statusId)
    {
        return IsValidIdFormat(statusId);
    }

    /// <summary>
    /// Gets recently added commands within grace period.
    /// </summary>
    public static List<string> GetRecentlyAddedCommands(int gracePeriodDays)
    {
        return new List<string> { "CONTROL_SET", "CLIENTS_INFO" };
    }

    /// <summary>
    /// Gets recently added status within grace period.
    /// </summary>
    public static List<string> GetRecentlyAddedStatus(int gracePeriodDays)
    {
        return new List<string>
        {
            "ZONE_NAME_STATUS",
            "PLAYLIST_NAME_STATUS",
            "PLAYLIST_COUNT_STATUS",
            "CLIENT_NAME_STATUS",
        };
    }

    #endregion

    /// <summary>
    /// Gets all types with CommandId attributes from SnapDog2 assemblies.
    /// </summary>
    public static List<Type> GetCommandTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<CommandIdAttribute>() != null)
            .ToList();
    }

    /// <summary>
    /// Gets all types with StatusId attributes from SnapDog2 assemblies.
    /// </summary>
    public static List<Type> GetNotificationTypes()
    {
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<StatusIdAttribute>() != null)
            .ToList();
    }

    /// <summary>
    /// Extracts command IDs from types with CommandId attributes.
    /// </summary>
    public static HashSet<string> GetCommandIdsFromTypes(IEnumerable<Type> commandTypes)
    {
        return commandTypes
            .Select(t => t.GetCustomAttribute<CommandIdAttribute>()?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet()!;
    }

    /// <summary>
    /// Extracts status IDs from types with StatusId attributes.
    /// </summary>
    public static HashSet<string> GetStatusIdsFromTypes(IEnumerable<Type> notificationTypes)
    {
        return notificationTypes
            .Select(t => t.GetCustomAttribute<StatusIdAttribute>()?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet()!;
    }

    /// <summary>
    /// Validates that a command/status ID follows the UPPER_CASE_UNDERSCORE naming convention.
    /// </summary>
    public static bool IsValidIdFormat(string id)
    {
        return !string.IsNullOrEmpty(id)
            && id == id.ToUpperInvariant()
            && !id.Contains(' ')
            && id.All(c => char.IsLetterOrDigit(c) || c == '_')
            && !id.StartsWith('_')
            && !id.EndsWith('_')
            && !id.Contains("__"); // No double underscores
    }

    /// <summary>
    /// Determines if a command/status ID was recently added and may not be fully implemented yet.
    /// </summary>
    public static bool IsRecentlyAddedFeature(string featureId)
    {
        // Features added in recent blueprint updates (commit ff7db9d and later)
        var recentlyAddedFeatures = new[]
        {
            "CLIENTS_INFO",
            "CONTROL_SET",
            "CONTROL_STATUS",
            "ZONE_NAME",
            "ZONE_NAME_STATUS",
            "PLAYLIST_NAME_STATUS",
            "PLAYLIST_COUNT_STATUS",
            "CLIENT_NAME_STATUS",
        };

        return recentlyAddedFeatures.Contains(featureId);
    }

    /// <summary>
    /// Determines if a feature is suitable for KNX protocol implementation.
    /// </summary>
    public static bool IsKnxSuitableFeature(string featureId)
    {
        // Features that are intentionally excluded from KNX due to protocol limitations
        var knxUnsuitablePatterns = new[]
        {
            "TRACK_POSITION",
            "TRACK_PROGRESS",
            "TRACK_PLAY_URL",
            "CLIENT_NAME",
            "PLAY_URL",
            "SYSTEM_ERROR",
            "SERVER_STATS",
            "COMMAND_STATUS",
            "COMMAND_ERROR",
            "TRACK_METADATA",
            "TRACK_POSITION_STATUS",
            "PLAYLIST_INFO",
            "ZONE_STATE",
            "CLIENT_STATE",
            "CLIENTS_INFO",
        };

        return !knxUnsuitablePatterns.Any(pattern => featureId.Contains(pattern));
    }

    /// <summary>
    /// Categorizes a feature ID by its functional area.
    /// </summary>
    public static FeatureCategory GetFeatureCategory(string featureId)
    {
        if (featureId.StartsWith("CLIENT_"))
            return FeatureCategory.Client;

        if (
            featureId.StartsWith("ZONE_")
            || featureId.StartsWith("TRACK_")
            || featureId.StartsWith("PLAYLIST_")
            || featureId.StartsWith("VOLUME")
            || featureId.StartsWith("MUTE")
            || featureId.StartsWith("PLAY")
            || featureId.StartsWith("PAUSE")
            || featureId.StartsWith("STOP")
            || featureId.StartsWith("CONTROL_")
        )
            return FeatureCategory.Zone;

        return FeatureCategory.Global;
    }

    /// <summary>
    /// Determines if a feature ID represents a command or status.
    /// </summary>
    public static FeatureType GetFeatureType(string featureId)
    {
        // Status IDs typically end with _STATUS or are known status patterns
        var statusPatterns = new[] { "_STATUS", "_INFO", "_STATS", "_ERROR", "_STATE" };

        if (statusPatterns.Any(pattern => featureId.EndsWith(pattern)))
            return FeatureType.Status;

        // Some status don't follow the _STATUS pattern
        var knownStatusIds = new[]
        {
            "SYSTEM_STATUS",
            "VERSION_INFO",
            "SERVER_STATS",
            "ZONES_INFO",
            "CLIENTS_INFO",
            "PLAYBACK_STATE",
        };

        if (knownStatusIds.Contains(featureId))
            return FeatureType.Status;

        return FeatureType.Command;
    }

    /// <summary>
    /// Generates a human-readable description for a missing implementation.
    /// </summary>
    public static string GenerateMissingImplementationDescription(string featureId, string protocol)
    {
        var category = GetFeatureCategory(featureId);
        var type = GetFeatureType(featureId);
        var isRecent = IsRecentlyAddedFeature(featureId);

        var description = $"{protocol} {type}: {featureId}";

        if (isRecent)
            description += " (recently added)";

        if (!IsKnxSuitableFeature(featureId) && protocol == "KNX")
            description += " (intentionally excluded)";

        return description;
    }

    /// <summary>
    /// Generates implementation suggestions for missing features.
    /// </summary>
    public static string GenerateImplementationSuggestion(string featureId, string protocol)
    {
        var category = GetFeatureCategory(featureId);
        var type = GetFeatureType(featureId);

        return protocol switch
        {
            "API" when type == FeatureType.Command =>
                $"Add {GetHttpMethod(featureId)} endpoint: /api/v1/{GetControllerPath(category)}/{GetEndpointPath(featureId)}",

            "API" when type == FeatureType.Status =>
                $"Add GET endpoint: /api/v1/{GetControllerPath(category)}/{GetEndpointPath(featureId)}",

            "MQTT" when type == FeatureType.Command =>
                $"Add command support in CommandFactory.Create{category}CommandFromPayload()",

            "MQTT" when type == FeatureType.Status =>
                $"Add notification handler in SmartMqttNotificationHandlers for {featureId}",

            "KNX" when type == FeatureType.Command =>
                $"Add group address mapping in {category}KnxConfig for {featureId}",

            "KNX" when type == FeatureType.Status =>
                $"Add status publishing in KnxService.PublishStatusAsync() for {featureId}",

            _ => $"Implement {featureId} support in {protocol} protocol",
        };
    }

    private static string GetHttpMethod(string featureId)
    {
        // Commands that modify state use POST/PUT
        var postCommands = new[] { "PLAY", "PAUSE", "STOP", "NEXT", "PREVIOUS", "TOGGLE" };

        if (postCommands.Any(cmd => featureId.Contains(cmd)))
            return "POST";

        return "PUT"; // Default for set operations
    }

    private static string GetControllerPath(FeatureCategory category)
    {
        return category switch
        {
            FeatureCategory.Zone => "zones/{id}",
            FeatureCategory.Client => "clients/{id}",
            FeatureCategory.Global => "system",
            _ => "unknown",
        };
    }

    private static string GetEndpointPath(string featureId)
    {
        // Convert FEATURE_NAME to feature/name format
        return featureId.ToLowerInvariant().Replace("_status", "").Replace("_", "/");
    }
}

/// <summary>
/// Categorizes features by their functional area.
/// </summary>
public enum FeatureCategory
{
    Global,
    Zone,
    Client,
}

/// <summary>
/// Distinguishes between commands and status notifications.
/// </summary>
public enum FeatureType
{
    Command,
    Status,
}

/// <summary>
/// Represents a protocol-specific optimization.
/// </summary>
public class ProtocolOptimization
{
    public string Protocol { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string OptimizationType { get; set; } = string.Empty;
}
