using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IKnowledgeRetriever
{
    Task<List<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId,
        string query,
        SearchStrategyType strategy = SearchStrategyType.Hybrid,
        int topK = 5,
        double minScore = 0.5,
        CancellationToken cancellationToken = default);

    Task<List<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string query,
        SearchStrategyType strategy = SearchStrategyType.Hybrid,
        int topK = 5,
        double minScore = 0.5,
        CancellationToken cancellationToken = default) => SearchAsync(tenantId, query, strategy, topK, minScore, cancellationToken);

    /// <summary>Solo en esas colecciones (las que eligió un agente). Vacía = todas.</summary>
    Task<List<KnowledgeSearchResult>> SearchInBasesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> knowledgeBaseIds,
        string query,
        int topK = 5,
        double minScore = 0.5,
        CancellationToken cancellationToken = default) => SearchAsync(tenantId, query, SearchStrategyType.Hybrid, topK, minScore, cancellationToken);
}
