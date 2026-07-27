using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class KnowledgeBaseConfiguration : IEntityTypeConfiguration<KnowledgeBase>
{
    public void Configure(EntityTypeBuilder<KnowledgeBase> builder)
    {
        builder.ToTable("KnowledgeBases");
        builder.HasKey(kb => kb.Id);

        builder.Property(kb => kb.Name).HasMaxLength(250).IsRequired();
        builder.Property(kb => kb.Description).HasMaxLength(1000);

        builder.HasIndex(kb => kb.TenantId);
    }
}

public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable("KnowledgeDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).HasMaxLength(250).IsRequired();
        builder.Property(d => d.SourcePath).HasMaxLength(500);
        builder.Property(d => d.Version).HasMaxLength(50);
        builder.Property(d => d.Author).HasMaxLength(100);
        builder.Property(d => d.ContentHash).HasMaxLength(64);

        builder.HasIndex(d => new { d.TenantId, d.KnowledgeBaseId });
    }
}

public class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> builder)
    {
        builder.ToTable("KnowledgeChunks");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.DocumentId });
        builder.HasIndex(c => new { c.TenantId, c.KnowledgeBaseId });
    }
}

public class DocumentProcessingJobConfiguration : IEntityTypeConfiguration<DocumentProcessingJob>
{
    public void Configure(EntityTypeBuilder<DocumentProcessingJob> builder)
    {
        builder.ToTable("DocumentProcessingJobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(j => new { j.TenantId, j.DocumentId });
    }
}

public class DocumentPermissionConfiguration : IEntityTypeConfiguration<DocumentPermission>
{
    public void Configure(EntityTypeBuilder<DocumentPermission> builder)
    {
        builder.ToTable("DocumentPermissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Role).HasMaxLength(100).IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.DocumentId });
    }
}
