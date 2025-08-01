using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Zone entity.
/// Configures table mapping, constraints, indexes, and collection conversions.
/// </summary>
public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    /// <summary>
    /// Configures the Zone entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        // Table configuration with check constraints
        builder.ToTable(
            "Zones",
            static t =>
            {
                t.HasCheckConstraint("CK_Zones_DefaultVolume", "DefaultVolume >= 0 AND DefaultVolume <= 100");
                t.HasCheckConstraint("CK_Zones_MinVolume", "MinVolume >= 0 AND MinVolume <= 100");
                t.HasCheckConstraint("CK_Zones_MaxVolume", "MaxVolume >= 0 AND MaxVolume <= 100");
                t.HasCheckConstraint("CK_Zones_Priority", "Priority > 0");
            }
        );

        // Primary key
        builder.HasKey(static x => x.Id);
        builder.Property(static x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();

        // ClientIds collection conversion to JSON
        builder
            .Property(static x => x.ClientIds)
            .HasConversion(
                static v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                static v =>
                    (
                        JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                    ).ToImmutableList()
            )
            .HasColumnName("ClientIds")
            .HasColumnType("TEXT");

        // Optional properties
        builder.Property(static x => x.CurrentStreamId).HasMaxLength(100).IsRequired(false);

        builder.Property(static x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(static x => x.Location).HasMaxLength(200).IsRequired(false);

        builder.Property(static x => x.Color).HasMaxLength(7).IsRequired().HasDefaultValue("#007bff");

        builder.Property(static x => x.Icon).HasMaxLength(50).IsRequired().HasDefaultValue("speaker");

        // Volume settings
        builder.Property(static x => x.DefaultVolume).IsRequired().HasDefaultValue(50);

        builder.Property(static x => x.MaxVolume).IsRequired().HasDefaultValue(100);

        builder.Property(static x => x.MinVolume).IsRequired().HasDefaultValue(0);

        // Boolean properties
        builder.Property(static x => x.IsEnabled).IsRequired().HasDefaultValue(true);

        builder.Property(static x => x.StereoEnabled).IsRequired().HasDefaultValue(true);

        builder.Property(static x => x.GroupingEnabled).IsRequired().HasDefaultValue(true);

        // Other properties
        builder.Property(static x => x.Priority).IsRequired().HasDefaultValue(1);

        builder.Property(static x => x.Tags).HasMaxLength(500).IsRequired(false);

        builder.Property(static x => x.AudioQuality).HasMaxLength(50).IsRequired().HasDefaultValue("high");

        // Timestamp properties
        builder.Property(static x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(static x => x.UpdatedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(static x => x.Name).HasDatabaseName("IX_Zones_Name");

        builder.HasIndex(static x => x.IsEnabled).HasDatabaseName("IX_Zones_IsEnabled");

        builder.HasIndex(static x => x.Priority).HasDatabaseName("IX_Zones_Priority");

        builder.HasIndex(static x => x.CurrentStreamId).HasDatabaseName("IX_Zones_CurrentStreamId");

        builder.HasIndex(static x => x.CreatedAt).HasDatabaseName("IX_Zones_CreatedAt");
    }
}
