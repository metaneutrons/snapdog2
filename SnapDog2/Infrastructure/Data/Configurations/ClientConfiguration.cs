using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Client entity.
/// Configures table mapping, constraints, indexes, and value object conversions.
/// </summary>
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    /// <summary>
    /// Configures the Client entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        // Table configuration with check constraints
        builder.ToTable(
            "Clients",
            t =>
            {
                t.HasCheckConstraint("CK_Clients_Volume", "Volume >= 0 AND Volume <= 100");
                t.HasCheckConstraint("CK_Clients_LatencyMs", "LatencyMs IS NULL OR LatencyMs >= 0");
            }
        );

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100).IsRequired();

        // Required properties
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Volume).IsRequired();

        // MacAddress value object conversion
        builder
            .Property(x => x.MacAddress)
            .HasConversion(v => v.Value, v => new MacAddress(v))
            .HasColumnName("MacAddress")
            .HasMaxLength(17)
            .IsRequired();

        // IpAddress value object conversion
        builder
            .Property(x => x.IpAddress)
            .HasConversion(v => v.Value.ToString(), v => new IpAddress(v))
            .HasColumnName("IpAddress")
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        // Enum conversions
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Boolean properties
        builder.Property(x => x.IsMuted).IsRequired().HasDefaultValue(false);

        // Optional properties
        builder.Property(x => x.ZoneId).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Location).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.LatencyMs).IsRequired(false);

        builder.Property(x => x.LastSeen).IsRequired(false);

        // Timestamp properties
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt).IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_Clients_Name");

        builder.HasIndex(x => x.MacAddress).IsUnique().HasDatabaseName("IX_Clients_MacAddress");

        builder.HasIndex(x => x.IpAddress).HasDatabaseName("IX_Clients_IpAddress");

        builder.HasIndex(x => x.Status).HasDatabaseName("IX_Clients_Status");

        builder.HasIndex(x => x.ZoneId).HasDatabaseName("IX_Clients_ZoneId");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Clients_CreatedAt");

        builder.HasIndex(x => x.LastSeen).HasDatabaseName("IX_Clients_LastSeen");
    }
}
