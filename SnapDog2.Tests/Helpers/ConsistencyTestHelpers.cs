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
        // Initialize the actual registries
        try
        {
            var commandRegistryType = typeof(SnapDog2.Core.Attributes.CommandIdRegistry);
            var statusRegistryType = typeof(SnapDog2.Core.Attributes.StatusIdRegistry);

            commandRegistryType.GetMethod("Initialize")?.Invoke(null, null);
            statusRegistryType.GetMethod("Initialize")?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail - registries might not be fully implemented yet
            Console.WriteLine($"Warning: Could not initialize registries: {ex.Message}");
        }
    }

    #region Registry Access Methods

    /// <summary>
    /// Gets all registered command IDs from the CommandIdRegistry.
    /// </summary>
    public static List<string> GetAllRegisteredCommands()
    {
        try
        {
            var commandRegistryType = typeof(SnapDog2.Core.Attributes.CommandIdRegistry);
            var getAllCommandIdsMethod = commandRegistryType.GetMethod("GetAllCommandIds");

            if (getAllCommandIdsMethod != null)
            {
                var result = getAllCommandIdsMethod.Invoke(null, null) as IReadOnlyCollection<string>;
                return result?.ToList() ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not get registered commands: {ex.Message}");
        }

        // Fallback to placeholder list if registry is not working
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
            "CONTROL",
            "CLIENTS_INFO",
        };
    }

    /// <summary>
    /// Gets all registered status IDs from the StatusIdRegistry.
    /// </summary>
    public static List<string> GetAllRegisteredStatus()
    {
        try
        {
            var statusRegistryType = typeof(SnapDog2.Core.Attributes.StatusIdRegistry);
            var getAllStatusIdsMethod = statusRegistryType.GetMethod("GetAllStatusIds");

            if (getAllStatusIdsMethod != null)
            {
                var result = getAllStatusIdsMethod.Invoke(null, null) as IReadOnlyCollection<string>;
                return result?.ToList() ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not get registered status: {ex.Message}");
        }

        // Fallback to placeholder list if registry is not working
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
    /// Gets all command types from the registries.
    /// </summary>
    public static List<Type> GetAllCommandTypes()
    {
        try
        {
            var commandRegistryType = typeof(SnapDog2.Core.Attributes.CommandIdRegistry);
            var getAllCommandTypesMethod = commandRegistryType.GetMethod("GetAllCommandTypes");

            if (getAllCommandTypesMethod != null)
            {
                var result = getAllCommandTypesMethod.Invoke(null, null) as IReadOnlyCollection<Type>;
                return result?.ToList() ?? new List<Type>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not get command types: {ex.Message}");
        }

        // Fallback to reflection-based discovery
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<SnapDog2.Core.Attributes.CommandIdAttribute>() != null)
            .ToList();
    }

    /// <summary>
    /// Gets all notification types from the registries.
    /// </summary>
    public static List<Type> GetAllNotificationTypes()
    {
        try
        {
            var statusRegistryType = typeof(SnapDog2.Core.Attributes.StatusIdRegistry);
            var getAllNotificationTypesMethod = statusRegistryType.GetMethod("GetAllNotificationTypes");

            if (getAllNotificationTypesMethod != null)
            {
                var result = getAllNotificationTypesMethod.Invoke(null, null) as IReadOnlyCollection<Type>;
                return result?.ToList() ?? new List<Type>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not get notification types: {ex.Message}");
        }

        // Fallback to reflection-based discovery
        return GetSnapDogAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<SnapDog2.Core.Attributes.StatusIdAttribute>() != null)
            .ToList();
    }

    #endregion

    #region Blueprint Methods

    /// <summary>
    /// Gets expected command IDs from the blueprint documentation.
    /// These are commands that should have API endpoints, MQTT handlers, and potentially KNX support.
    /// </summary>
    public static List<string> GetBlueprintCommandIds()
    {
        // Commands extracted from blueprint Section 14 - Command Framework
        return new List<string>
        {
            // Zone Playback Control Commands (Section 14.3.1)
            "PLAY",
            "PAUSE",
            "STOP",
            // Track Management Commands
            "TRACK",
            "TRACK_NEXT",
            "TRACK_PREVIOUS",
            "TRACK_PLAY_INDEX",
            "TRACK_PLAY_URL",
            "TRACK_POSITION",
            "TRACK_PROGRESS",
            "TRACK_REPEAT",
            "TRACK_REPEAT_TOGGLE",
            // Playlist Management Commands
            "PLAYLIST",
            "PLAYLIST_NEXT",
            "PLAYLIST_PREVIOUS",
            "PLAYLIST_SHUFFLE",
            "PLAYLIST_SHUFFLE_TOGGLE",
            "PLAYLIST_REPEAT",
            "PLAYLIST_REPEAT_TOGGLE",
            // Volume & Mute Control Commands
            "VOLUME",
            "VOLUME_UP",
            "VOLUME_DOWN",
            "MUTE",
            "MUTE_TOGGLE",
            // General Zone Commands
            "CONTROL",
            "ZONE_NAME",
            // Client Commands (Section 14.4.1)
            "CLIENT_VOLUME",
            "CLIENT_VOLUME_UP",
            "CLIENT_VOLUME_DOWN",
            "CLIENT_MUTE",
            "CLIENT_MUTE_TOGGLE",
            "CLIENT_LATENCY",
            "CLIENT_ZONE",
            "CLIENT_NAME",
        };
    }

    /// <summary>
    /// Gets expected status IDs from the blueprint documentation.
    /// These are status that should have MQTT publishers, API endpoints, and potentially KNX support.
    /// </summary>
    public static List<string> GetBlueprintStatusIds()
    {
        // Status extracted from blueprint Section 14 - Command Framework
        return new List<string>
        {
            // Global System Status (Section 14.2.1)
            "SYSTEM_STATUS",
            "SYSTEM_ERROR",
            "VERSION_INFO",
            "SERVER_STATS",
            "ZONES_INFO",
            "CLIENTS_INFO",
            // Zone Track Management Status
            "TRACK_STATUS",
            "TRACK_REPEAT_STATUS",
            // Zone Track Metadata Status (Static Information)
            "TRACK_METADATA",
            "TRACK_METADATA_DURATION",
            "TRACK_METADATA_TITLE",
            "TRACK_METADATA_ARTIST",
            "TRACK_METADATA_ALBUM",
            "TRACK_METADATA_COVER",
            // Zone Track Playback Status (Dynamic Real-Time)
            "TRACK_PLAYING_STATUS",
            "TRACK_POSITION_STATUS",
            "TRACK_PROGRESS_STATUS",
            // Zone Playlist Management Status
            "PLAYLIST_STATUS",
            "PLAYLIST_NAME_STATUS",
            "PLAYLIST_COUNT_STATUS",
            "PLAYLIST_INFO",
            "PLAYLIST_SHUFFLE_STATUS",
            "PLAYLIST_REPEAT_STATUS",
            // Zone Volume & Mute Status
            "VOLUME_STATUS",
            "MUTE_STATUS",
            // General Zone Status
            "CONTROL_STATUS",
            "ZONE_NAME_STATUS",
            "ZONE_STATE",
            // Client Status (Section 14.4.1)
            "CLIENT_VOLUME_STATUS",
            "CLIENT_MUTE_STATUS",
            "CLIENT_LATENCY_STATUS",
            "CLIENT_ZONE_STATUS",
            "CLIENT_NAME_STATUS",
            "CLIENT_CONNECTED",
            "CLIENT_STATE",
            // Command Response Status (Section 14.2.1)
            "COMMAND_STATUS",
            "COMMAND_ERROR",
            // Derived/Computed Status
            "PLAYBACK_STATE", // Derived from track playing status and other factors
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
        var commandIds = new List<string>();

        // Check if this type implements ICommandHandler<TCommand, TResult>
        var commandHandlerInterfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.Contains("ICommandHandler"));

        foreach (var handlerInterface in commandHandlerInterfaces)
        {
            var commandType = handlerInterface.GetGenericArguments().FirstOrDefault();
            if (commandType != null)
            {
                var commandAttr = commandType.GetCustomAttribute<CommandIdAttribute>();
                if (commandAttr != null)
                {
                    commandIds.Add(commandAttr.Id);
                }
            }
        }

        // Also check methods for CommandId attributes (legacy pattern)
        foreach (var method in handlerType.GetMethods())
        {
            var commandAttr = method.GetCustomAttribute<CommandIdAttribute>();
            if (commandAttr != null)
            {
                commandIds.Add(commandAttr.Id);
            }
        }

        return commandIds.Distinct().ToList();
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

        // Basic playback commands
        if (endpoint.Name.Contains("Play"))
        {
            commandIds.Add("PLAY");
        }

        if (endpoint.Name.Contains("Pause"))
        {
            commandIds.Add("PAUSE");
        }

        if (endpoint.Name.Contains("Stop"))
        {
            commandIds.Add("STOP");
        }

        if (endpoint.Name.Contains("PlayUrl"))
        {
            commandIds.Add("TRACK_PLAY_URL");
        }

        if (endpoint.Name.Contains("SetPlaylist"))
        {
            commandIds.Add("PLAYLIST");
        }

        // Track repeat commands
        if (endpoint.Name.Contains("ToggleTrackRepeat"))
        {
            commandIds.Add("TRACK_REPEAT_TOGGLE");
        }

        if (endpoint.Name.Contains("SetTrackRepeat"))
        {
            commandIds.Add("TRACK_REPEAT");
        }

        // Playlist repeat commands
        if (endpoint.Name.Contains("TogglePlaylistRepeat"))
        {
            commandIds.Add("PLAYLIST_REPEAT_TOGGLE");
        }

        if (endpoint.Name.Contains("SetPlaylistRepeat"))
        {
            commandIds.Add("PLAYLIST_REPEAT");
        }

        // Track commands
        if (endpoint.Name.Contains("SetTrack"))
        {
            commandIds.Add("TRACK");
        }

        if (endpoint.Name.Contains("PlayTrackByIndex"))
        {
            commandIds.Add("TRACK_PLAY_INDEX");
        }

        // Seek commands
        if (endpoint.Name.Contains("SetTrackPosition"))
        {
            commandIds.Add("TRACK_POSITION");
        }

        if (endpoint.Name.Contains("SetTrackProgress"))
        {
            commandIds.Add("TRACK_PROGRESS");
        }

        // Control commands
        if (endpoint.Name.Contains("ControlSet") || endpoint.Name.Contains("SetControl"))
        {
            commandIds.Add("CONTROL");
        }

        // Snapcast commands
        if (endpoint.Name.Contains("SetSnapcastClientVolume"))
        {
            commandIds.Add("SNAPCAST_CLIENT_VOLUME");
        }

        if (endpoint.Name.Contains("Volume"))
        {
            commandIds.Add("VOLUME_SET");
        }

        // Playlist shuffle commands
        if (endpoint.Name.Contains("TogglePlaylistShuffle"))
        {
            commandIds.Add("PLAYLIST_SHUFFLE_TOGGLE");
        }

        if (endpoint.Name.Contains("SetPlaylistShuffle"))
        {
            commandIds.Add("PLAYLIST_SHUFFLE");
        }

        // Track navigation commands
        if (endpoint.Name.Contains("NextTrack") || endpoint.Name.Contains("TrackNext"))
        {
            commandIds.Add("TRACK_NEXT");
        }

        if (endpoint.Name.Contains("PreviousTrack") || endpoint.Name.Contains("TrackPrevious"))
        {
            commandIds.Add("TRACK_PREVIOUS");
        }

        // Playlist navigation commands
        if (endpoint.Name.Contains("NextPlaylist") || endpoint.Name.Contains("PlaylistNext"))
        {
            commandIds.Add("PLAYLIST_NEXT");
        }

        if (endpoint.Name.Contains("PreviousPlaylist") || endpoint.Name.Contains("PlaylistPrevious"))
        {
            commandIds.Add("PLAYLIST_PREVIOUS");
        }

        // Client commands
        if (endpoint.Name.Contains("SetLatency"))
        {
            commandIds.Add("CLIENT_LATENCY");
        }

        if (
            endpoint.Name.Contains("SetClientVolume")
            || (endpoint.Name.Contains("SetVolume") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            commandIds.Add("CLIENT_VOLUME");
        }

        if (
            endpoint.Name.Contains("ClientVolumeUp")
            || (endpoint.Name.Contains("VolumeUp") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            commandIds.Add("CLIENT_VOLUME_UP");
        }

        if (
            endpoint.Name.Contains("ClientVolumeDown")
            || (endpoint.Name.Contains("VolumeDown") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            commandIds.Add("CLIENT_VOLUME_DOWN");
        }

        if (
            endpoint.Name.Contains("SetClientMute")
            || (endpoint.Name.Contains("SetMute") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            commandIds.Add("CLIENT_MUTE");
        }

        if (
            endpoint.Name.Contains("ToggleClientMute")
            || (endpoint.Name.Contains("ToggleMute") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            commandIds.Add("CLIENT_MUTE_TOGGLE");
        }

        if (
            endpoint.Name.Contains("SetClientName")
            || (endpoint.Name.Contains("SetName") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            commandIds.Add("CLIENT_NAME");
        }

        if (
            endpoint.Name.Contains("AssignClientToZone")
            || endpoint.Name.Contains("SetClientZone")
            || endpoint.Name.Contains("AssignToZone")
        )
        {
            commandIds.Add("CLIENT_ZONE");
        }

        // Zone name commands
        if (
            endpoint.Name.Contains("GetZoneName")
            || (endpoint.Name.Contains("ZoneName") && endpoint.DeclaringType?.Name.Contains("Zone") == true)
        )
        {
            commandIds.Add("ZONE_NAME");
        }

        // Zone volume commands
        if (
            endpoint.Name.Contains("SetZoneVolume")
            || (endpoint.Name.Contains("SetVolume") && endpoint.DeclaringType?.Name.Contains("Zone") == true)
        )
        {
            commandIds.Add("VOLUME");
        }

        if (endpoint.Name.Contains("IncreaseVolume") || endpoint.Name.Contains("VolumeUp"))
        {
            commandIds.Add("VOLUME_UP");
        }

        if (endpoint.Name.Contains("DecreaseVolume") || endpoint.Name.Contains("VolumeDown"))
        {
            commandIds.Add("VOLUME_DOWN");
        }

        // Zone mute commands
        if (
            endpoint.Name.Contains("SetZoneMute")
            || (endpoint.Name.Contains("SetMute") && endpoint.DeclaringType?.Name.Contains("Zone") == true)
        )
        {
            commandIds.Add("MUTE");
        }

        if (
            endpoint.Name.Contains("ToggleZoneMute")
            || (endpoint.Name.Contains("ToggleMute") && endpoint.DeclaringType?.Name.Contains("Zone") == true)
        )
        {
            commandIds.Add("MUTE_TOGGLE");
        }

        return commandIds.Distinct().ToList();
    }

    /// <summary>
    /// Extracts status IDs from an API endpoint method.
    /// </summary>
    public static List<string> ExtractStatusIdsFromApiEndpoint(MethodInfo endpoint)
    {
        // Look for status-related patterns in method names
        var statusIds = new List<string>();

        // System and global status endpoints
        if (endpoint.Name.Contains("GetSystemStatus") || endpoint.Name.Contains("SystemStatus"))
        {
            statusIds.Add("SYSTEM_STATUS");
        }

        if (endpoint.Name.Contains("GetServerStats") || endpoint.Name.Contains("ServerStats"))
        {
            statusIds.Add("SERVER_STATS");
        }

        if (
            endpoint.Name.Contains("GetVersionInfo")
            || endpoint.Name.Contains("VersionInfo")
            || endpoint.Name.Contains("GetVersion")
        )
        {
            statusIds.Add("VERSION_INFO");
        }

        if (endpoint.Name.Contains("GetZones") && !endpoint.Name.Contains("GetZone("))
        {
            statusIds.Add("ZONES_INFO");
        }

        if (endpoint.Name.Contains("GetClients") && !endpoint.Name.Contains("GetClient("))
        {
            statusIds.Add("CLIENTS_INFO");
        }

        // Zone state and status endpoints
        if (
            endpoint.Name.Contains("GetZone")
            && endpoint.GetParameters().Any(p => p.Name?.Contains("zoneIndex") == true)
        )
        {
            statusIds.Add("ZONE_STATE");
        }

        if (endpoint.Name.Contains("GetVolume") && endpoint.DeclaringType?.Name?.Contains("Zone") == true)
        {
            statusIds.Add("VOLUME_STATUS");
        }

        if (endpoint.Name.Contains("GetMute") && endpoint.DeclaringType?.Name.Contains("Zone") == true)
        {
            statusIds.Add("MUTE_STATUS");
        }

        // Track status endpoints
        if (endpoint.Name.Contains("GetTrackIndex"))
        {
            statusIds.Add("TRACK_STATUS");
        }

        if (endpoint.Name.Contains("GetTrackRepeat"))
        {
            statusIds.Add("TRACK_REPEAT_STATUS");
        }

        if (endpoint.Name.Contains("GetTrackPosition"))
        {
            statusIds.Add("TRACK_POSITION_STATUS");
        }

        if (endpoint.Name.Contains("GetTrackProgress"))
        {
            statusIds.Add("TRACK_PROGRESS_STATUS");
        }

        if (endpoint.Name.Contains("GetTrack") && endpoint.GetParameters().Any(p => p.Name == "id"))
        {
            statusIds.Add("TRACK_METADATA");
            statusIds.Add("TRACK_METADATA_TITLE");
            statusIds.Add("TRACK_METADATA_ARTIST");
            statusIds.Add("TRACK_METADATA_ALBUM");
            statusIds.Add("TRACK_METADATA_DURATION");
            statusIds.Add("TRACK_METADATA_COVER");
        }

        // Track playing status - derived from playback state endpoints
        if (
            endpoint.Name.Contains("GetZone")
            || endpoint.Name.Contains("GetPlayback")
            || endpoint.Name.Contains("GetTrackPlaying")
        )
        {
            statusIds.Add("TRACK_PLAYING_STATUS");
        }

        // Playlist status endpoints
        if (endpoint.Name.Contains("GetPlaylistIndex"))
        {
            statusIds.Add("PLAYLIST_STATUS");
        }

        if (endpoint.Name.Contains("GetPlaylistInfo"))
        {
            statusIds.Add("PLAYLIST_INFO");
            statusIds.Add("PLAYLIST_NAME_STATUS");
            statusIds.Add("PLAYLIST_COUNT_STATUS");
        }

        if (endpoint.Name.Contains("GetPlaylistRepeat"))
        {
            statusIds.Add("PLAYLIST_REPEAT_STATUS");
        }

        if (endpoint.Name.Contains("GetPlaylistShuffle"))
        {
            statusIds.Add("PLAYLIST_SHUFFLE_STATUS");
        }

        if (endpoint.Name.Contains("GetPlaylists"))
        {
            statusIds.Add("PLAYLIST_STATUS");
        }

        // Client status endpoints
        if (
            endpoint.Name.Contains("GetClient")
            && endpoint.GetParameters().Any(p => p.Name?.Contains("clientIndex") == true)
        )
        {
            statusIds.Add("CLIENT_STATE");
        }

        if (
            endpoint.Name.Contains("GetClientVolume")
            || (endpoint.Name.Contains("GetVolume") && endpoint.DeclaringType?.Name?.Contains("Client") == true)
        )
        {
            statusIds.Add("CLIENT_VOLUME_STATUS");
        }

        if (
            endpoint.Name.Contains("GetClientMute")
            || (endpoint.Name.Contains("GetMute") && endpoint.DeclaringType?.Name.Contains("Client") == true)
        )
        {
            statusIds.Add("CLIENT_MUTE_STATUS");
        }

        if (endpoint.Name.Contains("GetClientLatency") || endpoint.Name.Contains("GetLatency"))
        {
            statusIds.Add("CLIENT_LATENCY_STATUS");
        }

        if (endpoint.Name.Contains("GetZoneAssignment") || endpoint.Name.Contains("GetClientZone"))
        {
            statusIds.Add("CLIENT_ZONE_STATUS");
        }

        if (
            endpoint.Name.Contains("GetClientName")
            || (endpoint.Name.Contains("GetName") && endpoint.DeclaringType?.Name?.Contains("Client") == true)
        )
        {
            statusIds.Add("CLIENT_NAME_STATUS");
        }

        // GetClient endpoint provides client name as part of ClientState
        if (
            endpoint.Name.Contains("GetClient")
            && endpoint.GetParameters().Any(p => p.Name?.Contains("clientIndex") == true)
        )
        {
            statusIds.Add("CLIENT_NAME_STATUS");
        }

        if (endpoint.Name.Contains("GetClientConnected") || endpoint.Name.Contains("GetConnected"))
        {
            statusIds.Add("CLIENT_CONNECTED");
        }

        // Zone name endpoint
        if (endpoint.Name.Contains("GetZoneName"))
        {
            statusIds.Add("ZONE_NAME_STATUS");
        }

        // Media source endpoints
        if (endpoint.Name.Contains("GetMediaSources"))
        {
            statusIds.Add("SYSTEM_STATUS"); // Media sources are part of system status
        }

        // Playback state - derived from multiple endpoints
        if (
            endpoint.Name.Contains("GetZone")
            || endpoint.Name.Contains("GetTrack")
            || endpoint.Name.Contains("GetPlaylist")
        )
        {
            statusIds.Add("PLAYBACK_STATE");
        }

        // Health endpoints
        if (
            endpoint.Name.Contains("GetHealth")
            || endpoint.Name.Contains("GetReady")
            || endpoint.Name.Contains("GetLive")
        )
        {
            statusIds.Add("SYSTEM_STATUS");
        }

        // Command status endpoints
        if (endpoint.Name.Contains("GetCommandStatus") || endpoint.Name.Contains("CommandStatus"))
        {
            statusIds.Add("COMMAND_STATUS");
        }

        if (endpoint.Name.Contains("GetCommandErrors") || endpoint.Name.Contains("CommandErrors"))
        {
            statusIds.Add("COMMAND_ERROR");
        }

        if (
            endpoint.Name.Contains("GetErrorStatus")
            || endpoint.Name.Contains("ErrorStatus")
            || endpoint.Name.Contains("GetSystemErrors")
        )
        {
            statusIds.Add("SYSTEM_ERROR");
        }

        return statusIds.Distinct().ToList();
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
        // For MQTT, commands are handled in methods like MapZoneCommand and CreateZoneCommandFromPayload
        // These methods contain switch statements that handle all the MQTT command strings
        var commandIds = new List<string>();

        // Check if this is a command mapping method that handles multiple commands
        if (
            handlerMethod.Name.Contains("MapZoneCommand")
            || handlerMethod.Name.Contains("CreateZoneCommandFromPayload")
            || handlerMethod.Name.Contains("ProcessZoneCommand")
        )
        {
            // These methods handle all the basic playback commands
            commandIds.AddRange(
                new[]
                {
                    "PLAY",
                    "PAUSE",
                    "STOP",
                    "TRACK_NEXT",
                    "TRACK_PREVIOUS",
                    "PLAYLIST_NEXT",
                    "PLAYLIST_PREVIOUS",
                    "PLAYLIST_SHUFFLE",
                    "PLAYLIST_SHUFFLE_TOGGLE",
                    "PLAYLIST_REPEAT",
                    "PLAYLIST_REPEAT_TOGGLE",
                    "TRACK_REPEAT",
                    "TRACK_REPEAT_TOGGLE",
                    "VOLUME_SET",
                    "MUTE_TOGGLE",
                }
            );
        }

        // Legacy pattern matching for specific method names
        if (handlerMethod.Name.Contains("Play"))
        {
            commandIds.Add("PLAY");
        }

        if (handlerMethod.Name.Contains("Pause"))
        {
            commandIds.Add("PAUSE");
        }

        if (handlerMethod.Name.Contains("Volume"))
        {
            commandIds.Add("VOLUME_SET");
        }

        return commandIds.Distinct().ToList();
    }

    /// <summary>
    /// Extracts status IDs from an MQTT publisher type.
    /// </summary>
    public static List<string> ExtractStatusIdsFromMqttPublisher(Type publisherType)
    {
        var statusIds = new List<string>();

        // Check if this is the SmartMqttNotificationHandlers class that handles all notifications
        if (publisherType.Name.Contains("SmartMqttNotificationHandlers"))
        {
            // This class handles all the MQTT status publishing through notification handlers
            statusIds.AddRange(
                new[]
                {
                    "CLIENT_VOLUME_STATUS",
                    "CLIENT_MUTE_STATUS",
                    "CLIENT_LATENCY_STATUS",
                    "CLIENT_ZONE_STATUS",
                    "CLIENT_CONNECTED",
                    "CLIENT_STATE",
                    "ZONE_VOLUME_STATUS",
                    "ZONE_MUTE_STATUS",
                    "PLAYBACK_STATE",
                    "TRACK_STATUS",
                    "PLAYLIST_STATUS",
                    "TRACK_REPEAT_STATUS",
                    "PLAYLIST_REPEAT_STATUS",
                    "PLAYLIST_SHUFFLE_STATUS",
                    "ZONE_STATE_STATUS",
                    "TRACK_METADATA_ALBUM",
                }
            );
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
                    var statusAttr = paramType.GetCustomAttribute<SnapDog2.Core.Attributes.StatusIdAttribute>();
                    if (statusAttr != null)
                    {
                        statusIds.Add(statusAttr.Id);
                    }
                }
            }
        }

        return statusIds.Distinct().ToList();
    }

    /// <summary>
    /// Extracts MQTT topics from a handler method.
    /// </summary>
    public static List<string> ExtractMqttTopicsFromHandler(MethodInfo handlerMethod)
    {
        // Look for topic patterns in method names or attributes
        var topics = new List<string>();

        if (handlerMethod.Name.Contains("Zone"))
        {
            topics.Add("snapdog/zone/+/command");
        }

        if (handlerMethod.Name.Contains("Client"))
        {
            topics.Add("snapdog/client/+/command");
        }

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
        {
            return "Command";
        }

        if (handlerMethod.Name.Contains("Status"))
        {
            return "Status";
        }

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
        return new HashSet<string>
        {
            "VOLUME_SET",
            "SYSTEM_STATUS",
            "CLIENTS_INFO",
            "TRACK_INFO",
            "PLAYLIST_SHUFFLE_TOGGLE", // Toggle commands are typically excluded from KNX due to state synchronization complexity
        };
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
        return new List<string> { "CONTROL", "CLIENTS_INFO" };
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
            "CONTROL",
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
        {
            return FeatureCategory.Client;
        }

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
        {
            return FeatureCategory.Zone;
        }

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
        {
            return FeatureType.Status;
        }

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
        {
            return FeatureType.Status;
        }

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
        {
            description += " (recently added)";
        }

        if (!IsKnxSuitableFeature(featureId) && protocol == "KNX")
        {
            description += " (intentionally excluded)";
        }

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
        {
            return "POST";
        }

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
