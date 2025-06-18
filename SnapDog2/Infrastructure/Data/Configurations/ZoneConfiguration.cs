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
            t =>
            {
                t.HasCheckConstraint("CK_Zones_DefaultVolume", "DefaultVolume >= 0 AND DefaultVolume <= 100");
                t.HasCheckConstraint("CK_Zones_MinVolume", "MinVolume >= 0 AND MinVolume <= 100");
                t.HasCheckConstraint("CK_Zones_MaxVolume", "MaxVolume >= 0 AND MaxVolume <= 100");
                t.HasCheckConstraint("CK_Zones_Priority", "Priority > 0");
            }
        );

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        // ClientIds collection conversion to JSON
        builder
            .Property(x => x.ClientIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    (
                        JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                    ).ToImmutableList()
            )
            .HasColumnName("ClientIds")
            .HasColumnType("TEXT");

        // Optional properties
        builder.Property(x => x.CurrentStreamId).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Location).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Color).HasMaxLength(7).IsRequired().HasDefaultValue("#007bff");

        builder.Property(x => x.Icon).HasMaxLength(50).IsRequired().HasDefaultValue("speaker");

        // Volume settings
        builder.Property(x => x.DefaultVolume).IsRequired().HasDefaultValue(50);

        builder.Property(x => x.MaxVolume).IsRequired().HasDefaultValue(100);

        builder.Property(x => x.MinVolume).IsRequired().HasDefaultValue(0);

        // Boolean properties
        builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.StereoEnabled).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.GroupingEnabled).IsRequired().HasDefaultValue(true);

        // Other properties
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(1);

        builder.Property(x => x.Tags).HasMaxLength(500).IsRequired(false);

        builder.Property(x => x.AudioQuality).HasMaxLength(50).IsRequired().HasDefaultValue("high");

        // Timestamp properties
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_Zones_Name");

        builder.HasIndex(x => x.IsEnabled).HasDatabaseName("IX_Zones_IsEnabled");

        builder.HasIndex(x => x.Priority).HasDatabaseName("IX_Zones_Priority");

        builder.HasIndex(x => x.CurrentStreamId).HasDatabaseName("IX_Zones_CurrentStreamId");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Zones_CreatedAt");
    }
}
