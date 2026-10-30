using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IEmbeddingProvider
{
    string ProviderName { get; }
    Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<KnowledgeChunk> chunks, string model, EmbeddingCredential? credential = null, CancellationToken cancellationToken = default);
    Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<string> texts, string model, EmbeddingCredential? credential = null, CancellationToken cancellationToken = default);
}

/// <summary>Key y Base URL de un proveedor de embeddings; null en un campo = el de la configuración (.env).</summary>
public sealed record EmbeddingCredential(string? ApiKey, string? BaseUrl);

/// <summary>Proveedor, modelo y credencial con que una organización genera sus embeddings.</summary>
public sealed record EmbeddingTarget(string Provider, string Model, EmbeddingCredential? Credential);

/// <summary>
/// Elige con qué proveedor genera embeddings la organización de la sesión: el que ya usó para indexar (búsquedas e
/// índice deben usar el mismo modelo) o el primero que tenga configurado en IA y modelos. null = configuración global.
/// </summary>
public interface IEmbeddingTargetResolver
{
    Task<EmbeddingTarget?> ResolveAsync(CancellationToken cancellationToken = default);
}
