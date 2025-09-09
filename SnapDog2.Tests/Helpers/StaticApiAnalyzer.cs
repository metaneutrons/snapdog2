//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Tests.Helpers;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Shared.Attributes;
using SnapDog2.Tests.Blueprint;

/// <summary>
/// Analyzes API controllers statically without requiring a running server.
/// Uses reflection to extract routes and HTTP methods from controller attributes.
/// </summary>
public static class StaticApiAnalyzer
{
    /// <summary>
    /// Gets all API endpoints by analyzing controller classes via reflection.
    /// </summary>
    public static Dictionary<string, HashSet<string>> GetImplementedApiEndpoints()
    {
        var endpoints = new Dictionary<string, HashSet<string>>();

        var assembly = Assembly.Load("SnapDog2");
        var controllerTypes = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t.Name.EndsWith("Controller"))
            .ToList();

        foreach (var controllerType in controllerTypes)
        {
            var controllerRoute = GetControllerRoute(controllerType);

            var methods = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType != typeof(ControllerBase))
                .ToList();

            foreach (var method in methods)
            {
                var (httpMethods, routePath) = GetMethodRouteInfo(method);

                if (httpMethods.Any())
                {
                    var fullPath = CombinePaths(controllerRoute, routePath);

                    if (!endpoints.ContainsKey(fullPath))
                    {
                        endpoints[fullPath] = new HashSet<string>();
                    }

                    foreach (var httpMethod in httpMethods)
                    {
                        endpoints[fullPath].Add(httpMethod);
                    }
                }
            }
        }

        return endpoints;
    }

    /// <summary>
    /// Compares blueprint commands with actual API implementation using static analysis.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareCommandImplementation()
    {
        var blueprintCommands = GetBlueprintApiCommands();
        var implementedEndpoints = GetImplementedApiEndpoints();

        var missing = new HashSet<string>();
        var blueprintEndpoints = new HashSet<string>();

        // Check for missing implementations
        foreach (var (commandId, expectedPath, expectedMethod) in blueprintCommands)
        {
            var endpointKey = $"{expectedMethod.ToUpper()} {expectedPath}";
            blueprintEndpoints.Add(endpointKey);

            if (
                !implementedEndpoints.TryGetValue(expectedPath, out var methods)
                || !methods.Contains(expectedMethod.ToUpper())
            )
            {
                missing.Add(commandId);
            }
        }

        // Check for extra implementations (orphaned) - only command methods
        var commandMethods = new HashSet<string> { "PUT", "POST", "DELETE", "PATCH" };
        var extra = new HashSet<string>();
        foreach (var (path, methods) in implementedEndpoints)
        {
            foreach (var method in methods)
            {
                // Only check command methods for orphaned commands
                if (commandMethods.Contains(method))
                {
                    var endpointKey = $"{method} {path}";
                    if (!blueprintEndpoints.Contains(endpointKey))
                    {
                        extra.Add(endpointKey);
                    }
                }
            }
        }

        return (missing, extra);
    }

    /// <summary>
    /// Compares blueprint status with actual API implementation using static analysis.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareStatusImplementation()
    {
        var blueprintStatus = GetBlueprintApiStatusWithTypes();
        var implementedEndpoints = GetImplementedApiEndpointsWithTypes();
        var implementedStatusIds = GetImplementedStatusIdAttributes();

        var missing = new HashSet<string>();
        var blueprintEndpoints = new HashSet<string>();

        // Check for missing implementations and return type mismatches
        foreach (var (statusId, expectedPath, expectedMethod, expectedReturnType) in blueprintStatus)
        {
            var endpointKey = $"{expectedMethod.ToUpper()} {expectedPath}";
            blueprintEndpoints.Add(endpointKey);

            // First check if we have a StatusId attribute for this status
            if (implementedStatusIds.Contains(statusId))
            {
                continue; // Status is implemented via StatusId attribute
            }

            // Fallback to path matching for existing endpoints
            if (!implementedEndpoints.TryGetValue(expectedPath, out var methodsWithTypes))
            {
                missing.Add(statusId);
                continue;
            }

            if (!methodsWithTypes.TryGetValue(expectedMethod.ToUpper(), out var actualReturnType))
            {
                missing.Add(statusId);
                continue;
            }

            // Check return type compatibility
            if (expectedReturnType != null && !AreTypesCompatible(expectedReturnType, actualReturnType))
            {
                missing.Add($"{statusId} (return type mismatch: expected {expectedReturnType.Name}, got {actualReturnType?.Name ?? "void"})");
            }
        }

        // Check for extra implementations (orphaned) - only GET methods for status
        var extra = new HashSet<string>();
        foreach (var (path, methodsWithTypes) in implementedEndpoints)
        {
            foreach (var (method, _) in methodsWithTypes)
            {
                // Only check GET methods for orphaned status endpoints
                if (method == "GET")
                {
                    var endpointKey = $"{method} {path}";
                    if (!blueprintEndpoints.Contains(endpointKey))
                    {
                        extra.Add(endpointKey);
                    }
                }
            }
        }

        return (missing, extra);
    }

    private static string GetControllerRoute(Type controllerType)
    {
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute != null)
        {
            var template = routeAttribute.Template;
            // Ensure leading slash for consistency with blueprint paths
            return template.StartsWith("/") ? template : "/" + template;
        }

        // Default convention: /api/v1/[controller]
        var controllerName = controllerType.Name.Replace("Controller", "").ToLower();
        return $"/api/v1/{controllerName}";
    }

    private static (List<string> httpMethods, string routePath) GetMethodRouteInfo(MethodInfo method)
    {
        var httpMethods = new List<string>();
        var routePath = "";

        // Check for HTTP method attributes
        var httpGetAttr = method.GetCustomAttribute<HttpGetAttribute>();
        if (httpGetAttr != null)
        {
            httpMethods.Add("GET");
            routePath = httpGetAttr.Template ?? "";
        }

        var httpPostAttr = method.GetCustomAttribute<HttpPostAttribute>();
        if (httpPostAttr != null)
        {
            httpMethods.Add("POST");
            routePath = httpPostAttr.Template ?? "";
        }

        var httpPutAttr = method.GetCustomAttribute<HttpPutAttribute>();
        if (httpPutAttr != null)
        {
            httpMethods.Add("PUT");
            routePath = httpPutAttr.Template ?? "";
        }

        var httpDeleteAttr = method.GetCustomAttribute<HttpDeleteAttribute>();
        if (httpDeleteAttr != null)
        {
            httpMethods.Add("DELETE");
            routePath = httpDeleteAttr.Template ?? "";
        }

        return (httpMethods, routePath);
    }

    private static string CombinePaths(string basePath, string routePath)
    {
        if (string.IsNullOrEmpty(routePath))
        {
            return basePath;
        }

        if (routePath.StartsWith("/"))
        {
            return routePath; // Absolute path
        }

        return $"{basePath.TrimEnd('/')}/{routePath}";
    }

    private static List<(string commandId, string path, string method)> GetBlueprintApiCommands()
    {
        return SnapDogBlueprint
            .Spec.Commands.Required()
            .WithApi()
            .Select(cmd => (cmd.Id, cmd.ApiPath ?? "", cmd.HttpMethod ?? ""))
            .Where(x => !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item3))
            .ToList();
    }

    public static List<(string statusId, string path, string method, Type? returnType)> GetBlueprintApiStatusWithTypes()
    {
        return SnapDogBlueprint
            .Spec.Status.Required()
            .WithApi()
            .Select(status => (status.Id, status.ApiPath ?? "", status.HttpMethod ?? "", status.ApiReturnType))
            .Where(x => !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item3))
            .ToList();
    }

    public static Dictionary<string, Dictionary<string, Type?>> GetImplementedApiEndpointsWithTypes()
    {
        var endpoints = new Dictionary<string, Dictionary<string, Type?>>();

        var assembly = Assembly.Load("SnapDog2");
        var controllerTypes = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t.Name.EndsWith("Controller"))
            .ToList();

        foreach (var controllerType in controllerTypes)
        {
            var controllerRoute = GetControllerRoute(controllerType);

            var methods = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType != typeof(ControllerBase))
                .ToList();

            foreach (var method in methods)
            {
                var (httpMethods, routePath) = GetMethodRouteInfo(method);
                var returnType = GetActionReturnType(method);

                if (httpMethods.Any() && !string.IsNullOrEmpty(routePath))
                {
                    var fullPath = CombinePaths(controllerRoute, routePath);

                    if (!endpoints.ContainsKey(fullPath))
                    {
                        endpoints[fullPath] = new Dictionary<string, Type?>();
                    }

                    foreach (var httpMethod in httpMethods)
                    {
                        endpoints[fullPath][httpMethod.ToUpper()] = returnType;
                    }
                }
            }
        }

        return endpoints;
    }

    private static Type? GetActionReturnType(MethodInfo method)
    {
        var returnType = method.ReturnType;

        // Handle Task<T> - unwrap to T
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        // Handle ActionResult<T> - unwrap to T
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            return returnType.GetGenericArguments()[0];
        }

        // Handle IActionResult - try to get from ProducesResponseType attribute
        if (returnType == typeof(IActionResult) || returnType.IsSubclassOf(typeof(ActionResult)))
        {
            var producesAttrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            return producesAttrs.FirstOrDefault(a => a.StatusCode == 200)?.Type;
        }

        return returnType == typeof(void) ? null : returnType;
    }

    private static bool AreTypesCompatible(Type expected, Type? actual)
    {
        if (actual == null)
        {
            return expected == typeof(void);
        }

        if (expected == actual)
        {
            return true;
        }

        // Handle nullable types
        if (expected.IsGenericType && expected.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(expected);
            return underlyingType == actual;
        }

        // Handle common type conversions
        if (expected == typeof(int) && actual == typeof(int?))
        {
            return true;
        }

        if (expected == typeof(int?) && actual == typeof(int))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Compares blueprint status with actual StatusId notification implementations using static analysis.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareStatusNotificationImplementation()
    {
        var blueprintStatus = GetBlueprintStatusIds();
        var implementedNotifications = GetImplementedStatusIdNotifications();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        // Check for missing StatusId notifications
        foreach (var statusId in blueprintStatus)
        {
            if (!implementedNotifications.Contains(statusId))
            {
                missing.Add(statusId);
            }
        }

        // Check for extra StatusId notifications not in blueprint
        foreach (var statusId in implementedNotifications)
        {
            if (!blueprintStatus.Contains(statusId))
            {
                extra.Add(statusId);
            }
        }

        return (missing, extra);
    }

    private static HashSet<string> GetBlueprintStatusIds()
    {
        return SnapDogBlueprint
            .Spec.Status.Required()
            .Select(status => status.Id)
            .ToHashSet();
    }

    private static HashSet<string> GetImplementedStatusIdNotifications()
    {
        var statusIds = new HashSet<string>();
        var assembly = Assembly.Load("SnapDog2");

        var notificationTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in notificationTypes)
        {
            var statusIdAttr = type.GetCustomAttribute<StatusIdAttribute>();
            if (statusIdAttr != null)
            {
                statusIds.Add(statusIdAttr.Id);
            }
        }

        return statusIds;
    }

    /// <summary>
    /// Compares blueprint commands with actual CommandId attribute implementations using static analysis.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareCommandIdImplementation()
    {
        var blueprintCommands = GetBlueprintCommandIds();
        var implementedCommands = GetImplementedCommandIdAttributes();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        // Check for missing CommandId attributes
        foreach (var commandId in blueprintCommands)
        {
            if (!implementedCommands.Contains(commandId))
            {
                missing.Add(commandId);
            }
        }

        // Check for extra CommandId attributes not in blueprint
        foreach (var commandId in implementedCommands)
        {
            if (!blueprintCommands.Contains(commandId))
            {
                extra.Add(commandId);
            }
        }

        return (missing, extra);
    }

    private static HashSet<string> GetBlueprintCommandIds()
    {
        return SnapDogBlueprint
            .Spec.Commands.Required()
            .Where(command => !command.IsExcludedFrom(Protocol.Api))
            .Select(command => command.Id)
            .ToHashSet();
    }

    private static HashSet<string> GetImplementedCommandIdAttributes()
    {
        var commandIds = new HashSet<string>();
        var assembly = Assembly.Load("SnapDog2");

        var types = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            // Check methods for CommandId attributes (controller actions)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var commandIdAttrs = method.GetCustomAttributes(false)
                    .Where(attr => attr.GetType().Name == "CommandIdAttribute")
                    .ToList();

                foreach (var attr in commandIdAttrs)
                {
                    var idProperty = attr.GetType().GetProperty("Id");
                    if (idProperty != null)
                    {
                        var commandId = idProperty.GetValue(attr) as string;
                        if (!string.IsNullOrEmpty(commandId))
                        {
                            commandIds.Add(commandId);
                        }
                    }
                }
            }

            // Also check classes for CommandId attributes (command classes)
            var commandIdAttr = type.GetCustomAttribute<CommandIdAttribute>();
            if (commandIdAttr != null)
            {
                commandIds.Add(commandIdAttr.Id);
            }
        }

        return commandIds;
    }

    private static HashSet<string> GetImplementedStatusIdAttributes()
    {
        var statusIds = new HashSet<string>();
        var assembly = Assembly.Load("SnapDog2");

        var types = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            // Check methods for StatusId attributes (controller actions)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var statusIdAttrs = method.GetCustomAttributes(false)
                    .Where(attr => attr.GetType().Name == "StatusIdAttribute")
                    .ToList();

                foreach (var attr in statusIdAttrs)
                {
                    var idProperty = attr.GetType().GetProperty("Id");
                    if (idProperty != null)
                    {
                        var statusId = idProperty.GetValue(attr) as string;
                        if (!string.IsNullOrEmpty(statusId))
                        {
                            statusIds.Add(statusId);
                        }
                    }
                }
            }
        }

        return statusIds;
    }

    /// <summary>
    /// Compares blueprint status with MQTT notifier implementations.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareStatusMqttNotifierImplementation()
    {
        var blueprintStatusWithMqtt = GetBlueprintStatusWithMqtt();
        var implementedMqttNotifiers = GetImplementedMqttNotifiers();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        foreach (var statusId in blueprintStatusWithMqtt)
        {
            if (!implementedMqttNotifiers.Contains(statusId))
            {
                missing.Add(statusId);
            }
        }

        foreach (var statusId in implementedMqttNotifiers)
        {
            if (!blueprintStatusWithMqtt.Contains(statusId))
            {
                extra.Add(statusId);
            }
        }

        return (missing, extra);
    }

    /// <summary>
    /// Compares blueprint status with KNX notifier implementations.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareStatusKnxNotifierImplementation()
    {
        var blueprintStatusWithKnx = GetBlueprintStatusWithKnx();
        var implementedKnxNotifiers = GetImplementedKnxNotifiers();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        foreach (var statusId in blueprintStatusWithKnx)
        {
            if (!implementedKnxNotifiers.Contains(statusId))
            {
                missing.Add(statusId);
            }
        }

        foreach (var statusId in implementedKnxNotifiers)
        {
            if (!blueprintStatusWithKnx.Contains(statusId))
            {
                extra.Add(statusId);
            }
        }

        return (missing, extra);
    }

    /// <summary>
    /// Compares blueprint commands with MQTT handler implementations.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareCommandMqttHandlerImplementation()
    {
        var blueprintCommandsWithMqtt = GetBlueprintCommandsWithMqtt();
        var implementedMqttHandlers = GetImplementedMqttHandlers();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        foreach (var commandId in blueprintCommandsWithMqtt)
        {
            if (!implementedMqttHandlers.Contains(commandId))
            {
                missing.Add(commandId);
            }
        }

        foreach (var commandId in implementedMqttHandlers)
        {
            if (!blueprintCommandsWithMqtt.Contains(commandId))
            {
                extra.Add(commandId);
            }
        }

        return (missing, extra);
    }

    /// <summary>
    /// Compares blueprint commands with KNX handler implementations.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareCommandKnxHandlerImplementation()
    {
        var blueprintCommandsWithKnx = GetBlueprintCommandsWithKnx();
        var implementedKnxHandlers = GetImplementedKnxHandlers();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        foreach (var commandId in blueprintCommandsWithKnx)
        {
            if (!implementedKnxHandlers.Contains(commandId))
            {
                missing.Add(commandId);
            }
        }

        foreach (var commandId in implementedKnxHandlers)
        {
            if (!blueprintCommandsWithKnx.Contains(commandId))
            {
                extra.Add(commandId);
            }
        }

        return (missing, extra);
    }

    private static HashSet<string> GetBlueprintStatusWithMqtt()
    {
        return SnapDogBlueprint
            .Spec.Status.Required()
            .Where(status => !status.IsExcludedFrom(Protocol.Mqtt))
            .Select(status => status.Id)
            .ToHashSet();
    }

    private static HashSet<string> GetBlueprintStatusWithKnx()
    {
        return SnapDogBlueprint
            .Spec.Status.Required()
            .Where(status => !status.IsExcludedFrom(Protocol.Knx))
            .Select(status => status.Id)
            .ToHashSet();
    }

    private static HashSet<string> GetBlueprintCommandsWithMqtt()
    {
        return SnapDogBlueprint
            .Spec.Commands.Required()
            .Where(command => !command.IsExcludedFrom(Protocol.Mqtt))
            .Select(command => command.Id)
            .ToHashSet();
    }

    private static HashSet<string> GetBlueprintCommandsWithKnx()
    {
        return SnapDogBlueprint
            .Spec.Commands.Required()
            .Where(command => !command.IsExcludedFrom(Protocol.Knx))
            .Select(command => command.Id)
            .ToHashSet();
    }

    private static HashSet<string> GetImplementedMqttNotifiers()
    {
        var mqttNotifiers = new HashSet<string>();
        var assembly = Assembly.Load("SnapDog2");

        var types = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            var mqttNotifierAttr = type.GetCustomAttribute<MqttStatusNotifierAttribute>();
            if (mqttNotifierAttr != null)
            {
                mqttNotifiers.Add(mqttNotifierAttr.StatusId);
            }
        }

        return mqttNotifiers;
    }

    private static HashSet<string> GetImplementedKnxNotifiers()
    {
        var knxNotifiers = new HashSet<string>();
        var assembly = Assembly.Load("SnapDog2");

        var types = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            var knxNotifierAttr = type.GetCustomAttribute<KnxStatusNotifierAttribute>();
            if (knxNotifierAttr != null)
            {
                knxNotifiers.Add(knxNotifierAttr.StatusId);
            }
        }

        return knxNotifiers;
    }

    private static HashSet<string> GetImplementedMqttHandlers()
    {
        // Detect actual MQTT command handler implementations using reflection
        var mqttHandlers = new HashSet<string>();

        try
        {
            var assembly = Assembly.Load("SnapDog2");
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var mqttCommandAttr = method.GetCustomAttributes(false)
                        .FirstOrDefault(attr => attr.GetType().Name == "MqttCommandAttribute");

                    if (mqttCommandAttr != null)
                    {
                        var commandIdProperty = mqttCommandAttr.GetType().GetProperty("CommandId");
                        if (commandIdProperty != null)
                        {
                            var commandId = commandIdProperty.GetValue(mqttCommandAttr) as string;
                            if (!string.IsNullOrEmpty(commandId))
                            {
                                mqttHandlers.Add(commandId);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fallback to intersection method if reflection fails
        }

        // Also include commands that have CommandId attributes and MQTT support
        var commandIdAttributes = GetImplementedCommandIdAttributes();
        var blueprintMqttCommands = GetBlueprintCommandsWithMqtt();
        var commandIdMqttHandlers = commandIdAttributes.Intersect(blueprintMqttCommands);

        foreach (var handler in commandIdMqttHandlers)
        {
            mqttHandlers.Add(handler);
        }

        return mqttHandlers;
    }

    private static HashSet<string> GetImplementedKnxHandlers()
    {
        // Detect actual KNX command handler implementations using reflection
        var knxHandlers = new HashSet<string>();

        try
        {
            var assembly = Assembly.Load("SnapDog2");
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var knxCommandAttr = method.GetCustomAttributes(false)
                        .FirstOrDefault(attr => attr.GetType().Name == "KnxCommandAttribute");

                    if (knxCommandAttr != null)
                    {
                        var commandIdProperty = knxCommandAttr.GetType().GetProperty("CommandId");
                        if (commandIdProperty != null)
                        {
                            var commandId = commandIdProperty.GetValue(knxCommandAttr) as string;
                            if (!string.IsNullOrEmpty(commandId))
                            {
                                knxHandlers.Add(commandId);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fallback to intersection method if reflection fails
            var existingCommandIds = GetImplementedCommandIdAttributes();
            var blueprintWithKnx = GetBlueprintCommandsWithKnx();
            return existingCommandIds.Intersect(blueprintWithKnx).ToHashSet();
        }

        return knxHandlers;
    }
}
