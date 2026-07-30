using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Core.Entities;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(u => u.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(u => u.CreatedAt)
            .IsRequired();
            
        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => new { u.TenantId, u.DisplayName });
    }
}
