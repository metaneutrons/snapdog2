using Microsoft.EntityFrameworkCore;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Data.Configurations;

namespace SnapDog2.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the SnapDog2 application.
/// Provides access to all domain entities and their configurations.
/// </summary>
public class SnapDogDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapDogDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public SnapDogDbContext(DbContextOptions<SnapDogDbContext> options)
        : base(options) { }

    /// <summary>
    /// Gets or sets the audio streams DbSet.
    /// </summary>
    public DbSet<AudioStream> AudioStreams => Set<AudioStream>();

    /// <summary>
    /// Gets or sets the clients DbSet.
    /// </summary>
    public DbSet<Client> Clients => Set<Client>();

    /// <summary>
    /// Gets or sets the zones DbSet.
    /// </summary>
    public DbSet<Zone> Zones => Set<Zone>();

    /// <summary>
    /// Gets or sets the playlists DbSet.
    /// </summary>
    public DbSet<Playlist> Playlists => Set<Playlist>();

    /// <summary>
    /// Gets or sets the tracks DbSet.
    /// </summary>
    public DbSet<Track> Tracks => Set<Track>();

    /// <summary>
    /// Gets or sets the radio stations DbSet.
    /// </summary>
    public DbSet<RadioStation> RadioStations => Set<RadioStation>();

    /// <summary>
    /// Configures the model and entity relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new AudioStreamConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new ZoneConfiguration());
        modelBuilder.ApplyConfiguration(new PlaylistConfiguration());
        modelBuilder.ApplyConfiguration(new TrackConfiguration());
        modelBuilder.ApplyConfiguration(new RadioStationConfiguration());
    }
}
