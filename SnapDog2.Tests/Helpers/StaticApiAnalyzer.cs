namespace SnapDog2.Tests.Helpers;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
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
        var blueprintStatus = GetBlueprintApiStatus();
        var implementedEndpoints = GetImplementedApiEndpoints();

        var missing = new HashSet<string>();
        var blueprintEndpoints = new HashSet<string>();

        // Check for missing implementations
        foreach (var (statusId, expectedPath, expectedMethod) in blueprintStatus)
        {
            var endpointKey = $"{expectedMethod.ToUpper()} {expectedPath}";
            blueprintEndpoints.Add(endpointKey);

            if (
                !implementedEndpoints.TryGetValue(expectedPath, out var methods)
                || !methods.Contains(expectedMethod.ToUpper())
            )
            {
                missing.Add(statusId);
            }
        }

        // Check for extra implementations (orphaned) - only GET methods for status
        var extra = new HashSet<string>();
        foreach (var (path, methods) in implementedEndpoints)
        {
            foreach (var method in methods)
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
            var template = routeAttribute.Template ?? "";
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

    private static List<(string statusId, string path, string method)> GetBlueprintApiStatus()
    {
        return SnapDogBlueprint
            .Spec.Status.Required()
            .WithApi()
            .Select(status => (status.Id, status.ApiPath ?? "", status.HttpMethod ?? ""))
            .Where(x => !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item3))
            .ToList();
    }
}
