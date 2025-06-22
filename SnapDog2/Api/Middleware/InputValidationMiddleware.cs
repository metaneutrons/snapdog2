using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace SnapDog2.Api.Middleware;

/// <summary>
/// Middleware for input validation and sanitization to prevent XSS and injection attacks.
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private readonly InputValidationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The validation options.</param>
    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger,
        InputValidationOptions options
    )
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Skip validation for excluded paths
        if (ShouldSkipValidation(context))
        {
            await _next(context);
            return;
        }

        // Validate and sanitize query parameters
        if (_options.ValidateQueryParameters)
        {
            var validationResult = ValidateQueryParameters(context);
            if (!validationResult.IsValid)
            {
                await ReturnValidationError(context, validationResult);
                return;
            }
        }

        // Validate and sanitize headers
        if (_options.ValidateHeaders)
        {
            var headerValidationResult = ValidateHeaders(context);
            if (!headerValidationResult.IsValid)
            {
                await ReturnValidationError(context, headerValidationResult);
                return;
            }
        }

        // Validate and sanitize request body
        if (_options.ValidateRequestBody && HasRequestBody(context))
        {
            var bodyValidationResult = await ValidateRequestBodyAsync(context);
            if (!bodyValidationResult.IsValid)
            {
                await ReturnValidationError(context, bodyValidationResult);
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Determines if validation should be skipped for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if validation should be skipped; otherwise, false.</returns>
    private bool ShouldSkipValidation(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        return _options.ExcludedPaths.Any(excludedPath => path?.StartsWith(excludedPath.ToLowerInvariant()) == true);
    }

    /// <summary>
    /// Determines if the request has a body that should be validated.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if the request has a body; otherwise, false.</returns>
    private bool HasRequestBody(HttpContext context)
    {
        return context.Request.ContentLength > 0
            || string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Request.Method, "PUT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Request.Method, "PATCH", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates query parameters.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The validation result.</returns>
    private ValidationResult ValidateQueryParameters(HttpContext context)
    {
        var errors = new List<string>();

        foreach (var param in context.Request.Query)
        {
            var key = param.Key;
            var values = param.Value;

            // Validate parameter name
            if (!IsValidParameterName(key))
            {
                errors.Add($"Invalid parameter name: {key}");
                _logger.LogWarning("Invalid query parameter name detected: {ParameterName}", key);
                continue;
            }

            // Validate parameter values
            foreach (var value in values)
            {
                if (value == null)
                    continue;

                var sanitizedValue = SanitizeString(value);
                if (sanitizedValue != value)
                {
                    _logger.LogWarning(
                        "Potentially malicious content detected in query parameter {ParameterName}: {Value}",
                        key,
                        value
                    );
                }

                if (ContainsMaliciousContent(value))
                {
                    errors.Add($"Malicious content detected in parameter: {key}");
                }

                if (value.Length > _options.MaxParameterLength)
                {
                    errors.Add($"Parameter {key} exceeds maximum length of {_options.MaxParameterLength}");
                }
            }
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    /// <summary>
    /// Validates request headers.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The validation result.</returns>
    private ValidationResult ValidateHeaders(HttpContext context)
    {
        var errors = new List<string>();

        foreach (var header in context.Request.Headers)
        {
            var key = header.Key;
            var values = header.Value;

            // Skip standard headers that are handled by the framework
            if (IsStandardHeader(key))
                continue;

            // Validate header name
            if (!IsValidHeaderName(key))
            {
                errors.Add($"Invalid header name: {key}");
                _logger.LogWarning("Invalid header name detected: {HeaderName}", key);
                continue;
            }

            // Validate header values
            foreach (var value in values)
            {
                if (value == null)
                    continue;

                if (ContainsMaliciousContent(value))
                {
                    errors.Add($"Malicious content detected in header: {key}");
                    _logger.LogWarning(
                        "Potentially malicious content detected in header {HeaderName}: {Value}",
                        key,
                        value
                    );
                }

                if (value.Length > _options.MaxHeaderLength)
                {
                    errors.Add($"Header {key} exceeds maximum length of {_options.MaxHeaderLength}");
                }
            }
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    /// <summary>
    /// Validates the request body.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The validation result.</returns>
    private async Task<ValidationResult> ValidateRequestBodyAsync(HttpContext context)
    {
        var errors = new List<string>();
        var request = context.Request;

        // Check content length
        if (request.ContentLength > _options.MaxBodySize)
        {
            errors.Add($"Request body exceeds maximum size of {_options.MaxBodySize} bytes");
            return new ValidationResult { IsValid = false, Errors = errors };
        }

        // Read and validate body content
        if (ShouldValidateBodyContent(context))
        {
            request.EnableBuffering();

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            // Validate JSON structure if it's JSON content
            if (IsJsonContent(context))
            {
                var jsonValidation = ValidateJsonContent(body);
                if (!jsonValidation.IsValid)
                {
                    errors.AddRange(jsonValidation.Errors);
                }
            }

            // Check for malicious content
            if (ContainsMaliciousContent(body))
            {
                errors.Add("Malicious content detected in request body");
                _logger.LogWarning("Potentially malicious content detected in request body");
            }

            // Validate against SQL injection patterns
            if (ContainsSqlInjectionPatterns(body))
            {
                errors.Add("Potential SQL injection detected in request body");
                _logger.LogWarning("Potential SQL injection patterns detected in request body");
            }
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    /// <summary>
    /// Determines if the body content should be validated.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if the body should be validated; otherwise, false.</returns>
    private bool ShouldValidateBodyContent(HttpContext context)
    {
        var contentType = context.Request.ContentType?.ToLowerInvariant();

        return contentType != null
            && (
                contentType.Contains("application/json")
                || contentType.Contains("application/xml")
                || contentType.Contains("text/")
                || contentType.Contains("application/x-www-form-urlencoded")
            );
    }

    /// <summary>
    /// Determines if the content is JSON.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if the content is JSON; otherwise, false.</returns>
    private bool IsJsonContent(HttpContext context)
    {
        var contentType = context.Request.ContentType?.ToLowerInvariant();
        return contentType?.Contains("application/json") == true;
    }

    /// <summary>
    /// Validates JSON content structure.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <returns>The validation result.</returns>
    private ValidationResult ValidateJsonContent(string jsonContent)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new ValidationResult { IsValid = true, Errors = errors };
        }

        try
        {
            // Try to parse JSON to validate structure
            using var document = JsonDocument.Parse(jsonContent);

            // Additional JSON-specific validations can be added here
            ValidateJsonElement(document.RootElement, "", errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    /// <summary>
    /// Recursively validates JSON elements.
    /// </summary>
    /// <param name="element">The JSON element to validate.</param>
    /// <param name="path">The current path in the JSON structure.</param>
    /// <param name="errors">The list of errors to add to.</param>
    private void ValidateJsonElement(JsonElement element, string path, List<string> errors)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

                    // Validate property name
                    if (!IsValidPropertyName(property.Name))
                    {
                        errors.Add($"Invalid property name at {propertyPath}");
                    }

                    ValidateJsonElement(property.Value, propertyPath, errors);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var itemPath = $"{path}[{index}]";
                    ValidateJsonElement(item, itemPath, errors);
                    index++;
                }
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                if (stringValue != null)
                {
                    if (ContainsMaliciousContent(stringValue))
                    {
                        errors.Add($"Malicious content detected at {path}");
                    }

                    if (stringValue.Length > _options.MaxStringLength)
                    {
                        errors.Add($"String at {path} exceeds maximum length of {_options.MaxStringLength}");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Checks if a parameter name is valid.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private bool IsValidParameterName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_-]*$");
    }

    /// <summary>
    /// Checks if a header name is valid.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private bool IsValidHeaderName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_-]*$");
    }

    /// <summary>
    /// Checks if a property name is valid.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private bool IsValidPropertyName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z_$][a-zA-Z0-9_$]*$");
    }

    /// <summary>
    /// Checks if a header is a standard HTTP header.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <returns>True if it's a standard header; otherwise, false.</returns>
    private bool IsStandardHeader(string headerName)
    {
        var standardHeaders = new[]
        {
            "accept",
            "authorization",
            "content-type",
            "content-length",
            "host",
            "user-agent",
            "referer",
            "origin",
            "x-forwarded-for",
            "x-real-ip",
            "connection",
            "cache-control",
        };

        return standardHeaders.Contains(headerName.ToLowerInvariant());
    }

    /// <summary>
    /// Sanitizes a string by removing potentially dangerous characters.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The sanitized string.</returns>
    private string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove common XSS patterns
        var sanitized = input;
        sanitized = Regex.Replace(sanitized, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"vbscript:", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"onload=", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"onerror=", "", RegexOptions.IgnoreCase);

        return sanitized;
    }

    /// <summary>
    /// Checks if content contains malicious patterns.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if malicious content is detected; otherwise, false.</returns>
    private bool ContainsMaliciousContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        var maliciousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"onmouseover\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>",
            @"eval\s*\(",
            @"expression\s*\(",
        };

        return maliciousPatterns.Any(pattern => Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Checks if content contains SQL injection patterns.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if SQL injection patterns are detected; otherwise, false.</returns>
    private bool ContainsSqlInjectionPatterns(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        var sqlPatterns = new[]
        {
            @"(\s|\+|%20)*(union|select|insert|update|delete|drop|create|alter|exec|execute)\s+",
            @"(\s|\+|%20)*('|""|`)\s*(or|and)\s*('|""|`|\d+|true|false)\s*('|""|`)",
            @"(\s|\+|%20)*('|""|`)\s*;\s*(drop|delete|update|insert)",
            @"(\s|\+|%20)*(or|and)\s+('|""|`)*1\s*=\s*1",
            @"(\s|\+|%20)*(or|and)\s+('|""|`)*\d+\s*=\s*\d+",
            @"--\s*$",
            @"/\*.*?\*/",
        };

        return sqlPatterns.Any(pattern => Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Returns a validation error response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="validationResult">The validation result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ReturnValidationError(HttpContext context, ValidationResult validationResult)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "validation_failed",
            message = "Input validation failed",
            details = validationResult.Errors,
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Configuration options for input validation middleware.
/// </summary>
public class InputValidationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether validation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate query parameters.
    /// </summary>
    public bool ValidateQueryParameters { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate headers.
    /// </summary>
    public bool ValidateHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate request bodies.
    /// </summary>
    public bool ValidateRequestBody { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum parameter length.
    /// </summary>
    public int MaxParameterLength { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum header length.
    /// </summary>
    public int MaxHeaderLength { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum body size in bytes.
    /// </summary>
    public int MaxBodySize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Gets or sets the maximum string length in JSON.
    /// </summary>
    public int MaxStringLength { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the paths to exclude from validation.
    /// </summary>
    public IList<string> ExcludedPaths { get; set; } = new List<string> { "/swagger", "/health", "/metrics" };

    /// <summary>
    /// Creates default validation options.
    /// </summary>
    /// <returns>Default validation options.</returns>
    public static InputValidationOptions CreateDefault()
    {
        return new InputValidationOptions
        {
            Enabled = true,
            ValidateQueryParameters = true,
            ValidateHeaders = true,
            ValidateRequestBody = true,
            MaxParameterLength = 1000,
            MaxHeaderLength = 2000,
            MaxBodySize = 1024 * 1024,
            MaxStringLength = 10000,
            ExcludedPaths = new List<string> { "/swagger", "/health", "/metrics" },
        };
    }
}
