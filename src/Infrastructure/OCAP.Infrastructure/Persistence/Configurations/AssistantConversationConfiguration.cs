using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Agents.Domain.Entities;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class AssistantConversationConfiguration : IEntityTypeConfiguration<AssistantConversation>
{
    public void Configure(EntityTypeBuilder<AssistantConversation> builder)
    {
        builder.ToTable("AssistantConversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(AssistantConversation.TitleMaxLength);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.UpdatedAt });
        builder.HasMany<AssistantMessage>().WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssistantMessageConfiguration : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("AssistantMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
    }
}
