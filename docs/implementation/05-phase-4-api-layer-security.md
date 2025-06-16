# Phase 4: API Layer & Security

## Overview

Phase 4 implements the complete REST API layer with authentication, authorization, and comprehensive OpenAPI documentation. This phase exposes the business logic through a secure, well-documented web API.

**Deliverable**: Full REST API with security, validation, and comprehensive OpenAPI documentation.

## Objectives

### Primary Goals

- [ ] Implement complete REST API controllers for all operations
- [ ] Add authentication and authorization with JWT
- [ ] Create comprehensive OpenAPI/Swagger documentation
- [ ] Implement API versioning and content negotiation
- [ ] Add rate limiting and security middleware
- [ ] Create API client SDKs

### Success Criteria

- All business operations exposed via REST API
- Authentication and authorization working
- OpenAPI documentation complete and accurate
- API integration tests passing
- Security vulnerabilities addressed
- Performance meets requirements

## Implementation Steps

### Step 1: API Controllers Implementation

#### 1.1 Audio Stream Controller

```csharp
namespace SnapDog.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StreamsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StreamsController> _logger;

    public StreamsController(IMediator mediator, ILogger<StreamsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all audio streams
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AudioStreamDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AudioStreamDto>>> GetStreams(CancellationToken cancellationToken)
    {
        var query = new GetAllAudioStreamsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            var dtos = result.Value.Select(AudioStreamDto.FromDomain);
            return Ok(dtos);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Creates a new audio stream
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AudioStreamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AudioStreamDto>> CreateStream(
        CreateStreamRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAudioStreamCommand(
            request.Name,
            request.Codec,
            request.SampleRate,
            request.BitDepth,
            request.Channels);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            var dto = AudioStreamDto.FromDomain(result.Value);
            return CreatedAtAction(nameof(GetStream), new { id = dto.Id }, dto);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Starts an audio stream
    /// </summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartStream(int id, CancellationToken cancellationToken)
    {
        var command = new StartAudioStreamCommand(id, User.Identity?.Name ?? "Anonymous");
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
```

#### 1.2 System Controller

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SystemController> _logger;

    /// <summary>
    /// Gets system health status
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SystemHealth), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealth>> GetHealth(CancellationToken cancellationToken)
    {
        var query = new GetSystemHealthQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    /// <summary>
    /// Gets system status information
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = "Admin,User")]
    [ProducesResponseType(typeof(SystemStatus), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatus>> GetStatus(CancellationToken cancellationToken)
    {
        var query = new GetSystemStatusQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

### Step 2: Authentication & Authorization

#### 2.1 JWT Authentication Setup

```csharp
namespace SnapDog.Api.Authentication;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtTokenService> _logger;

    public string GenerateToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### 2.2 Authorization Policies

```csharp
public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string UserOrAdmin = "UserOrAdmin";
    public const string SystemAccess = "SystemAccess";

    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(UserOrAdmin, policy =>
            policy.RequireRole("Admin", "User"));

        options.AddPolicy(SystemAccess, policy =>
            policy.RequireAuthenticatedUser());
    }
}
```

### Step 3: OpenAPI Documentation

#### 3.1 Swagger Configuration

```csharp
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SnapDog API",
                Version = "v1",
                Description = "Multi-room audio streaming system API",
                Contact = new OpenApiContact
                {
                    Name = "SnapDog Team",
                    Email = "support@snapdog.audio"
                }
            });

            // JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}
```

### Step 4: DTOs and Models

#### 4.1 Response Models

```csharp
namespace SnapDog.Api.Models;

/// <summary>
/// Audio stream data transfer object
/// </summary>
public record AudioStreamDto(
    int Id,
    string Name,
    string Codec,
    int SampleRate,
    int BitDepth,
    int Channels,
    string Status,
    DateTime CreatedAt,
    DateTime? LastStartedAt)
{
    public static AudioStreamDto FromDomain(AudioStream stream)
    {
        return new AudioStreamDto(
            stream.Id,
            stream.Name,
            stream.Codec.ToString(),
            stream.SampleRate,
            stream.BitDepth,
            stream.Channels,
            stream.Status.ToString(),
            stream.CreatedAt,
            stream.LastStartedAt);
    }
}

/// <summary>
/// API response wrapper
/// </summary>
public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    DateTime Timestamp)
{
    public static ApiResponse<T> Success(T data) =>
        new(true, data, null, DateTime.UtcNow);

    public static ApiResponse<T> Failure(string error) =>
        new(false, default, error, DateTime.UtcNow);
}
```

## Expected Deliverable

### Working REST API

```
SnapDog API v1.0
===============
🟢 GET    /api/v1/streams          - Get all streams
🟢 POST   /api/v1/streams          - Create stream
🟢 GET    /api/v1/streams/{id}     - Get stream
🟢 POST   /api/v1/streams/{id}/start - Start stream
🟢 POST   /api/v1/streams/{id}/stop  - Stop stream
🟢 DELETE /api/v1/streams/{id}     - Delete stream
🟢 GET    /api/v1/clients          - Get all clients
🟢 POST   /api/v1/clients/{id}/volume - Set volume
🟢 GET    /api/v1/system/health    - Health check
🟢 GET    /api/v1/system/status    - System status

Authentication: JWT Bearer Token
Documentation: /swagger
Health Check: /health
```

### Test Results

```
Phase 4 Test Results:
===================
API Controller Tests: 40/40 passed
Authentication Tests: 15/15 passed
Authorization Tests: 20/20 passed
Integration Tests: 25/25 passed
Security Tests: 12/12 passed

Total Tests: 112/112 passed
Code Coverage: 93%
Security Scan: ✅ No vulnerabilities
```

## Quality Gates

- [ ] All endpoints implemented and documented
- [ ] Authentication and authorization working
- [ ] OpenAPI documentation complete
- [ ] Security vulnerabilities addressed
- [ ] Performance requirements met
- [ ] Integration tests passing

## Next Steps

Phase 4 provides a complete, secure REST API ready for external integration. Proceed to Phase 5 for protocol integrations.
