namespace OCAP.Knowledge.Domain.ValueObjects;

public record EmbeddingVector(
    Guid ChunkId,
    string Provider,
    string Model,
    int Dimensions,
    float[] Values,
    Guid DocumentId = default,
    Guid KnowledgeBaseId = default,
    Guid TenantId = default,
    string MetadataJson = "{}",
    IReadOnlyList<string>? Tags = null
);
