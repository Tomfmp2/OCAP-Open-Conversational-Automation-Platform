using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Intelligence.Domain;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class AiProviderConfigurationConfiguration : IEntityTypeConfiguration<AiProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<AiProviderConfiguration> builder)
    {
        builder.ToTable("AiProviderConfigurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ProviderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ModelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.VaultSecretReference)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.SettingsJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.TenantId, x.ProviderName })
            .IsUnique();
    }
}
