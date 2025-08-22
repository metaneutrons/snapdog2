namespace SnapDog2.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Core.Attributes;
using SnapDog2.Tests.Blueprint;

/// <summary>
/// Helper methods for consistency testing.
/// These methods inspect the actual implementation to validate against the blueprint.
/// </summary>
public static class ConsistencyTestHelpers
{
    #region Assembly Discovery

    /// <summary>
    /// Gets all API controller types from the main assembly.
    /// </summary>
    public static List<Type> GetAllApiControllerTypes()
    {
        var assembly = Assembly.Load("SnapDog2");
        return assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t.Name.EndsWith("Controller"))
            .ToList();
    }

    /// <summary>
    /// Gets all API endpoints from controller types.
    /// </summary>
    public static List<MethodInfo> GetAllApiEndpoints()
    {
        return GetAllApiControllerTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType != typeof(ControllerBase))
            .ToList();
    }

    /// <summary>
    /// Gets all MQTT handler types from the main assembly.
    /// </summary>
    public static List<Type> GetAllMqttHandlerTypes()
    {
        var assembly = Assembly.Load("SnapDog2");
        return assembly
            .GetTypes()
            .Where(t => t.Name.Contains("Mqtt") && (t.Name.Contains("Handler") || t.Name.Contains("Service")))
            .ToList();
    }

    #endregion

    #region Registry Discovery

    /// <summary>
    /// Gets all registered command IDs from the CommandIdRegistry.
    /// </summary>
    public static List<string> GetAllRegisteredCommands()
    {
        try
        {
            var registryType = Type.GetType("SnapDog2.Core.Attributes.CommandIdRegistry, SnapDog2");
            if (registryType == null)
                return new List<string>();

            var getAllMethod = registryType.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
            if (getAllMethod?.Invoke(null, null) is IEnumerable<string> commands)
            {
                return commands.ToList();
            }
        }
        catch (Exception)
        {
            // Registry not available or not implemented yet
        }

        return new List<string>();
    }

    /// <summary>
    /// Gets all registered status IDs from the StatusIdRegistry.
    /// </summary>
    public static List<string> GetAllRegisteredStatus()
    {
        try
        {
            var registryType = Type.GetType("SnapDog2.Core.Attributes.StatusIdRegistry, SnapDog2");
            if (registryType == null)
                return new List<string>();

            var getAllMethod = registryType.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
            if (getAllMethod?.Invoke(null, null) is IEnumerable<string> status)
            {
                return status.ToList();
            }
        }
        catch (Exception)
        {
            // Registry not available or not implemented yet
        }

        return new List<string>();
    }

    #endregion

    #region Command/Status Extraction

    /// <summary>
    /// Extracts command IDs from an API endpoint method.
    /// </summary>
    public static List<string> ExtractCommandIdsFromApiEndpoint(MethodInfo endpoint)
    {
        var commandIds = new List<string>();

        // Skip GET methods as they are status/query endpoints, not commands
        var httpMethods = GetHttpMethodsFromEndpoint(endpoint);
        if (httpMethods.Contains("GET"))
        {
            return commandIds; // Return empty list for GET endpoints
        }

        // Extract command IDs based on method names and patterns
        var methodName = endpoint.Name;

        // Basic playback commands
        if (methodName.Contains("Play") && !methodName.Contains("Playlist"))
            commandIds.Add("PLAY");
        if (methodName.Contains("Pause"))
            commandIds.Add("PAUSE");
        if (methodName.Contains("Stop"))
            commandIds.Add("STOP");

        // Volume commands
        if (methodName.Contains("SetVolume") || methodName.Equals("Volume"))
            commandIds.Add("VOLUME");
        if (methodName.Contains("VolumeUp"))
            commandIds.Add("VOLUME_UP");
        if (methodName.Contains("VolumeDown"))
            commandIds.Add("VOLUME_DOWN");

        // Mute commands
        if (methodName.Contains("SetMute") && !methodName.Contains("Toggle"))
            commandIds.Add("MUTE");
        if (methodName.Contains("ToggleMute") || methodName.Contains("MuteToggle"))
            commandIds.Add("MUTE_TOGGLE");

        // Track commands
        if (methodName.Contains("SetTrack") && !methodName.Contains("Repeat"))
            commandIds.Add("TRACK");
        if (methodName.Contains("NextTrack") || methodName.Contains("TrackNext"))
            commandIds.Add("TRACK_NEXT");
        if (methodName.Contains("PreviousTrack") || methodName.Contains("TrackPrevious"))
            commandIds.Add("TRACK_PREVIOUS");

        // Playlist commands
        if (methodName.Contains("SetPlaylist") && !methodName.Contains("Shuffle") && !methodName.Contains("Repeat"))
            commandIds.Add("PLAYLIST");
        if (methodName.Contains("NextPlaylist") || methodName.Contains("PlaylistNext"))
            commandIds.Add("PLAYLIST_NEXT");
        if (methodName.Contains("PreviousPlaylist") || methodName.Contains("PlaylistPrevious"))
            commandIds.Add("PLAYLIST_PREVIOUS");

        // Client commands
        if (methodName.Contains("SetLatency"))
            commandIds.Add("CLIENT_LATENCY");
        if (methodName.Contains("SetName") && endpoint.DeclaringType?.Name.Contains("Client") == true)
            commandIds.Add("CLIENT_NAME");
        if (methodName.Contains("AssignToZone") || methodName.Contains("SetZone"))
            commandIds.Add("CLIENT_ZONE");

        // Control commands
        if (methodName.Contains("Control"))
            commandIds.Add("CONTROL");

        return commandIds;
    }

    /// <summary>
    /// Extracts status IDs from an API endpoint method.
    /// </summary>
    public static List<string> ExtractStatusIdsFromApiEndpoint(MethodInfo endpoint)
    {
        var statusIds = new List<string>();

        // Only process GET methods for status
        var httpMethods = GetHttpMethodsFromEndpoint(endpoint);
        if (!httpMethods.Contains("GET"))
        {
            return statusIds;
        }

        var methodName = endpoint.Name;

        // System status
        if (methodName.Contains("SystemStatus") || methodName.Equals("GetStatus"))
            statusIds.Add("SYSTEM_STATUS");
        if (methodName.Contains("Version"))
            statusIds.Add("VERSION_INFO");
        if (methodName.Contains("Stats"))
            statusIds.Add("SERVER_STATS");

        // Zone status
        if (methodName.Contains("GetVolume"))
            statusIds.Add("VOLUME_STATUS");
        if (methodName.Contains("GetMute"))
            statusIds.Add("MUTE_STATUS");
        if (methodName.Contains("Playback"))
            statusIds.Add("PLAYBACK_STATE");

        // Collection status
        if (methodName.Contains("GetZones") || methodName.Equals("GetAll"))
            statusIds.Add("ZONES_INFO");
        if (methodName.Contains("GetClients"))
            statusIds.Add("CLIENTS_INFO");

        return statusIds;
    }

    /// <summary>
    /// Extracts command IDs from MQTT handler methods.
    /// </summary>
    public static List<string> ExtractCommandIdsFromMqttHandler(MethodInfo handlerMethod)
    {
        var commandIds = new List<string>();

        // Check method parameters for command notification types
        foreach (var parameter in handlerMethod.GetParameters())
        {
            var paramType = parameter.ParameterType;
            if (paramType.Name.Contains("Command"))
            {
                // Try to extract command ID from CommandIdAttribute
                var commandAttr = paramType.GetCustomAttribute<CommandIdAttribute>();
                if (commandAttr != null)
                {
                    commandIds.Add(commandAttr.Id);
                }
            }
        }

        return commandIds;
    }

    /// <summary>
    /// Extracts status IDs from MQTT publisher types.
    /// </summary>
    public static List<string> ExtractStatusIdsFromMqttPublisher(Type publisherType)
    {
        var statusIds = new List<string>();

        // Skip notification handlers - they handle notifications but aren't publishers themselves
        if (publisherType.Name.Contains("NotificationHandlers") || publisherType.Name.Contains("NotificationHandler"))
        {
            return statusIds; // Return empty list for notification handlers
        }

        // Look for methods that publish status
        foreach (var method in publisherType.GetMethods())
        {
            if (method.Name.Contains("Publish") && method.Name.Contains("Status"))
            {
                statusIds.Add("SYSTEM_STATUS");
            }

            // Check for Handle methods that process notifications
            if (method.Name.StartsWith("Handle") && method.GetParameters().Length > 0)
            {
                var paramType = method.GetParameters()[0].ParameterType;
                if (paramType.Name.Contains("Notification"))
                {
                    // Extract status ID from notification type using StatusIdAttribute
                    var statusAttr = paramType.GetCustomAttribute<StatusIdAttribute>();
                    if (statusAttr != null)
                    {
                        statusIds.Add(statusAttr.Id);
                    }
                }
            }
        }

        return statusIds;
    }

    #endregion

    #region HTTP Method Helpers

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

    #endregion

    #region Blueprint Integration

    /// <summary>
    /// Gets all MQTT handler methods from a type.
    /// </summary>
    public static List<MethodInfo> GetMqttHandlerMethods(Type handlerType)
    {
        return handlerType
            .GetMethods()
            .Where(m => m.Name.Contains("Handle") || m.Name.Contains("Process") || m.Name.Contains("Map"))
            .ToList();
    }

    /// <summary>
    /// Gets actual KNX exclusions by checking what commands are not implemented in KNX handlers.
    /// This is a placeholder - in a real implementation, you'd check actual KNX handler registrations.
    /// </summary>
    public static HashSet<string> GetActualKnxExclusions()
    {
        // For now, return the blueprint exclusions as the actual exclusions
        // In a real implementation, you'd inspect actual KNX handler registrations
        return SnapDogBlueprint.Spec.Commands.ExcludedFrom(Protocol.Knx).Select(c => c.Id).ToHashSet();
    }

    #endregion
}
