using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Track entity.
/// Configures table mapping, constraints, indexes, and collection conversions.
/// </summary>
public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    /// <summary>
    /// Configures the Track entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        // Table configuration with check constraints
        builder.ToTable(
            "Tracks",
            t =>
            {
                t.HasCheckConstraint("CK_Tracks_Year", "Year IS NULL OR Year > 0");
                t.HasCheckConstraint("CK_Tracks_TrackNumber", "TrackNumber IS NULL OR TrackNumber > 0");
                t.HasCheckConstraint("CK_Tracks_TotalTracks", "TotalTracks IS NULL OR TotalTracks > 0");
                t.HasCheckConstraint("CK_Tracks_DurationSeconds", "DurationSeconds IS NULL OR DurationSeconds >= 0");
                t.HasCheckConstraint("CK_Tracks_FileSizeBytes", "FileSizeBytes IS NULL OR FileSizeBytes >= 0");
                t.HasCheckConstraint("CK_Tracks_BitrateKbps", "BitrateKbps IS NULL OR BitrateKbps > 0");
                t.HasCheckConstraint("CK_Tracks_SampleRateHz", "SampleRateHz IS NULL OR SampleRateHz > 0");
                t.HasCheckConstraint("CK_Tracks_Channels", "Channels IS NULL OR Channels > 0");
                t.HasCheckConstraint("CK_Tracks_PlayCount", "PlayCount >= 0");
            }
        );

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();

        // Optional metadata properties
        builder.Property(x => x.Artist).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Album).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Genre).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Year).IsRequired(false);

        builder.Property(x => x.TrackNumber).IsRequired(false);

        builder.Property(x => x.TotalTracks).IsRequired(false);

        builder.Property(x => x.AlbumArtist).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Composer).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Conductor).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.Label).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.ISRC).HasMaxLength(12).IsRequired(false);

        builder.Property(x => x.MusicBrainzTrackId).HasMaxLength(36).IsRequired(false);

        builder.Property(x => x.MusicBrainzRecordingId).HasMaxLength(36).IsRequired(false);

        // File properties
        builder.Property(x => x.FilePath).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.FileSizeBytes).IsRequired(false);

        builder.Property(x => x.Format).HasMaxLength(50).IsRequired(false);

        builder.Property(x => x.ArtworkPath).HasMaxLength(1000).IsRequired(false);

        // Technical properties
        builder.Property(x => x.DurationSeconds).IsRequired(false);

        builder.Property(x => x.BitrateKbps).IsRequired(false);

        builder.Property(x => x.SampleRateHz).IsRequired(false);

        builder.Property(x => x.Channels).IsRequired(false);

        // Tags collection conversion to JSON
        builder
            .Property(x => x.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    (
                        JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>()
                    ).ToImmutableDictionary()
            )
            .HasColumnName("Tags")
            .HasColumnType("TEXT");

        // Large text properties
        builder.Property(x => x.Lyrics).HasColumnType("TEXT").IsRequired(false);

        // Play statistics
        builder.Property(x => x.PlayCount).IsRequired().HasDefaultValue(0);

        // Timestamp properties
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.Property(x => x.LastPlayedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => x.Title).HasDatabaseName("IX_Tracks_Title");

        builder.HasIndex(x => x.Artist).HasDatabaseName("IX_Tracks_Artist");

        builder.HasIndex(x => x.Album).HasDatabaseName("IX_Tracks_Album");

        builder.HasIndex(x => x.Genre).HasDatabaseName("IX_Tracks_Genre");

        builder.HasIndex(x => x.Year).HasDatabaseName("IX_Tracks_Year");

        builder.HasIndex(x => x.AlbumArtist).HasDatabaseName("IX_Tracks_AlbumArtist");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Tracks_CreatedAt");

        builder.HasIndex(x => x.LastPlayedAt).HasDatabaseName("IX_Tracks_LastPlayedAt");

        builder.HasIndex(x => x.PlayCount).HasDatabaseName("IX_Tracks_PlayCount");

        builder.HasIndex(x => x.DurationSeconds).HasDatabaseName("IX_Tracks_DurationSeconds");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.Artist, x.Album }).HasDatabaseName("IX_Tracks_Artist_Album");

        builder.HasIndex(x => new { x.Album, x.TrackNumber }).HasDatabaseName("IX_Tracks_Album_TrackNumber");
    }
}
