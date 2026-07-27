namespace OCAP.Knowledge.Domain.Entities;

public class KnowledgeChunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid KnowledgeBaseId { get; private set; }
    public Guid TenantId { get; private set; }
    public int Index { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }
    public int StartChar { get; private set; }
    public int EndChar { get; private set; }
    public string MetadataJson { get; private set; } = "{}";
    public DateTime CreatedAtUtc { get; private set; }

    private KnowledgeChunk() { }

    public KnowledgeChunk(
        Guid id,
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        int index,
        string content,
        int tokenCount,
        int startChar,
        int endChar,
        string metadataJson = "{}")
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DocumentId = documentId;
        KnowledgeBaseId = knowledgeBaseId;
        TenantId = tenantId;
        Index = index;
        Content = content;
        TokenCount = tokenCount;
        StartChar = startChar;
        EndChar = endChar;
        MetadataJson = metadataJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
