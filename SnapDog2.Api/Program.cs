using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Authorization;
using SnapDog2.Api.Configuration;
using SnapDog2.Api.Extensions;
using SnapDog2.Api.Middleware;
using SnapDog2.Infrastructure;
using SnapDog2.Server.Extensions;

namespace SnapDog2.Api;

/// <summary>
/// Main entry point for the SnapDog2 API application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();

        // Add API documentation
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "SnapDog2 API",
                    Version = "v1",
                    Description = "Multi-room audio streaming system API",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "SnapDog2 Team",
                        Email = "support@snapdog2.local",
                    },
                }
            );

            // Add API Key security definition
            options.AddSecurityDefinition(
                "ApiKey",
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "API Key needed to access the endpoints. Format: X-API-Key: {your-api-key}",
                    Name = "X-API-Key",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme",
                }
            );

            options.AddSecurityRequirement(
                new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "ApiKey",
                            },
                        },
                        Array.Empty<string>()
                    },
                }
            );

            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // Add basic services needed for the API
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();

        // Load and register configurations
        var rateLimitingConfig =
            builder.Configuration.GetSection("RateLimiting").Get<RateLimitingConfiguration>()
            ?? RateLimitingConfiguration.CreateDefault();
        builder.Services.AddSingleton(rateLimitingConfig);

        var loggingOptions =
            builder.Configuration.GetSection("RequestResponseLogging").Get<RequestResponseLoggingOptions>()
            ?? RequestResponseLoggingOptions.CreateDefault();
        builder.Services.AddSingleton(loggingOptions);

        var validationOptions =
            builder.Configuration.GetSection("InputValidation").Get<InputValidationOptions>()
            ?? InputValidationOptions.CreateDefault();
        builder.Services.AddSingleton(validationOptions);

        // Configure rate limiting
        builder.Services.Configure<IpRateLimitOptions>(options =>
        {
            var rateLimitOptions = rateLimitingConfig.ToIpRateLimitOptions();
            options.EnableEndpointRateLimiting = rateLimitOptions.EnableEndpointRateLimiting;
            options.StackBlockedRequests = rateLimitOptions.StackBlockedRequests;
            options.HttpStatusCode = rateLimitOptions.HttpStatusCode;
            options.RealIpHeader = rateLimitOptions.RealIpHeader;
            options.ClientIdHeader = rateLimitOptions.ClientIdHeader;
            options.GeneralRules = rateLimitOptions.GeneralRules;
            options.IpWhitelist = rateLimitOptions.IpWhitelist;
            options.ClientWhitelist = rateLimitOptions.ClientWhitelist;
            options.QuotaExceededResponse = rateLimitOptions.QuotaExceededResponse;
        });

        builder.Services.Configure<IpRateLimitPolicies>(policies =>
        {
            var rateLimitPolicies = rateLimitingConfig.ToIpRateLimitPolicies();
            policies.IpRules = rateLimitPolicies.IpRules;
        });
        builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

        // TODO: Add infrastructure layer services when repositories are implemented
        // For now, we'll register minimal services to get the API running

        // Add basic MediatR for controllers (without server layer dependencies for now)
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        // Add authorization services
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, ApiAuthorizationPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnershipHandler>();

        // Add API security (authentication, authorization, CORS, security headers)
        builder.Services.AddApiSecurity();
        builder.Services.AddSecurityHeaders();
        builder.Services.AddApiCors();

        // Add logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Configure logging levels
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SnapDog2 API v1");
                options.RoutePrefix = "swagger";
                options.DisplayRequestDuration();
                options.EnableTryItOutByDefault();
            });
        }

        // Security and infrastructure middleware pipeline (order matters!)

        // 1. Input validation and sanitization (first line of defense)
        app.UseMiddleware<InputValidationMiddleware>();

        // 2. Request/Response logging with correlation ID
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        // 3. Rate limiting (after logging, before expensive operations)
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseIpRateLimiting(); // AspNetCoreRateLimit middleware

        // 4. Security headers and CORS
        app.UseApiSecurity();

        // 5. Standard middleware
        app.UseRouting();

        // Map controllers
        app.MapControllers();

        // Add health check endpoint
        app.MapGet(
            "/",
            () =>
                new
                {
                    Service = "SnapDog2 API",
                    Version = "1.0.0",
                    Status = "Running",
                    Timestamp = DateTime.UtcNow,
                    Documentation = "/swagger",
                }
        );

        // Log startup information
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("SnapDog2 API starting up...");
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
        logger.LogInformation("Swagger UI available at: /swagger");

        try
        {
            app.Run();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
    }
}
