namespace OCAP.Knowledge.Abstractions;

/// <summary>
/// Contrato de telemetría y observabilidad para el módulo de Base de Conocimiento y RAG.
/// Diseñado para ser 100% compatible con OpenTelemetry, Prometheus, Grafana y Application Insights.
/// </summary>
public interface IKnowledgeTelemetry
{
    void RecordDocumentProcessed(Guid tenantId, string documentType, long bytesProcessed, double durationMs);
    void RecordChunkGenerated(Guid tenantId, int chunkCount);
    void RecordEmbeddingCreated(Guid tenantId, string provider, string model, int tokenCount, double durationMs);
    void RecordRetrievalExecuted(Guid tenantId, string strategy, int topK, int resultsCount, double topScore, double durationMs);
    void RecordError(Guid tenantId, string operation, string errorMessage);
}
