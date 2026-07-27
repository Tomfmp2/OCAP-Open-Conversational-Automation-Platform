using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Domain.Entities;

public class KnowledgeBase
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ChunkingStrategy Strategy { get; private set; }
    public int ChunkSize { get; private set; }
    public int Overlap { get; private set; }
    public int MaxTokens { get; private set; }
    public int MinTokens { get; private set; }
    public VectorDbProviderType VectorDbProvider { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private KnowledgeBase() { }

    public KnowledgeBase(
        Guid id,
        Guid tenantId,
        string name,
        string description,
        ChunkingStrategy strategy = ChunkingStrategy.Paragraph,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50,
        VectorDbProviderType vectorDbProvider = VectorDbProviderType.PgVector)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TenantId = tenantId;
        Name = name;
        Description = description ?? string.Empty;
        Strategy = strategy;
        ChunkSize = chunkSize;
        Overlap = overlap;
        MaxTokens = maxTokens;
        MinTokens = minTokens;
        VectorDbProvider = vectorDbProvider;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string description,
        ChunkingStrategy strategy,
        int chunkSize,
        int overlap,
        int maxTokens,
        int minTokens,
        VectorDbProviderType vectorDbProvider)
    {
        Name = name;
        Description = description;
        Strategy = strategy;
        ChunkSize = chunkSize;
        Overlap = overlap;
        MaxTokens = maxTokens;
        MinTokens = minTokens;
        VectorDbProvider = vectorDbProvider;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
