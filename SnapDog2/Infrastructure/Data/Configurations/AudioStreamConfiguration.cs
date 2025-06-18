using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the AudioStream entity.
/// Configures table mapping, constraints, indexes, and value object conversions.
/// </summary>
public class AudioStreamConfiguration : IEntityTypeConfiguration<AudioStream>
{
    /// <summary>
    /// Configures the AudioStream entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<AudioStream> builder)
    {
        // Table configuration with check constraints
        builder.ToTable(
            "AudioStreams",
            t =>
            {
                t.HasCheckConstraint("CK_AudioStreams_BitrateKbps", "BitrateKbps > 0");
                t.HasCheckConstraint("CK_AudioStreams_SampleRateHz", "SampleRateHz IS NULL OR SampleRateHz > 0");
                t.HasCheckConstraint("CK_AudioStreams_Channels", "Channels IS NULL OR Channels > 0");
            }
        );

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.Property(x => x.BitrateKbps).IsRequired();

        // StreamUrl value object conversion
        builder
            .Property(x => x.Url)
            .HasConversion(v => v.Value.AbsoluteUri, v => new StreamUrl(v))
            .HasColumnName("Url")
            .HasMaxLength(2000)
            .IsRequired();

        // Enum conversions
        builder.Property(x => x.Codec).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Optional properties
        builder.Property(x => x.SampleRateHz).IsRequired(false);

        builder.Property(x => x.Channels).IsRequired(false);

        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Tags).HasMaxLength(500).IsRequired(false);

        // Timestamp properties
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_AudioStreams_Name");

        builder.HasIndex(x => x.Status).HasDatabaseName("IX_AudioStreams_Status");

        builder.HasIndex(x => x.Codec).HasDatabaseName("IX_AudioStreams_Codec");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_AudioStreams_CreatedAt");
    }
}
