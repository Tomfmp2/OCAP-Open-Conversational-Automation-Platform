using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IEmbeddingGenerator
{
    Task<float[]> GenerateVectorAsync(string text, string provider = "OpenAI", string model = "text-embedding-3-small", CancellationToken cancellationToken = default);
    Task<List<EmbeddingVector>> GenerateVectorsForChunksAsync(List<KnowledgeChunk> chunks, string provider = "OpenAI", string model = "text-embedding-3-small", CancellationToken cancellationToken = default);
}
