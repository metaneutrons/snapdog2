using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Playlist entity.
/// Configures table mapping, constraints, indexes, and collection conversions.
/// </summary>
public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    /// <summary>
    /// Configures the Playlist entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        // Table configuration with check constraints
        builder.ToTable(
            "Playlists",
            t =>
            {
                t.HasCheckConstraint("CK_Playlists_PlayCount", "PlayCount >= 0");
                t.HasCheckConstraint(
                    "CK_Playlists_TotalDurationSeconds",
                    "TotalDurationSeconds IS NULL OR TotalDurationSeconds >= 0"
                );
            }
        );

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        // TrackIds collection conversion to JSON
        builder
            .Property(x => x.TrackIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    (
                        JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                    ).ToImmutableList()
            )
            .HasColumnName("TrackIds")
            .HasColumnType("TEXT");

        // Optional properties
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Owner).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Tags).HasMaxLength(500).IsRequired(false);

        builder.Property(x => x.CoverArtPath).HasMaxLength(500).IsRequired(false);

        // Boolean properties
        builder.Property(x => x.IsPublic).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);

        // Numeric properties
        builder.Property(x => x.TotalDurationSeconds).IsRequired(false);

        builder.Property(x => x.PlayCount).IsRequired().HasDefaultValue(0);

        // Timestamp properties
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.Property(x => x.LastPlayedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_Playlists_Name");

        builder.HasIndex(x => x.Owner).HasDatabaseName("IX_Playlists_Owner");

        builder.HasIndex(x => x.IsPublic).HasDatabaseName("IX_Playlists_IsPublic");

        builder.HasIndex(x => x.IsSystem).HasDatabaseName("IX_Playlists_IsSystem");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Playlists_CreatedAt");

        builder.HasIndex(x => x.LastPlayedAt).HasDatabaseName("IX_Playlists_LastPlayedAt");

        builder.HasIndex(x => x.PlayCount).HasDatabaseName("IX_Playlists_PlayCount");
    }
}
