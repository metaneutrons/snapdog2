namespace SnapDog2.Tests.Helpers;

using System.Text.Json;
using SnapDog2.Tests.Blueprint;

/// <summary>
/// Helper for comparing blueprint specification with actual API implementation.
/// Uses OpenAPI specification as single source of truth for actual implementation.
/// </summary>
public static class ApiSpecificationHelper
{
    /// <summary>
    /// Gets all implemented API endpoints from the OpenAPI specification.
    /// </summary>
    public static Dictionary<string, HashSet<string>> GetImplementedApiEndpoints()
    {
        try
        {
            using var httpClient = new HttpClient();
            var openApiJson = httpClient.GetStringAsync("http://localhost:8000/swagger/v1/swagger.json").Result;
            var openApiSpec = JsonDocument.Parse(openApiJson);

            var endpoints = new Dictionary<string, HashSet<string>>();

            if (openApiSpec.RootElement.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    var methods = new HashSet<string>();
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        methods.Add(method.Name.ToUpper());
                    }
                    endpoints[path.Name] = methods;
                }
            }

            return endpoints;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to retrieve OpenAPI specification. Ensure the API is running at localhost:8000",
                ex
            );
        }
    }

    /// <summary>
    /// Compares blueprint commands with actual API implementation.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareCommandImplementation()
    {
        var blueprintCommands = GetBlueprintApiCommands();
        var implementedEndpoints = GetImplementedApiEndpoints();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        // Check for missing commands
        foreach (var (commandId, expectedPath, expectedMethod) in blueprintCommands)
        {
            if (!implementedEndpoints.TryGetValue(expectedPath, out var methods) || !methods.Contains(expectedMethod))
            {
                missing.Add(commandId);
            }
        }

        return (missing, extra);
    }

    /// <summary>
    /// Compares blueprint status with actual API implementation.
    /// </summary>
    public static (HashSet<string> missing, HashSet<string> extra) CompareStatusImplementation()
    {
        var blueprintStatus = GetBlueprintApiStatus();
        var implementedEndpoints = GetImplementedApiEndpoints();

        var missing = new HashSet<string>();
        var extra = new HashSet<string>();

        // Check for missing status endpoints
        foreach (var (statusId, expectedPath, expectedMethod) in blueprintStatus)
        {
            if (!implementedEndpoints.TryGetValue(expectedPath, out var methods) || !methods.Contains(expectedMethod))
            {
                missing.Add(statusId);
            }
        }

        return (missing, extra);
    }

    /// <summary>
    /// Extracts API commands from blueprint specification.
    /// </summary>
    private static List<(string commandId, string path, string method)> GetBlueprintApiCommands()
    {
        return SnapDogBlueprint
            .Spec.Commands.Required()
            .WithApi()
            .Select(cmd => (cmd.Id, cmd.ApiPath ?? "", cmd.HttpMethod ?? ""))
            .Where(x => !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item3))
            .ToList();
    }

    /// <summary>
    /// Extracts API status from blueprint specification.
    /// </summary>
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
