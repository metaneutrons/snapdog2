using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Server.Behaviors;
using SnapDog2.Server.Caching;
using SnapDog2.Server.Monitoring;

namespace SnapDog2.Server.Extensions;

/// <summary>
/// Extension methods for configuring MediatR and related services in the dependency injection container.
/// Provides centralized configuration for the server layer business logic components.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MediatR configuration for the server layer.
    /// Registers MediatR with assembly scanning, FluentValidation validators, and pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddServerLayer(this IServiceCollection services)
    {
        // Register MediatR with assembly scanning (using MediatR 12.x built-in registration)
        services.AddMediatR(config =>
        {
            // Register handlers from the current assembly (Server layer)
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // Register handlers from Core assembly if needed
            var coreAssembly = Assembly.GetAssembly(typeof(Core.Models.Entities.AudioStream));
            if (coreAssembly != null)
            {
                config.RegisterServicesFromAssembly(coreAssembly);
            }
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register MediatR pipeline behaviors for cross-cutting concerns
        services.AddMediatRBehaviors();

        return services;
    }

    /// <summary>
    /// Adds MediatR with minimal configuration for basic command/query handling.
    /// Use this method when you need only basic MediatR functionality without additional behaviors.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddBasicMediatR(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        return services;
    }

    /// <summary>
    /// Adds FluentValidation configuration for the server layer.
    /// Registers all validators from the current assembly with automatic discovery.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddServerValidation(this IServiceCollection services)
    {
        // Add FluentValidation with automatic validator discovery
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure FluentValidation global settings
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        return services;
    }

    /// <summary>
    /// Adds audio stream feature services.
    /// Registers all services specific to audio stream management functionality.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddAudioStreamFeatures(this IServiceCollection services)
    {
        // Register audio stream-specific validators
        services.AddValidatorsFromAssemblyContaining<Features.AudioStreams.Validators.CreateAudioStreamValidator>();

        // Register audio stream-specific handlers (automatically discovered by MediatR)
        // Command handlers are automatically registered when AddMediatR is called

        // Register audio stream-specific services (to be added in future phases)
        // services.AddScoped<IAudioStreamService, AudioStreamService>();

        return services;
    }

    /// <summary>
    /// Adds MediatR pipeline behaviors for cross-cutting concerns.
    /// Registers all pipeline behaviors in the correct order for optimal request processing.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddMediatRBehaviors(this IServiceCollection services)
    {
        // Register supporting services first
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

        // Register pipeline behaviors in execution order (last registered = first executed)
        // The order is critical for proper request processing:

        // 1. ErrorHandlingBehavior - Outermost layer to catch all exceptions
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ErrorHandlingBehavior<,>));

        // 2. TransactionBehavior - Manage database transactions for commands
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // 3. PerformanceBehavior - Monitor execution time
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        // 4. CachingBehavior - Check cache for queries before processing
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        // 5. ValidationBehavior - Validate inputs before processing
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // 6. LoggingBehavior - Log all requests and responses
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // 7. AuthorizationBehavior - Security checks (innermost, closest to handler)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        return services;
    }

    /// <summary>
    /// Validates the MediatR configuration by ensuring all required handlers are registered.
    /// This method can be used during application startup to verify the configuration.
    /// </summary>
    /// <param name="services">The service collection to validate.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection ValidateMediatRConfiguration(this IServiceCollection services)
    {
        // Build a temporary service provider to validate registrations
        using var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Verify MediatR is registered
            var mediator = serviceProvider.GetService<IMediator>();
            if (mediator == null)
            {
                throw new InvalidOperationException("MediatR is not properly registered. Call AddMediatR() first.");
            }

            // Verify key handlers are registered
            var handlerTypes = new[]
            {
                typeof(IRequestHandler<
                    Features.AudioStreams.Commands.CreateAudioStreamCommand,
                    Core.Common.Result<Core.Models.Entities.AudioStream>
                >),
                typeof(IRequestHandler<Features.AudioStreams.Commands.StartAudioStreamCommand, Core.Common.Result>),
                typeof(IRequestHandler<Features.AudioStreams.Commands.StopAudioStreamCommand, Core.Common.Result>),
                typeof(IRequestHandler<Features.AudioStreams.Commands.DeleteAudioStreamCommand, Core.Common.Result>),
            };

            foreach (var handlerType in handlerTypes)
            {
                var handler = serviceProvider.GetService(handlerType);
                if (handler == null)
                {
                    throw new InvalidOperationException($"Handler {handlerType.Name} is not registered.");
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("MediatR configuration validation failed.", ex);
        }

        return services;
    }

    /// <summary>
    /// Gets information about registered MediatR handlers for debugging purposes.
    /// </summary>
    /// <param name="services">The service collection to inspect.</param>
    /// <returns>A dictionary containing handler information.</returns>
    public static Dictionary<string, string> GetMediatRHandlerInfo(this IServiceCollection services)
    {
        var handlerInfo = new Dictionary<string, string>();

        // Find all registered MediatR handlers
        var mediatRServices = services
            .Where(s =>
                s.ServiceType.IsGenericType
                && (
                    s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    || s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<>)
                )
            )
            .ToList();

        foreach (var service in mediatRServices)
        {
            var serviceTypeName = service.ServiceType.Name;
            var implementationTypeName = service.ImplementationType?.Name ?? "Unknown";
            handlerInfo[serviceTypeName] = implementationTypeName;
        }

        return handlerInfo;
    }
}
