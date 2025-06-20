using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Api.Authorization;
using SnapDog2.Api.Configuration;
using SnapDog2.Api.Extensions;
using SnapDog2.Api.Middleware;
using SnapDog2.Core.Configuration; // Added for KnxAddressConverter
using SnapDog2.Infrastructure;
using SnapDog2.Server.Extensions;
using Microsoft.AspNetCore.Diagnostics; // Required for IExceptionHandlerPathFeature
// Removed: using SnapDog2.Api.Exceptions;

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
        builder
            .Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new KnxAddressConverter());
                // Add other converters here if needed in the future
            });

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

        // Custom rate limiting configuration (AspNetCoreRateLimit services removed to avoid conflicts)

        // TODO: Add infrastructure layer services when repositories are implemented
        // For now, we'll register minimal services to get the API running

        // Register API Authentication Configuration
        var apiAuthConfig = ApiAuthConfiguration.LoadFromEnvironment();
        builder.Services.AddSingleton(apiAuthConfig);

        // Add basic MediatR for controllers (without server layer dependencies for now)
        // builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)); // Replaced by AddServerLayer
        builder.Services.AddServerLayer(); // Registers MediatR, validators, and behaviors from SnapDog2.Server

        // Add authorization services
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, ApiAuthorizationPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnershipHandler>();

        // Add API security (authentication, authorization, CORS, security headers)
        // AddApiSecurity now relies on ApiAuthConfiguration being pre-registered.
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

        // VERY FIRST: Global Exception Handler
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;
                var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("GlobalExceptionHandler");

                if (exception is FluentValidation.ValidationException validationException) // Reverted to FluentValidation.ValidationException
                {
                    // No logging in this path for extreme simplification
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"title\":\"Validation Error\",\"status\":400}");
                }
                else
                {
                    // No logging for extreme simplification here either for the moment
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json"; // Simplified for consistency
                    await context.Response.WriteAsync("{\"title\":\"Internal Server Error\",\"status\":500}");
                }
            });
        });

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
        // FluentValidationExceptionHandlerMiddleware was removed. InputValidationMiddleware is next.

        // 1. Input validation and sanitization (first line of defense)
        app.UseMiddleware<InputValidationMiddleware>();

        // 2. Request/Response logging with correlation ID
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        // 3. Rate limiting (after logging, before expensive operations)
        app.UseMiddleware<RateLimitingMiddleware>();

        // 4. Standard middleware
        app.UseRouting();

        // 5. Security headers and CORS (includes Authentication and Authorization)
        app.UseApiSecurity();

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
