namespace OCAP.Knowledge.Domain.ValueObjects;

public record KnowledgeSearchResult(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string Content,
    double Score,
    double Distance,
    string MetadataJson,
    List<string> Highlights
);
