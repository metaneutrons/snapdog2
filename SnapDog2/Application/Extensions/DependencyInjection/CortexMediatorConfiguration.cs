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
namespace SnapDog2.Application.Extensions.DependencyInjection;

using System.Reflection;
using Cortex.Mediator.Commands;
using Cortex.Mediator.DependencyInjection;
using Cortex.Mediator.Notifications;
using Cortex.Mediator.Queries;
using FluentValidation;
using SnapDog2.Application.Behaviors;
using SnapDog2.Shared.Models;

/// <summary>
/// Extension methods for configuring Cortex.Mediator services with auto-discovery.
/// </summary>
public static class CortexMediatorConfiguration
{
    /// <summary>
    /// Adds Cortex.Mediator and related services with auto-discovery.
    /// Automatically discovers and registers all handlers, eliminating manual registration overhead.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandProcessing(this IServiceCollection services)
    {
        // Get assemblies for auto-discovery
        var serverAssembly = typeof(SharedLoggingCommandBehavior<,>).Assembly;
        var coreAssembly = typeof(IResult).Assembly;
        var apiAssembly = typeof(SnapDog2.Api.Hubs.SnapDogHub).Assembly;
        var assemblies = new[] { serverAssembly, coreAssembly, apiAssembly };

        // Add Cortex.Mediator with enhanced auto-discovery
        services.AddCortexMediator(
            new ConfigurationBuilder().Build().GetSection("Mediator"),
            new[] { typeof(SharedLoggingCommandBehavior<,>) }, // Use type array as expected
            options =>
            {
                // Add pipeline behaviors in execution order
                // Validation first to fail fast
                options.AddOpenCommandPipelineBehavior(typeof(ValidationCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(ValidationQueryBehavior<,>));

                // Enterprise-grade performance monitoring with metrics
                options.AddOpenCommandPipelineBehavior(typeof(PerformanceCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(PerformanceQueryBehavior<,>));

                // Shared logging implementation
                options.AddOpenCommandPipelineBehavior(typeof(SharedLoggingCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(SharedLoggingQueryBehavior<,>));
            }
        );

        // Enhanced auto-discovery with comprehensive handler registration (but suppress duplicate warnings)
        RegisterHandlersWithAutoDiscovery(services, assemblies);

        // Auto-register all FluentValidation validators from all assemblies
        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient);
        }

        return services;
    }

    /// <summary>
    /// Auto-discovery method that comprehensively registers all handlers.
    /// Registers both interface types (for mediator) and concrete types (for direct injection).
    /// Prevents duplicate registrations by checking existing service descriptors.
    /// </summary>
    private static void RegisterHandlersWithAutoDiscovery(IServiceCollection services, Assembly[] assemblies)
    {
        var logger = services.BuildServiceProvider().GetService<ILogger<object>>();
        var registeredHandlers = 0;

        // Get all unique handler types across all assemblies
        var allHandlerTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.GetInterfaces().Any(IsHandlerInterface))
            .Distinct()
            .ToList();

        logger?.LogInformation("Found {HandlerTypeCount} handler types to register", allHandlerTypes.Count);

        foreach (var handlerType in allHandlerTypes)
        {
            // Register the concrete type (for direct injection compatibility) - only if not already registered
            if (!services.Any(s => s.ServiceType == handlerType && s.ImplementationType == handlerType))
            {
                services.AddScoped(handlerType);
                registeredHandlers++;
                logger?.LogDebug("Registered concrete type: {HandlerType}", handlerType.Name);
            }

            // Register each handler with its interfaces (for mediator pattern) - only if not already registered
            var handlerInterfaces = handlerType.GetInterfaces().Where(IsHandlerInterface).ToList();

            foreach (var interfaceType in handlerInterfaces)
            {
                if (!services.Any(s => s.ServiceType == interfaceType && s.ImplementationType == handlerType))
                {
                    services.AddScoped(interfaceType, handlerType);
                    registeredHandlers++;
                    logger?.LogDebug(
                        "Registered interface: {InterfaceType} -> {HandlerType}",
                        interfaceType.Name,
                        handlerType.Name
                    );
                }
                else
                {
                    // Changed from LogWarning to LogDebug to reduce noise
                    logger?.LogDebug(
                        "Skipped duplicate registration: {InterfaceType} -> {HandlerType}",
                        interfaceType.Name,
                        handlerType.Name
                    );
                }
            }
        }

        logger?.LogInformation(
            "Auto-discovery registered {HandlerCount} unique handler registrations from {AssemblyCount} assemblies ({HandlerTypes} handler types)",
            registeredHandlers,
            assemblies.Length,
            allHandlerTypes.Count
        );
    }

    /// <summary>
    /// Determines if a type is a handler interface (Command, Query, or Notification handler).
    /// </summary>
    private static bool IsHandlerInterface(Type interfaceType)
    {
        if (!interfaceType.IsGenericType)
        {
            return false;
        }

        var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(ICommandHandler<,>)
            || genericTypeDefinition == typeof(IQueryHandler<,>)
            || genericTypeDefinition == typeof(INotificationHandler<>);
    }

}
