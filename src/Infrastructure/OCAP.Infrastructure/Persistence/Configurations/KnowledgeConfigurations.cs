using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Knowledge.Domain.Entities;
using Pgvector;

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

public class KnowledgeEmbeddingConfiguration : IEntityTypeConfiguration<KnowledgeEmbedding>
{
    public const int DefaultDimensions = 1536;

    public void Configure(EntityTypeBuilder<KnowledgeEmbedding> builder)
    {
        builder.ToTable("KnowledgeEmbeddings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Model).HasMaxLength(128).IsRequired();
        builder.Property(e => e.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.TagsJson).HasColumnType("jsonb").IsRequired();

        builder.Property(e => e.Values)
            .HasColumnName("Embedding")
            .HasConversion(
                v => new Vector(v),
                v => v.ToArray())
            .HasColumnType($"vector({DefaultDimensions})")
            .IsRequired();

        builder.HasIndex(e => e.ChunkId).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.KnowledgeBaseId });
        builder.HasIndex(e => new { e.TenantId, e.DocumentId });
        builder.HasIndex(e => e.TenantId);
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
