using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the RadioStation entity.
/// Configures table mapping, constraints, indexes, and value object conversions.
/// </summary>
public class RadioStationConfiguration : IEntityTypeConfiguration<RadioStation>
{
    /// <summary>
    /// Configures the RadioStation entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<RadioStation> builder)
    {
        // Table configuration with check constraints
        builder.ToTable(
            "RadioStations",
            static t =>
            {
                t.HasCheckConstraint("CK_RadioStations_BitrateKbps", "BitrateKbps IS NULL OR BitrateKbps > 0");
                t.HasCheckConstraint("CK_RadioStations_SampleRateHz", "SampleRateHz IS NULL OR SampleRateHz > 0");
                t.HasCheckConstraint("CK_RadioStations_Channels", "Channels IS NULL OR Channels > 0");
                t.HasCheckConstraint("CK_RadioStations_Priority", "Priority > 0");
                t.HasCheckConstraint("CK_RadioStations_PlayCount", "PlayCount >= 0");
            }
        );

        // Primary key
        builder.HasKey(static x => x.Id);
        builder.Property(static x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();

        // StreamUrl value object conversion
        builder
            .Property(static x => x.Url)
            .HasConversion(static v => v.Value.AbsoluteUri, static v => new StreamUrl(v))
            .HasColumnName("Url")
            .HasMaxLength(2000)
            .IsRequired();

        // Enum conversions
        builder.Property(static x => x.Codec).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Optional metadata properties
        builder.Property(static x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(static x => x.Genre).HasMaxLength(100).IsRequired(false);

        builder.Property(static x => x.Country).HasMaxLength(100).IsRequired(false);

        builder.Property(static x => x.Language).HasMaxLength(50).IsRequired(false);

        builder.Property(static x => x.Website).HasMaxLength(500).IsRequired(false);

        builder.Property(static x => x.LogoUrl).HasMaxLength(500).IsRequired(false);

        builder.Property(static x => x.Tags).HasMaxLength(500).IsRequired(false);

        // Technical properties
        builder.Property(static x => x.BitrateKbps).IsRequired(false);

        builder.Property(static x => x.SampleRateHz).IsRequired(false);

        builder.Property(static x => x.Channels).IsRequired(false);

        // Boolean properties
        builder.Property(static x => x.IsEnabled).IsRequired().HasDefaultValue(true);

        builder.Property(static x => x.RequiresAuth).IsRequired().HasDefaultValue(false);

        builder.Property(static x => x.IsOnline).IsRequired(false);

        // Authentication properties
        builder.Property(static x => x.Username).HasMaxLength(100).IsRequired(false);

        builder.Property(static x => x.Password).HasMaxLength(200).IsRequired(false);

        // Other properties
        builder.Property(static x => x.Priority).IsRequired().HasDefaultValue(1);

        builder.Property(static x => x.PlayCount).IsRequired().HasDefaultValue(0);

        // Timestamp properties
        builder.Property(static x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(static x => x.UpdatedAt).IsRequired(false);

        builder.Property(static x => x.LastPlayedAt).IsRequired(false);

        builder.Property(static x => x.LastCheckedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(static x => x.Name).HasDatabaseName("IX_RadioStations_Name");

        builder.HasIndex(static x => x.Genre).HasDatabaseName("IX_RadioStations_Genre");

        builder.HasIndex(static x => x.Country).HasDatabaseName("IX_RadioStations_Country");

        builder.HasIndex(static x => x.Language).HasDatabaseName("IX_RadioStations_Language");

        builder.HasIndex(static x => x.Codec).HasDatabaseName("IX_RadioStations_Codec");

        builder.HasIndex(static x => x.IsEnabled).HasDatabaseName("IX_RadioStations_IsEnabled");

        builder.HasIndex(static x => x.IsOnline).HasDatabaseName("IX_RadioStations_IsOnline");

        builder.HasIndex(static x => x.Priority).HasDatabaseName("IX_RadioStations_Priority");

        builder.HasIndex(static x => x.CreatedAt).HasDatabaseName("IX_RadioStations_CreatedAt");

        builder.HasIndex(static x => x.LastPlayedAt).HasDatabaseName("IX_RadioStations_LastPlayedAt");

        builder.HasIndex(static x => x.PlayCount).HasDatabaseName("IX_RadioStations_PlayCount");

        // Composite indexes for common queries
        builder
            .HasIndex(static x => new { x.IsEnabled, x.IsOnline })
            .HasDatabaseName("IX_RadioStations_IsEnabled_IsOnline");

        builder.HasIndex(static x => new { x.Genre, x.Country }).HasDatabaseName("IX_RadioStations_Genre_Country");
    }
}
