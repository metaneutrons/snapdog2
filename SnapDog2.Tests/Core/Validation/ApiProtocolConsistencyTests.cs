namespace SnapDog2.Tests.Core.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Tests.Helpers;

/// <summary>
/// Consistency tests for API protocol implementation.
/// These tests validate that all registered commands and status have corresponding
/// API endpoints and that the API layer properly implements the command framework.
/// </summary>
public class ApiProtocolConsistencyTests
{
    private readonly List<string> _allRegisteredCommands;
    private readonly List<string> _allRegisteredStatus;
    private readonly List<Type> _controllerTypes;
    private readonly List<MethodInfo> _apiEndpoints;

    public ApiProtocolConsistencyTests()
    {
        // Initialize test data
        ConsistencyTestHelpers.InitializeRegistries();

        _allRegisteredCommands = ConsistencyTestHelpers.GetAllRegisteredCommands();
        _allRegisteredStatus = ConsistencyTestHelpers.GetAllRegisteredStatus();
        _controllerTypes = ConsistencyTestHelpers.GetAllControllerTypes();
        _apiEndpoints = ConsistencyTestHelpers.GetAllApiEndpoints();
    }

    /// <summary>
    /// Validates that all registered commands have corresponding API endpoints.
    /// This test ensures that every command can be invoked via the REST API.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void AllRegisteredCommands_ShouldHaveApiEndpoints()
    {
        // Arrange
        var commandsWithEndpoints = new HashSet<string>();

        // Extract command IDs from API endpoints
        foreach (var endpoint in _apiEndpoints)
        {
            var commandIds = ConsistencyTestHelpers.ExtractCommandIdsFromApiEndpoint(endpoint);
            foreach (var commandId in commandIds)
            {
                commandsWithEndpoints.Add(commandId);
            }
        }

        // Act & Assert
        foreach (var registeredCommand in _allRegisteredCommands)
        {
            commandsWithEndpoints
                .Should()
                .Contain(
                    registeredCommand,
                    $"Command '{registeredCommand}' is registered but has no corresponding API endpoint"
                );
        }
    }

    /// <summary>
    /// Validates that all registered status have corresponding API endpoints for retrieval.
    /// This test ensures that every status can be queried via the REST API.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void AllRegisteredStatus_ShouldHaveApiEndpoints()
    {
        // Arrange
        var statusWithEndpoints = new HashSet<string>();

        // Extract status IDs from API endpoints
        foreach (var endpoint in _apiEndpoints)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromApiEndpoint(endpoint);
            foreach (var statusId in statusIds)
            {
                statusWithEndpoints.Add(statusId);
            }
        }

        // Act & Assert
        foreach (var registeredStatus in _allRegisteredStatus)
        {
            statusWithEndpoints
                .Should()
                .Contain(
                    registeredStatus,
                    $"Status '{registeredStatus}' is registered but has no corresponding API endpoint"
                );
        }
    }

    /// <summary>
    /// Validates that API controllers follow proper naming conventions and structure.
    /// This test ensures consistency in API controller organization.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiControllers_ShouldFollowNamingConventions()
    {
        // Arrange
        var invalidControllers = new List<string>();

        // Validate controller naming conventions
        foreach (var controllerType in _controllerTypes)
        {
            if (!ConsistencyTestHelpers.IsValidControllerName(controllerType.Name))
            {
                invalidControllers.Add(controllerType.Name);
            }
        }

        // Act & Assert
        invalidControllers
            .Should()
            .BeEmpty($"Found controllers with invalid naming: {string.Join(", ", invalidControllers)}");
    }

    /// <summary>
    /// Validates that API endpoints have proper HTTP method attributes.
    /// This test ensures that all endpoints are properly configured for REST operations.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiEndpoints_ShouldHaveProperHttpMethods()
    {
        // Arrange
        var endpointsWithoutHttpMethods = new List<string>();

        // Check each endpoint for HTTP method attributes
        foreach (var endpoint in _apiEndpoints)
        {
            var hasHttpMethodAttribute = endpoint
                .GetCustomAttributes()
                .Any(attr => ConsistencyTestHelpers.IsHttpMethodAttribute(attr.GetType()));

            if (!hasHttpMethodAttribute)
            {
                endpointsWithoutHttpMethods.Add($"{endpoint.DeclaringType?.Name}.{endpoint.Name}");
            }
        }

        // Act & Assert
        endpointsWithoutHttpMethods
            .Should()
            .BeEmpty(
                $"Found API endpoints without HTTP method attributes: {string.Join(", ", endpointsWithoutHttpMethods)}"
            );
    }

    /// <summary>
    /// Validates that API endpoints have proper route attributes.
    /// This test ensures that all endpoints are accessible via proper URL routes.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiEndpoints_ShouldHaveProperRoutes()
    {
        // Arrange
        var endpointsWithoutRoutes = new List<string>();

        // Check each endpoint for route attributes
        foreach (var endpoint in _apiEndpoints)
        {
            var hasRouteAttribute = endpoint
                .GetCustomAttributes()
                .Any(attr => ConsistencyTestHelpers.IsRouteAttribute(attr.GetType()));

            // Also check if the controller has a route attribute
            var controllerHasRoute =
                endpoint
                    .DeclaringType?.GetCustomAttributes()
                    .Any(attr => ConsistencyTestHelpers.IsRouteAttribute(attr.GetType())) ?? false;

            if (!hasRouteAttribute && !controllerHasRoute)
            {
                endpointsWithoutRoutes.Add($"{endpoint.DeclaringType?.Name}.{endpoint.Name}");
            }
        }

        // Act & Assert
        endpointsWithoutRoutes
            .Should()
            .BeEmpty($"Found API endpoints without route attributes: {string.Join(", ", endpointsWithoutRoutes)}");
    }

    /// <summary>
    /// Validates that command endpoints use appropriate HTTP methods (POST for commands).
    /// This test ensures that command operations follow REST conventions.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void CommandEndpoints_ShouldUsePostMethod()
    {
        // Arrange
        var commandEndpointsWithWrongMethod = new List<string>();

        // Check command endpoints for proper HTTP methods
        foreach (var endpoint in _apiEndpoints)
        {
            var commandIds = ConsistencyTestHelpers.ExtractCommandIdsFromApiEndpoint(endpoint);
            if (commandIds.Any())
            {
                var httpMethods = ConsistencyTestHelpers.GetHttpMethodsFromEndpoint(endpoint);
                if (!httpMethods.Contains("POST"))
                {
                    commandEndpointsWithWrongMethod.Add(
                        $"{endpoint.DeclaringType?.Name}.{endpoint.Name} (uses: {string.Join(", ", httpMethods)})"
                    );
                }
            }
        }

        // Act & Assert
        commandEndpointsWithWrongMethod
            .Should()
            .BeEmpty(
                $"Found command endpoints not using POST method: {string.Join(", ", commandEndpointsWithWrongMethod)}"
            );
    }

    /// <summary>
    /// Validates that status endpoints use appropriate HTTP methods (GET for status queries).
    /// This test ensures that status operations follow REST conventions.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void StatusEndpoints_ShouldUseGetMethod()
    {
        // Arrange
        var statusEndpointsWithWrongMethod = new List<string>();

        // Check status endpoints for proper HTTP methods
        foreach (var endpoint in _apiEndpoints)
        {
            var statusIds = ConsistencyTestHelpers.ExtractStatusIdsFromApiEndpoint(endpoint);
            if (statusIds.Any())
            {
                var httpMethods = ConsistencyTestHelpers.GetHttpMethodsFromEndpoint(endpoint);
                if (!httpMethods.Contains("GET"))
                {
                    statusEndpointsWithWrongMethod.Add(
                        $"{endpoint.DeclaringType?.Name}.{endpoint.Name} (uses: {string.Join(", ", httpMethods)})"
                    );
                }
            }
        }

        // Act & Assert
        statusEndpointsWithWrongMethod
            .Should()
            .BeEmpty(
                $"Found status endpoints not using GET method: {string.Join(", ", statusEndpointsWithWrongMethod)}"
            );
    }

    /// <summary>
    /// Validates that API endpoints have proper parameter validation.
    /// This test ensures that endpoints validate input parameters appropriately.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiEndpoints_ShouldHaveProperParameterValidation()
    {
        // Arrange
        var endpointsWithoutValidation = new List<string>();

        // Check endpoints for parameter validation attributes
        foreach (var endpoint in _apiEndpoints)
        {
            var parameters = endpoint.GetParameters();
            foreach (var parameter in parameters)
            {
                if (ConsistencyTestHelpers.RequiresValidation(parameter.ParameterType))
                {
                    var hasValidationAttributes = parameter
                        .GetCustomAttributes()
                        .Any(attr => ConsistencyTestHelpers.IsValidationAttribute(attr.GetType()));

                    if (!hasValidationAttributes)
                    {
                        endpointsWithoutValidation.Add(
                            $"{endpoint.DeclaringType?.Name}.{endpoint.Name}({parameter.Name})"
                        );
                    }
                }
            }
        }

        // Act & Assert
        endpointsWithoutValidation
            .Should()
            .BeEmpty(
                $"Found API endpoints with parameters lacking validation: {string.Join(", ", endpointsWithoutValidation)}"
            );
    }

    /// <summary>
    /// Validates that API controllers are properly registered with dependency injection.
    /// This test ensures that all controllers can be instantiated by the DI container.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiControllers_ShouldBeProperlyRegistered()
    {
        // Arrange
        var unregisteredControllers = new List<string>();

        // Check if controllers have proper constructors for DI
        foreach (var controllerType in _controllerTypes)
        {
            if (!ConsistencyTestHelpers.HasValidDependencyInjectionConstructor(controllerType))
            {
                unregisteredControllers.Add(controllerType.Name);
            }
        }

        // Act & Assert
        unregisteredControllers
            .Should()
            .BeEmpty($"Found controllers without proper DI constructors: {string.Join(", ", unregisteredControllers)}");
    }

    /// <summary>
    /// Validates that API responses follow consistent formatting and structure.
    /// This test ensures that all endpoints return properly structured responses.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiEndpoints_ShouldHaveConsistentResponseTypes()
    {
        // Arrange
        var endpointsWithInconsistentResponses = new List<string>();

        // Check endpoint return types for consistency
        foreach (var endpoint in _apiEndpoints)
        {
            if (!ConsistencyTestHelpers.HasConsistentResponseType(endpoint))
            {
                endpointsWithInconsistentResponses.Add($"{endpoint.DeclaringType?.Name}.{endpoint.Name}");
            }
        }

        // Act & Assert
        endpointsWithInconsistentResponses
            .Should()
            .BeEmpty(
                $"Found API endpoints with inconsistent response types: {string.Join(", ", endpointsWithInconsistentResponses)}"
            );
    }

    /// <summary>
    /// Validates that API endpoints have proper error handling.
    /// This test ensures that all endpoints handle exceptions appropriately.
    /// </summary>
    [Fact]
    [Trait("Category", "Consistency")]
    [Trait("Category", "ApiConsistency")]
    public void ApiEndpoints_ShouldHaveProperErrorHandling()
    {
        // Arrange
        var endpointsWithoutErrorHandling = new List<string>();

        // Check endpoints for error handling patterns
        foreach (var endpoint in _apiEndpoints)
        {
            if (!ConsistencyTestHelpers.HasProperErrorHandling(endpoint))
            {
                endpointsWithoutErrorHandling.Add($"{endpoint.DeclaringType?.Name}.{endpoint.Name}");
            }
        }

        // Act & Assert
        endpointsWithoutErrorHandling
            .Should()
            .BeEmpty(
                $"Found API endpoints without proper error handling: {string.Join(", ", endpointsWithoutErrorHandling)}"
            );
    }
}
