using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Security.Domain.Entities;
using System.Text.Json;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class ChannelConnectionConfiguration : IEntityTypeConfiguration<ChannelConnection>
{
    public void Configure(EntityTypeBuilder<ChannelConnection> builder)
    {
        builder.ToTable("ChannelConnections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.DisplayName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.CredentialsReference)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Enabled)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired(false);

        builder.Property(c => c.ConfigurationMetadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        // Índice único compuesto por TenantId y Provider para garantizar el aislamiento y la unicidad del canal por organización.
        builder.HasIndex(c => new { c.TenantId, c.Provider })
            .IsUnique();
    }
}
