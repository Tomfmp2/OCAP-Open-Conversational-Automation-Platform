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
}
