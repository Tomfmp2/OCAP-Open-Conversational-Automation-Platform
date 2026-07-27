using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Domain.Entities;

public class KnowledgeDocument
{
    public Guid Id { get; private set; }
    public Guid KnowledgeBaseId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string SourcePath { get; private set; } = string.Empty;
    public DocumentType FileType { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string Version { get; private set; } = "1.0.0";
    public string Author { get; private set; } = string.Empty;
    public DocumentCategory Category { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public int TotalChunks { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private KnowledgeDocument() { }

    public KnowledgeDocument(
        Guid id,
        Guid knowledgeBaseId,
        Guid tenantId,
        string title,
        string sourcePath,
        DocumentType fileType,
        DocumentCategory category = DocumentCategory.General,
        string author = "System",
        string version = "1.0.0",
        string contentHash = "")
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (knowledgeBaseId == Guid.Empty) throw new ArgumentException("KnowledgeBaseId is required", nameof(knowledgeBaseId));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        KnowledgeBaseId = knowledgeBaseId;
        TenantId = tenantId;
        Title = title;
        SourcePath = sourcePath;
        FileType = fileType;
        Category = category;
        Author = author;
        Version = version;
        ContentHash = contentHash;
        Status = DocumentStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkProcessing() => Status = DocumentStatus.Processing;

    public void MarkIndexed(int totalChunks)
    {
        Status = DocumentStatus.Indexed;
        TotalChunks = totalChunks;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed() => Status = DocumentStatus.Failed;
    public void Archive() => Status = DocumentStatus.Archived;
}
