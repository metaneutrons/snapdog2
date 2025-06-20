using System.Net;
using System.Text.Json;
using FluentValidation; // Needs this using
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace SnapDog2.Api.Middleware;

public class FluentValidationExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FluentValidationExceptionHandlerMiddleware> _logger;

    public FluentValidationExceptionHandlerMiddleware(RequestDelegate next, ILogger<FluentValidationExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex) // Specifically FluentValidation.ValidationException
        {
            _logger.LogWarning(ex, "Validation error occurred: {ValidationErrors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors.Select(e => new {
                e.PropertyName,
                e.ErrorMessage,
                e.AttemptedValue
            });

            // Consistent problem details format
            var problemDetails = new {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", // Link to RFC for 400 Bad Request
                title = "One or more validation errors occurred.",
                status = (int)HttpStatusCode.BadRequest,
                errors // Using the structured errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }));
        }
    }
}
