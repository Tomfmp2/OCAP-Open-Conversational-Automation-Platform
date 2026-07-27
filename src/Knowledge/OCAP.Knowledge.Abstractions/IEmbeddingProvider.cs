using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IEmbeddingProvider
{
    string ProviderName { get; }
    Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<KnowledgeChunk> chunks, string model, CancellationToken cancellationToken = default);
    Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<string> texts, string model, CancellationToken cancellationToken = default);
}
