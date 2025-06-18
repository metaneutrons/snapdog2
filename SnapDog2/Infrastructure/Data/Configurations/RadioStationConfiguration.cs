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
            t =>
            {
                t.HasCheckConstraint("CK_RadioStations_BitrateKbps", "BitrateKbps IS NULL OR BitrateKbps > 0");
                t.HasCheckConstraint("CK_RadioStations_SampleRateHz", "SampleRateHz IS NULL OR SampleRateHz > 0");
                t.HasCheckConstraint("CK_RadioStations_Channels", "Channels IS NULL OR Channels > 0");
                t.HasCheckConstraint("CK_RadioStations_Priority", "Priority > 0");
                t.HasCheckConstraint("CK_RadioStations_PlayCount", "PlayCount >= 0");
            }
        );

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        // StreamUrl value object conversion
        builder
            .Property(x => x.Url)
            .HasConversion(v => v.Value.AbsoluteUri, v => new StreamUrl(v))
            .HasColumnName("Url")
            .HasMaxLength(2000)
            .IsRequired();

        // Enum conversions
        builder.Property(x => x.Codec).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Optional metadata properties
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Genre).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Country).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Language).HasMaxLength(50).IsRequired(false);

        builder.Property(x => x.Website).HasMaxLength(500).IsRequired(false);

        builder.Property(x => x.LogoUrl).HasMaxLength(500).IsRequired(false);

        builder.Property(x => x.Tags).HasMaxLength(500).IsRequired(false);

        // Technical properties
        builder.Property(x => x.BitrateKbps).IsRequired(false);

        builder.Property(x => x.SampleRateHz).IsRequired(false);

        builder.Property(x => x.Channels).IsRequired(false);

        // Boolean properties
        builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.RequiresAuth).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.IsOnline).IsRequired(false);

        // Authentication properties
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Password).HasMaxLength(200).IsRequired(false);

        // Other properties
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(1);

        builder.Property(x => x.PlayCount).IsRequired().HasDefaultValue(0);

        // Timestamp properties
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.Property(x => x.LastPlayedAt).IsRequired(false);

        builder.Property(x => x.LastCheckedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_RadioStations_Name");

        builder.HasIndex(x => x.Genre).HasDatabaseName("IX_RadioStations_Genre");

        builder.HasIndex(x => x.Country).HasDatabaseName("IX_RadioStations_Country");

        builder.HasIndex(x => x.Language).HasDatabaseName("IX_RadioStations_Language");

        builder.HasIndex(x => x.Codec).HasDatabaseName("IX_RadioStations_Codec");

        builder.HasIndex(x => x.IsEnabled).HasDatabaseName("IX_RadioStations_IsEnabled");

        builder.HasIndex(x => x.IsOnline).HasDatabaseName("IX_RadioStations_IsOnline");

        builder.HasIndex(x => x.Priority).HasDatabaseName("IX_RadioStations_Priority");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_RadioStations_CreatedAt");

        builder.HasIndex(x => x.LastPlayedAt).HasDatabaseName("IX_RadioStations_LastPlayedAt");

        builder.HasIndex(x => x.PlayCount).HasDatabaseName("IX_RadioStations_PlayCount");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.IsEnabled, x.IsOnline }).HasDatabaseName("IX_RadioStations_IsEnabled_IsOnline");

        builder.HasIndex(x => new { x.Genre, x.Country }).HasDatabaseName("IX_RadioStations_Genre_Country");
    }
}
