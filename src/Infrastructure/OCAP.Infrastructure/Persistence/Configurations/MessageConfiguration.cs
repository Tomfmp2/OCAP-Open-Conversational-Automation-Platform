using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Core.Entities;
using OCAP.Core.ValueObjects;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.ConversationId)
            .IsRequired();
            
        builder.Property(m => m.Content)
            .HasConversion(
                v => v.Value,
                v => new MessageContent(v))
            .IsRequired()
            .HasMaxLength(4096);
            
        builder.Property(m => m.SenderType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(m => m.CreatedAt)
            .IsRequired();
            
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
