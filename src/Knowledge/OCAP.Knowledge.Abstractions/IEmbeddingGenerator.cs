using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IEmbeddingGenerator
{
    Task<float[]> GenerateVectorAsync(
        string text,
        string? provider = null,
        string? model = null,
        CancellationToken cancellationToken = default);

    Task<List<EmbeddingVector>> GenerateVectorsForChunksAsync(
        List<KnowledgeChunk> chunks,
        string? provider = null,
        string? model = null,
        CancellationToken cancellationToken = default);
}
