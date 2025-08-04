namespace SnapDog2.Infrastructure.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Audio;

/// <summary>
/// Extension methods for registering SoundFlow services.
/// </summary>
public static class SoundFlowServiceCollectionExtensions
{
    /// <summary>
    /// Registers SoundFlow audio services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSoundFlowServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure SoundFlow settings
        services.Configure<SoundFlowConfig>(configuration.GetSection("SoundFlow"));

        // Register HTTP client for SoundFlow streaming with resilience policies
        services
            .AddHttpClient(
                "SoundFlowStreaming",
                (serviceProvider, client) =>
                {
                    var soundFlowConfig = serviceProvider.GetRequiredService<IOptions<SoundFlowConfig>>().Value;

                    client.DefaultRequestHeaders.Add("User-Agent", "SnapDog2-SoundFlow/1.0");
                    client.DefaultRequestHeaders.Add("Accept", "audio/*");
                    client.Timeout = TimeSpan.FromSeconds(soundFlowConfig.HttpTimeoutSeconds);
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

        // Register the SoundFlow media player service
        services.AddSingleton<IMediaPlayerService, SoundFlowMediaPlayerService>();

        return services;
    }
}

/// <summary>
/// Extension methods for Polly context.
/// </summary>
internal static class PollyContextExtensions
{
    private const string LoggerKey = "ILogger";

    /// <summary>
    /// Gets the logger from the Polly context.
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out var logger))
        {
            return logger as Microsoft.Extensions.Logging.ILogger;
        }
        return null;
    }

    /// <summary>
    /// Sets the logger in the Polly context.
    /// </summary>
    public static Context WithLogger(this Context context, Microsoft.Extensions.Logging.ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }
}
