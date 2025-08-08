namespace SnapDog2.Worker.DI;

using System.Reflection;
using Cortex.Mediator.Commands;
using Cortex.Mediator.DependencyInjection;
using Cortex.Mediator.Notifications;
using Cortex.Mediator.Queries;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Server.Behaviors;
using SnapDog2.Server.Features.Shared.Notifications;

/// <summary>
/// Extension methods for configuring Cortex.Mediator services with auto-discovery.
/// Eliminates 50+ manual handler registrations through assembly scanning.
/// Uses shared logging behavior to reduce code duplication.
/// </summary>
public static class CortexMediatorConfiguration
{
    /// <summary>
    /// Adds Cortex.Mediator and related services with auto-discovery.
    /// Automatically discovers and registers all handlers, eliminating manual registration overhead.
    /// Uses shared logging behavior to reduce code duplication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandProcessing(this IServiceCollection services)
    {
        // Get assemblies for auto-discovery
        var serverAssembly = typeof(SharedLoggingCommandBehavior<,>).Assembly;
        var coreAssembly = typeof(SnapDog2.Core.Models.IResult).Assembly;
        var assemblies = new[] { serverAssembly, coreAssembly };

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

                // Performance monitoring
                options.AddOpenCommandPipelineBehavior(typeof(PerformanceCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(PerformanceQueryBehavior<,>));

                // Shared logging implementation (reduces duplication)
                options.AddOpenCommandPipelineBehavior(typeof(SharedLoggingCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(SharedLoggingQueryBehavior<,>));
            }
        );

        // Enhanced auto-discovery with comprehensive handler registration
        RegisterHandlersWithAutoDiscovery(services, assemblies);

        // Register integration services as notification handlers using elegant approach
        RegisterIntegrationNotificationHandlers(services);

        // Auto-register all FluentValidation validators from all assemblies
        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient);
        }

        return services;
    }

    /// <summary>
    /// Enhanced auto-discovery method that comprehensively registers all handlers.
    /// Eliminates the need for 50+ manual registrations through reflection-based discovery.
    /// </summary>
    private static void RegisterHandlersWithAutoDiscovery(IServiceCollection services, Assembly[] assemblies)
    {
        var logger = services.BuildServiceProvider().GetService<ILogger<object>>();
        var registeredHandlers = 0;

        foreach (var assembly in assemblies)
        {
            // Get all handler types from the assembly
            var handlerTypes = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
                .Where(t => t.GetInterfaces().Any(IsHandlerInterface))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                // Register each handler with its interfaces
                var handlerInterfaces = handlerType.GetInterfaces().Where(IsHandlerInterface).ToList();

                foreach (var interfaceType in handlerInterfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                    registeredHandlers++;
                }
            }
        }

        logger?.LogInformation(
            "Auto-discovery registered {HandlerCount} handlers from {AssemblyCount} assemblies",
            registeredHandlers,
            assemblies.Length
        );
    }

    /// <summary>
    /// Determines if a type is a handler interface (Command, Query, or Notification handler).
    /// </summary>
    private static bool IsHandlerInterface(Type interfaceType)
    {
        if (!interfaceType.IsGenericType)
            return false;

        var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(ICommandHandler<,>)
            || genericTypeDefinition == typeof(IQueryHandler<,>)
            || genericTypeDefinition == typeof(INotificationHandler<>);
    }

    /// <summary>
    /// Registers integration services as notification handlers in a DRY way.
    /// Eliminates repetitive registration code and makes it easier to add new services.
    /// </summary>
    private static void RegisterIntegrationNotificationHandlers(IServiceCollection services)
    {
        // Register services that implement INotificationHandler<StatusChangedNotification>
        var integrationServiceTypes = new[] { typeof(ISnapcastService), typeof(IKnxService), typeof(IMqttService) };

        foreach (var serviceType in integrationServiceTypes)
        {
            services.AddScoped<INotificationHandler<StatusChangedNotification>>(provider =>
            {
                var service = provider.GetRequiredService(serviceType);
                return service as INotificationHandler<StatusChangedNotification>
                    ?? throw new InvalidOperationException(
                        $"{serviceType.Name} does not implement INotificationHandler<StatusChangedNotification>"
                    );
            });
        }
    }
}
