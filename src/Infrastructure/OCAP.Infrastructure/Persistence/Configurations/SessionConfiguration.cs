using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Core.Entities;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.ConversationId)
            .IsRequired();
            
        builder.Property(s => s.ContextData)
            .IsRequired();
            
        builder.Property(s => s.CreatedAt)
            .IsRequired();
            
        builder.Property(s => s.ExpiresAt)
            .IsRequired();
            
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(s => s.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
