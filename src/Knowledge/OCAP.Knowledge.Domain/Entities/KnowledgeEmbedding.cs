namespace OCAP.Knowledge.Domain.Entities;

/// <summary>
/// Persistencia de embeddings vectoriales asociados a un KnowledgeChunk.
/// </summary>
public class KnowledgeEmbedding
{
    public Guid Id { get; private set; }
    public Guid ChunkId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid KnowledgeBaseId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Dimensions { get; private set; }
    public float[] Values { get; private set; } = Array.Empty<float>();
    public string MetadataJson { get; private set; } = "{}";
    public string TagsJson { get; private set; } = "[]";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private KnowledgeEmbedding()
    {
    }

    public KnowledgeEmbedding(
        Guid id,
        Guid chunkId,
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        string provider,
        string model,
        float[] values,
        string metadataJson = "{}",
        string tagsJson = "[]")
    {
        if (chunkId == Guid.Empty) throw new ArgumentException("ChunkId is required.", nameof(chunkId));
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (values is null || values.Length == 0) throw new ArgumentException("Embedding values are required.", nameof(values));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ChunkId = chunkId;
        DocumentId = documentId;
        KnowledgeBaseId = knowledgeBaseId;
        TenantId = tenantId;
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Dimensions = values.Length;
        Values = values;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
        TagsJson = string.IsNullOrWhiteSpace(tagsJson) ? "[]" : tagsJson;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void UpdateVector(float[] values, string provider, string model, string metadataJson, string tagsJson)
    {
        if (values is null || values.Length == 0) throw new ArgumentException("Embedding values are required.", nameof(values));
        Values = values;
        Dimensions = values.Length;
        Provider = provider ?? Provider;
        Model = model ?? Model;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? MetadataJson : metadataJson;
        TagsJson = string.IsNullOrWhiteSpace(tagsJson) ? TagsJson : tagsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
