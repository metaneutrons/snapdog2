# Phase 2: Infrastructure & External Services

## Overview

Phase 2 implements the infrastructure layer with repository patterns, external service abstractions, and fault tolerance mechanisms. This phase establishes robust foundations for data persistence and external system integration.

**Deliverable**: Service-integrated application with external system abstractions and fault tolerance.

## Objectives

### Primary Goals

- [ ] Implement repository pattern with proper abstractions
- [ ] Create external service client interfaces and implementations
- [ ] Integrate Polly for comprehensive fault tolerance
- [ ] Setup database integration with Entity Framework Core
- [ ] Implement caching strategies for performance
- [ ] Create health check infrastructure

### Success Criteria

- All repositories implemented with clean abstractions
- External service clients with comprehensive error handling
- Fault tolerance policies applied to all external calls
- Database operations working with migrations
- Health checks provide system status visibility
- 90%+ test coverage for infrastructure components

## Prerequisites

### Dependencies

- Phase 1 successfully completed with domain foundation
- Database server available (SQL Server, PostgreSQL, or SQLite for development)
- Understanding of repository pattern and dependency injection
- Knowledge of Polly resilience patterns

### Knowledge Requirements

- Entity Framework Core migrations and DbContext
- Polly policies (retry, circuit breaker, timeout)
- HTTP client patterns and dependency injection
- Health check implementations

## Implementation Steps

### Step 1: Repository Pattern Implementation

#### 1.1 Core Repository Abstractions

```csharp
namespace SnapDog.Core.Abstractions;

/// <summary>
/// Base repository interface for common CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The entity identifier type</typeparam>
public interface IRepository<TEntity, TId> where TEntity : class
{
    /// <summary>Gets entity by ID</summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Gets all entities</summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new entity</summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing entity</summary>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entity by ID</summary>
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Checks if entity exists</summary>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized repository for audio streams with domain-specific operations.
/// </summary>
public interface IAudioStreamRepository : IRepository<AudioStream, int>
{
    /// <summary>Gets all active streams</summary>
    Task<IEnumerable<AudioStream>> GetActiveStreamsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets streams by codec type</summary>
    Task<IEnumerable<AudioStream>> GetStreamsByCodecAsync(AudioCodec codec, CancellationToken cancellationToken = default);

    /// <summary>Gets streams with specific sample rate</summary>
    Task<IEnumerable<AudioStream>> GetStreamsBySampleRateAsync(int sampleRate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for client management with connection tracking.
/// </summary>
public interface IClientRepository : IRepository<Client, int>
{
    /// <summary>Gets client by MAC address</summary>
    Task<Client?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default);

    /// <summary>Gets all connected clients</summary>
    Task<IEnumerable<Client>> GetConnectedClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets clients in specific zone</summary>
    Task<IEnumerable<Client>> GetClientsByZoneAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>Updates client connection status</summary>
    Task UpdateConnectionStatusAsync(int clientId, ClientStatus status, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for zone management and client assignments.
/// </summary>
public interface IZoneRepository : IRepository<Zone, int>
{
    /// <summary>Gets zone with all assigned clients</summary>
    Task<Zone?> GetZoneWithClientsAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>Gets zones by name pattern</summary>
    Task<IEnumerable<Zone>> GetZonesByNameAsync(string namePattern, CancellationToken cancellationToken = default);

    /// <summary>Updates zone volume</summary>
    Task UpdateZoneVolumeAsync(int zoneId, int volume, CancellationToken cancellationToken = default);
}
```

#### 1.2 Database Context Implementation

```csharp
namespace SnapDog.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for SnapDog data persistence.
/// </summary>
public class SnapDogDbContext : DbContext
{
    public SnapDogDbContext(DbContextOptions<SnapDogDbContext> options) : base(options)
    {
    }

    public DbSet<AudioStream> AudioStreams { get; set; } = null!;
    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Zone> Zones { get; set; } = null!;
    public DbSet<Playlist> Playlists { get; set; } = null!;
    public DbSet<RadioStation> RadioStations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AudioStream configuration
        modelBuilder.Entity<AudioStream>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Codec)
                .HasConversion<string>()
                .IsRequired();
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();
            entity.Property(e => e.SampleRate)
                .IsRequired();
            entity.Property(e => e.BitDepth)
                .IsRequired();
            entity.Property(e => e.Channels)
                .IsRequired();
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            entity.Property(e => e.SnapcastSinkName)
                .HasMaxLength(50);

            // Indexes
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Codec);
        });

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.MacAddress)
                .IsRequired()
                .HasMaxLength(17);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();
            entity.Property(e => e.Volume)
                .HasDefaultValue(50);
            entity.Property(e => e.Muted)
                .HasDefaultValue(false);
            entity.Property(e => e.LastSeen)
                .IsRequired();

            // Unique constraints
            entity.HasIndex(e => e.MacAddress)
                .IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // Zone configuration
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Volume)
                .HasDefaultValue(50);
            entity.Property(e => e.Muted)
                .HasDefaultValue(false);

            // Relationships
            entity.HasMany<Client>()
                .WithOne()
                .HasForeignKey("ZoneId")
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Name);
        });

        // Playlist configuration
        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            entity.Property(e => e.IsRadioPlaylist)
                .HasDefaultValue(false);

            entity.HasIndex(e => e.Name);
        });

        // RadioStation configuration
        modelBuilder.Entity<RadioStation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Url)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Name);
        });
    }
}
```

#### 1.3 Repository Implementations

```csharp
namespace SnapDog.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation with common CRUD operations.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TId">Entity ID type</typeparam>
public abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
{
    protected readonly SnapDogDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly ILogger<Repository<TEntity, TId>> _logger;

    protected Repository(SnapDogDbContext context, ILogger<Repository<TEntity, TId>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {EntityType} by ID: {Id}", typeof(TEntity).Name, id);

        try
        {
            var entity = await _dbSet.FindAsync(new object[] { id! }, cancellationToken);

            if (entity == null)
            {
                _logger.LogDebug("{EntityType} with ID {Id} not found", typeof(TEntity).Name, id);
            }

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all {EntityType} entities", typeof(TEntity).Name);

        try
        {
            var entities = await _dbSet.ToListAsync(cancellationToken);
            _logger.LogDebug("Retrieved {Count} {EntityType} entities", entities.Count, typeof(TEntity).Name);
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _logger.LogDebug("Creating new {EntityType} entity", typeof(TEntity).Name);

        try
        {
            var entry = await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created {EntityType} entity successfully", typeof(TEntity).Name);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityType} entity", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _logger.LogDebug("Updating {EntityType} entity", typeof(TEntity).Name);

        try
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated {EntityType} entity successfully", typeof(TEntity).Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType} entity", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting {EntityType} by ID: {Id}", typeof(TEntity).Name, id);

        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} with ID {Id} not found for deletion", typeof(TEntity).Name, id);
                return false;
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {EntityType} with ID {Id} successfully", typeof(TEntity).Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of {EntityType} by ID: {Id}", typeof(TEntity).Name, id);

        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            return entity != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }
}

/// <summary>
/// Audio stream repository with domain-specific queries.
/// </summary>
public class AudioStreamRepository : Repository<AudioStream, int>, IAudioStreamRepository
{
    public AudioStreamRepository(SnapDogDbContext context, ILogger<AudioStreamRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<AudioStream>> GetActiveStreamsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all active audio streams");

        try
        {
            var activeStreams = await _dbSet
                .Where(s => s.Status == StreamStatus.Active)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} active streams", activeStreams.Count);
            return activeStreams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active audio streams");
            throw;
        }
    }

    public async Task<IEnumerable<AudioStream>> GetStreamsByCodecAsync(AudioCodec codec, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting audio streams by codec: {Codec}", codec);

        try
        {
            var streams = await _dbSet
                .Where(s => s.Codec == codec)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} streams with codec {Codec}", streams.Count, codec);
            return streams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streams by codec: {Codec}", codec);
            throw;
        }
    }

    public async Task<IEnumerable<AudioStream>> GetStreamsBySampleRateAsync(int sampleRate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting audio streams by sample rate: {SampleRate}", sampleRate);

        try
        {
            var streams = await _dbSet
                .Where(s => s.SampleRate == sampleRate)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} streams with sample rate {SampleRate}", streams.Count, sampleRate);
            return streams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streams by sample rate: {SampleRate}", sampleRate);
            throw;
        }
    }
}
```

### Step 2: External Service Abstractions

#### 2.1 Snapcast Service Interface

```csharp
namespace SnapDog.Core.Abstractions;

/// <summary>
/// Interface for Snapcast server communication.
/// </summary>
public interface ISnapcastService
{
    /// <summary>Gets current server status</summary>
    Task<Result<ServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets all server groups</summary>
    Task<Result<IEnumerable<SnapcastGroup>>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets all server clients</summary>
    Task<Result<IEnumerable<SnapcastClient>>> GetClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets client volume</summary>
    Task<Result> SetClientVolumeAsync(string clientId, int volume, CancellationToken cancellationToken = default);

    /// <summary>Sets client mute status</summary>
    Task<Result> SetClientMuteAsync(string clientId, bool muted, CancellationToken cancellationToken = default);

    /// <summary>Moves client to different group</summary>
    Task<Result> MoveClientToGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default);

    /// <summary>Sets group volume</summary>
    Task<Result> SetGroupVolumeAsync(string groupId, int volume, CancellationToken cancellationToken = default);

    /// <summary>Creates new group</summary>
    Task<Result<SnapcastGroup>> CreateGroupAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>Deletes group</summary>
    Task<Result> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for MQTT communication.
/// </summary>
public interface IMqttService
{
    /// <summary>Connects to MQTT broker</summary>
    Task<Result> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from MQTT broker</summary>
    Task<Result> DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Publishes message to topic</summary>
    Task<Result> PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default);

    /// <summary>Subscribes to topic pattern</summary>
    Task<Result> SubscribeAsync(string topicPattern, Func<string, string, Task> messageHandler, CancellationToken cancellationToken = default);

    /// <summary>Unsubscribes from topic pattern</summary>
    Task<Result> UnsubscribeAsync(string topicPattern, CancellationToken cancellationToken = default);

    /// <summary>Gets connection status</summary>
    bool IsConnected { get; }
}

/// <summary>
/// Interface for KNX communication.
/// </summary>
public interface IKnxService
{
    /// <summary>Connects to KNX gateway</summary>
    Task<Result> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from KNX gateway</summary>
    Task<Result> DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads value from group address</summary>
    Task<Result<byte[]>> ReadGroupValueAsync(string groupAddress, CancellationToken cancellationToken = default);

    /// <summary>Writes value to group address</summary>
    Task<Result> WriteGroupValueAsync(string groupAddress, byte[] value, CancellationToken cancellationToken = default);

    /// <summary>Subscribes to group address changes</summary>
    Task<Result> SubscribeToGroupAsync(string groupAddress, Func<string, byte[], Task> valueHandler, CancellationToken cancellationToken = default);

    /// <summary>Gets connection status</summary>
    bool IsConnected { get; }
}
```

#### 2.2 HTTP Service Implementation with Polly

```csharp
namespace SnapDog.Infrastructure.Services;

/// <summary>
/// Base HTTP service with Polly resilience policies.
/// </summary>
public abstract class HttpServiceBase
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    protected readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;
    protected readonly IAsyncPolicy<HttpResponseMessage> _timeoutPolicy;
    protected readonly IAsyncPolicy<HttpResponseMessage> _combinedPolicy;

    protected HttpServiceBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure Polly policies
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry attempt {RetryCount} after {Delay}ms for {Context}",
                        retryCount, timeSpan.TotalMilliseconds, context.GetType().Name);
                });

        _circuitBreakerPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("Circuit breaker opened for {Duration}s due to: {Exception}",
                        duration.TotalSeconds, exception.Exception?.Message ?? exception.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset - service is healthy again");
                });

        _timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(30),
            timeoutStrategy: TimeoutStrategy.Optimistic);

        // Combine policies: Timeout -> CircuitBreaker -> Retry
        _combinedPolicy = Policy.WrapAsync(_timeoutPolicy, _circuitBreakerPolicy, _retryPolicy);
    }

    protected async Task<Result<T>> ExecuteWithPolicyAsync<T>(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        Func<HttpResponseMessage, Task<T>> responseParser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _combinedPolicy.ExecuteAsync(async (ct) =>
            {
                return await operation(ct);
            }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await responseParser(response);
                return Result<T>.Success(result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("HTTP request failed with status {StatusCode}: {Content}",
                    response.StatusCode, errorContent);
                return Result<T>.Failure($"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open - service unavailable");
            return Result<T>.Failure("Service temporarily unavailable - circuit breaker open");
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Request timeout");
            return Result<T>.Failure("Request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing HTTP request");
            return Result<T>.Failure($"Unexpected error: {ex.Message}");
        }
    }
}

/// <summary>
/// Snapcast service implementation using Sturd.SnapcastNet.
/// </summary>
public class SnapcastService : ISnapcastService
{
    private readonly ISnapcastClient _snapcastClient;
    private readonly ILogger<SnapcastService> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public SnapcastService(ISnapcastClient snapcastClient, ILogger<SnapcastService> logger)
    {
        _snapcastClient = snapcastClient ?? throw new ArgumentNullException(nameof(snapcastClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _retryPolicy = Policy
            .Handle<SnapcastException>()
            .Or<SocketException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Snapcast operation retry {RetryCount} after {Delay}ms",
                        retryCount, timeSpan.TotalMilliseconds);
                });
    }

    public async Task<Result<ServerStatus>> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting Snapcast server status");

        try
        {
            var status = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _snapcastClient.GetServerStatusAsync(cancellationToken);
            });

            _logger.LogDebug("Retrieved server status: Version {Version}, Connected: {Connected}",
                status.Version, status.Connected);

            return Result<ServerStatus>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Snapcast server status");
            return Result<ServerStatus>.Failure($"Failed to get server status: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<SnapcastGroup>>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting Snapcast groups");

        try
        {
            var groups = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _snapcastClient.GetGroupsAsync(cancellationToken);
            });

            _logger.LogDebug("Retrieved {GroupCount} groups", groups.Count());
            return Result<IEnumerable<SnapcastGroup>>.Success(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Snapcast groups");
            return Result<IEnumerable<SnapcastGroup>>.Failure($"Failed to get groups: {ex.Message}");
        }
    }

    public async Task<Result> SetClientVolumeAsync(string clientId, int volume, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return Result.Failure("Client ID cannot be empty");

        if (volume < 0 || volume > 100)
            return Result.Failure("Volume must be between 0 and 100");

        _logger.LogDebug("Setting client {ClientId} volume to {Volume}", clientId, volume);

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _snapcastClient.SetClientVolumeAsync(clientId, volume, cancellationToken);
            });

            _logger.LogInformation("Successfully set client {ClientId} volume to {Volume}", clientId, volume);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set client {ClientId} volume to {Volume}", clientId, volume);
            return Result.Failure($"Failed to set client volume: {ex.Message}");
        }
    }

    // Additional method implementations...
}
```

### Step 3: Fault Tolerance Implementation

#### 3.1 Polly Policy Configuration

```csharp
namespace SnapDog.Infrastructure.Resilience;

/// <summary>
/// Configuration for Polly resilience policies.
/// </summary>
public class ResiliencePolicyOptions
{
    public RetryPolicyOptions Retry { get; set; } = new();
    public CircuitBreakerPolicyOptions CircuitBreaker { get; set; } = new();
    public TimeoutPolicyOptions Timeout { get; set; } = new();
}

public class RetryPolicyOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool UseExponentialBackoff { get; set; } = true;
}

public class CircuitBreakerPolicyOptions
{
    public int FailureThreshold { get; set; } = 3;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
    public int MinimumThroughput { get; set; } = 10;
}

public class TimeoutPolicyOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan DatabaseTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Factory for creating Polly policies.
/// </summary>
public class PolicyFactory
{
    private readonly ResiliencePolicyOptions _options;
    private readonly ILogger<PolicyFactory> _logger;

    public PolicyFactory(ResiliencePolicyOptions options, ILogger<PolicyFactory> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IAsyncPolicy CreateRetryPolicy(string operationName)
    {
        return Policy
            .Handle<Exception>(ex => !(ex is ArgumentException || ex is InvalidOperationException))
            .WaitAndRetryAsync(
                retryCount: _options.Retry.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                {
                    if (_options.Retry.UseExponentialBackoff)
                    {
                        return TimeSpan.FromMilliseconds(
                            _options.Retry.BaseDelay.TotalMilliseconds * Math.Pow(2, retryAttempt));
                    }
                    return _options.Retry.BaseDelay;
                },
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry attempt {RetryCount} for {OperationName} after {Delay}ms. Error: {Error}",
                        retryCount, operationName, timeSpan.TotalMilliseconds, outcome.Message);
                });
    }

    public IAsyncPolicy CreateCircuitBreakerPolicy(string serviceName)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _options.CircuitBreaker.FailureThreshold,
                durationOfBreak: _options.CircuitBreaker.BreakDuration,
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(exception, "Circuit breaker opened for {ServiceName} for {Duration}s",
                        serviceName, duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset for {ServiceName} - service is healthy",
                        serviceName);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open for {ServiceName} - testing service health",
                        serviceName);
                });
    }

    public IAsyncPolicy CreateTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync(
            timeout: timeout,
            timeoutStrategy: TimeoutStrategy.Optimistic,
            onTimeout: (context, timespan, task) =>
            {
                _logger.LogWarning("Operation timeout after {Timeout}s for context: {Context}",
                    timespan.TotalSeconds, context.GetType().Name);
                return Task.CompletedTask;
            });
    }

    public IAsyncPolicy CreateCombinedPolicy(string operationName, TimeSpan? timeout = null)
    {
        var timeoutPolicy = CreateTimeoutPolicy(timeout ?? _options.Timeout.DefaultTimeout);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy(operationName);
        var retryPolicy = CreateRetryPolicy(operationName);

        // Wrap policies: Timeout -> CircuitBreaker -> Retry
        return Policy.WrapAsync(timeoutPolicy, circuitBreakerPolicy, retryPolicy);
    }
}
```

### Step 4: Health Checks Implementation

#### 4.1 Health Check Services

```csharp
namespace SnapDog.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Snapcast server connectivity.
/// </summary>
public class SnapcastHealthCheck : IHealthCheck
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<SnapcastHealthCheck> _logger;

    public SnapcastHealthCheck(ISnapcastService snapcastService, ILogger<SnapcastHealthCheck> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing Snapcast health check");

            var statusResult = await _snapcastService.GetServerStatusAsync(cancellationToken);

            if (statusResult.IsSuccess)
            {
                var status = statusResult.Value;
                var data = new Dictionary<string, object>
                {
                    ["version"] = status.Version ?? "unknown",
                    ["connected"] = status.Connected,
                    ["checkTime"] = DateTime.UtcNow
                };

                if (status.Connected)
                {
                    _logger.LogDebug("Snapcast health check passed - server connected");
                    return HealthCheckResult.Healthy("Snapcast server is connected and responsive", data);
                }
                else
                {
                    _logger.LogWarning("Snapcast health check degraded - server not connected");
                    return HealthCheckResult.Degraded("Snapcast server is not connected", null, data);
                }
            }
            else
            {
                _logger.LogError("Snapcast health check failed: {Error}", statusResult.Error);
                return HealthCheckResult.Unhealthy($"Failed to get Snapcast server status: {statusResult.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Snapcast health check");
            return HealthCheckResult.Unhealthy($"Exception during health check: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Health check for database connectivity.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly SnapDogDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(SnapDogDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing database health check");

            var stopwatch = Stopwatch.StartNew();

            // Simple connectivity test
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // Performance test - should complete quickly
            var streamCount = await _dbContext.AudioStreams.CountAsync(cancellationToken);

            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["streamCount"] = streamCount,
                ["responseTimeMs"] = stopwatch.ElapsedMilliseconds,
                ["checkTime"] = DateTime.UtcNow
            };

            if (stopwatch.ElapsedMilliseconds < 5000) // 5 second threshold
            {
                _logger.LogDebug("Database health check passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Healthy("Database is responsive", data);
            }
            else
            {
                _logger.LogWarning("Database health check slow: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Degraded("Database is slow to respond", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during database health check");
            return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Health check for MQTT broker connectivity.
/// </summary>
public class MqttHealthCheck : IHealthCheck
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<MqttHealthCheck> _logger;

    public MqttHealthCheck(IMqttService mqttService, ILogger<MqttHealthCheck> logger)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing MQTT health check");

            var data = new Dictionary<string, object>
            {
                ["connected"] = _mqttService.IsConnected,
                ["checkTime"] = DateTime.UtcNow
            };

            if (_mqttService.IsConnected)
            {
                // Test publish capability
                var testResult = await _mqttService.PublishAsync("SNAPDOG/HEALTH", "test", false, cancellationToken);

                if (testResult.IsSuccess)
                {
                    _logger.LogDebug("MQTT health check passed");
                    return HealthCheckResult.Healthy("MQTT broker is connected and responsive", data);
                }
                else
                {
                    _logger.LogWarning("MQTT health check degraded - publish failed: {Error}", testResult.Error);
                    return HealthCheckResult.Degraded($"MQTT connected but publish failed: {testResult.Error}", null, data);
                }
            }
            else
            {
                _logger.LogWarning("MQTT health check failed - not connected");
                return HealthCheckResult.Unhealthy("MQTT broker is not connected", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during MQTT health check");
            return HealthCheckResult.Unhealthy($"MQTT health check failed: {ex.Message}", ex);
        }
    }
}
```

### Step 5: Dependency Injection Configuration

#### 5.1 Infrastructure Service Registration

```csharp
namespace SnapDog.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        SnapDogConfiguration configuration)
    {
        // Database registration
        services.AddDbContext<SnapDogDbContext>(options =>
        {
            if (configuration.Environment == "Development")
            {
                options.UseInMemoryDatabase("SnapDogDev");
                options.EnableSensitiveDataLogging();
            }
            else
            {
                options.UseSqlServer(configuration.DatabaseConnectionString);
            }

            options.EnableDetailedErrors();
        });

        // Repository registration
        services.AddScoped<IAudioStreamRepository, AudioStreamRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();

        // External service registration
        services.AddHttpClient<ISnapcastService, SnapcastService>(client =>
        {
            client.BaseAddress = new Uri($"http://{configuration.SnapcastServerHost}:{configuration.SnapcastServerPort}");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Polly policies
        services.AddSingleton<ResiliencePolicyOptions>(provider =>
        {
            return new ResiliencePolicyOptions
            {
                Retry = new RetryPolicyOptions
                {
                    MaxRetryAttempts = 3,
                    BaseDelay = TimeSpan.FromSeconds(1),
                    UseExponentialBackoff = true
                },
                CircuitBreaker = new CircuitBreakerPolicyOptions
                {
                    FailureThreshold = 3,
                    BreakDuration = TimeSpan.FromSeconds(30)
                },
                Timeout = new TimeoutPolicyOptions
                {
                    DefaultTimeout = TimeSpan.FromSeconds(30),
                    DatabaseTimeout = TimeSpan.FromSeconds(10)
                }
            };
        });

        services.AddSingleton<PolicyFactory>();

        // Health checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<SnapcastHealthCheck>("snapcast")
            .AddCheck<MqttHealthCheck>("mqtt");

        // Caching
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            if (!string.IsNullOrEmpty(configuration.RedisConnectionString))
            {
                options.Configuration = configuration.RedisConnectionString;
            }
        });

        return services;
    }
}
```

## Expected Deliverable

### Working Console Application Output

```
[15:45:30 INF] Starting SnapDog Phase 2 - Infrastructure & External Services
[15:45:30 INF] Configuring database connection
[15:45:30 INF] Database connection established successfully
[15:45:30 INF] Running database migrations
[15:45:31 INF] Database migrations completed
[15:45:31 INF] Initializing external service connections
[15:45:31 INF] Snapcast service initialized - Server: localhost:1705
[15:45:31 INF] === Infrastructure Demonstration ===
[15:45:31 INF] Creating test audio stream
[15:45:31 INF] Audio stream created with ID: 1
[15:45:31 INF] Retrieving audio stream from database
[15:45:31 INF] Retrieved stream: Living Room (FLAC, 44100Hz)
[15:45:31 INF] Testing Snapcast connectivity
[15:45:32 INF] Snapcast server status: Version 0.26.0, Connected: True
[15:45:32 INF] Running health checks
[15:45:32 INF] Health check results:
[15:45:32 INF]   Database: Healthy (Response: 45ms)
[15:45:32 INF]   Snapcast: Healthy (Connected)
[15:45:32 INF]   MQTT: Degraded (Not configured)
[15:45:32 INF] === Phase 2 Infrastructure Complete ===
```

### Test Results

```
Phase 2 Test Results:
===================
Repository Tests: 45/45 passed
External Service Tests: 30/30 passed
Fault Tolerance Tests: 25/25 passed
Health Check Tests: 15/15 passed
Integration Tests: 20/20 passed

Total Tests: 135/135 passed
Code Coverage: 94%
```

## Quality Gates

### Code Quality Checklist

- [ ] All repositories implement proper abstractions
- [ ] External services include comprehensive error handling
- [ ] Polly policies configured for all external calls
- [ ] Database operations use proper async patterns
- [ ] Health checks provide meaningful status information
- [ ] 90%+ test coverage for infrastructure components

### Architecture Validation

- [ ] Infrastructure layer properly separated from domain
- [ ] Repository pattern correctly implemented
- [ ] External service abstractions follow interface segregation
- [ ] Fault tolerance policies appropriately configured
- [ ] Database context properly configured for environments

## Next Steps

Upon successful completion of Phase 2:

1. **Validate all infrastructure components** against success criteria
2. **Test external service integrations** in development environment
3. **Verify health check functionality** across all services
4. **Prepare for Phase 3** by reviewing MediatR and CQRS requirements
5. **Begin Phase 3** with solid infrastructure foundation

Phase 2 establishes the robust infrastructure foundation required for business logic and external system integration in subsequent phases.
