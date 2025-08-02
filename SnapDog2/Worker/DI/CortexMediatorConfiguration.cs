namespace SnapDog2.Worker.DI;

using System.Reflection;
using Cortex.Mediator.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Server.Behaviors;

/// <summary>
/// Extension methods for configuring Cortex.Mediator services.
/// </summary>
public static class CortexMediatorConfiguration
{
    /// <summary>
    /// Adds Cortex.Mediator and related services (handlers, validators, behaviors) to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandProcessing(this IServiceCollection services)
    {
        // Get the assembly containing the Server layer code
        var serverAssembly = typeof(LoggingCommandBehavior<,>).Assembly;

        // Add Cortex.Mediator using the proper extension method
        services.AddCortexMediator(
            new ConfigurationBuilder().Build().GetSection("Mediator"), 
            new[] { typeof(LoggingCommandBehavior<,>) }, 
            options => 
            {
                // Add default behaviors (logging)
                options.AddDefaultBehaviors();
                
                // Add custom pipeline behaviors
                options.AddOpenCommandPipelineBehavior(typeof(LoggingCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(LoggingQueryBehavior<,>));
                options.AddOpenCommandPipelineBehavior(typeof(PerformanceCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(PerformanceQueryBehavior<,>));
                options.AddOpenCommandPipelineBehavior(typeof(ValidationCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(ValidationQueryBehavior<,>));
            }
        );

        // Manually register query handlers since auto-discovery isn't working
        services.AddScoped<SnapDog2.Server.Features.Global.Handlers.GetSystemStatusQueryHandler>();
        services.AddScoped<SnapDog2.Server.Features.Global.Handlers.GetErrorStatusQueryHandler>();
        services.AddScoped<SnapDog2.Server.Features.Global.Handlers.GetVersionInfoQueryHandler>();
        services.AddScoped<SnapDog2.Server.Features.Global.Handlers.GetServerStatsQueryHandler>();

        // Automatically register all FluentValidation AbstractValidator<> implementations
        // found in the specified assembly. These are used by the ValidationBehavior.
        services.AddValidatorsFromAssembly(serverAssembly, ServiceLifetime.Transient);

        return services;
    }
}
