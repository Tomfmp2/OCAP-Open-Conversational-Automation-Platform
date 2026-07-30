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

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.UserId });
            
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Ignore(c => c.DomainEvents);
    }
}
