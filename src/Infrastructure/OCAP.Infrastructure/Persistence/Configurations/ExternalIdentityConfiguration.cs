using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Security.Domain.Entities;
using System.Text.Json;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.ToTable("ExternalIdentities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        // Índice compuesto único para evitar identidades externas duplicadas por Tenant.
        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalId })
            .IsUnique();
    }
}
