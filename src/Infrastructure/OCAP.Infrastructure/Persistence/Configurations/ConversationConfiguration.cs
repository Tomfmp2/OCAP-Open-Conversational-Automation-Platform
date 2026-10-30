using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Core.Entities;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.UserId)
            .IsRequired();
            
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(c => c.CreatedAt)
            .IsRequired();
            
        builder.Property(c => c.LastActivityAt)
            .IsRequired();

        builder.Property(c => c.Channel).HasMaxLength(50);
        builder.Property(c => c.ExternalContactId).HasMaxLength(200);

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.UserId });
        builder.HasIndex(c => new { c.TenantId, c.LastActivityAt });
        builder.Property(c => c.Summary).HasMaxLength(4000);
            
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.IsHandledByHuman);
    }
}

public class ContactMemoryConfiguration : IEntityTypeConfiguration<OCAP.Core.Entities.ContactMemory>
{
    public void Configure(EntityTypeBuilder<OCAP.Core.Entities.ContactMemory> builder)
    {
        builder.ToTable("ContactMemories");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.OwnerKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Key).HasMaxLength(OCAP.Core.Entities.ContactMemory.MaxKeyLength);
        builder.Property(m => m.Text).IsRequired().HasMaxLength(OCAP.Core.Entities.ContactMemory.MaxTextLength);
        builder.HasIndex(m => new { m.TenantId, m.OwnerKind, m.OwnerId, m.ExpiresAt });
        builder.HasIndex(m => m.ExpiresAt);
    }
}
