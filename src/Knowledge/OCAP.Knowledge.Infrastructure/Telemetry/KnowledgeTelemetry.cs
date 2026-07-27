using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OCAP.Knowledge.Abstractions;

namespace OCAP.Knowledge.Infrastructure.Telemetry;

// Sistema de observabilidad enterprise para OCAP Knowledge & RAG. Emite métricas y trazas distribuidas utilizando System.Diagnostics.
public class KnowledgeTelemetry : IKnowledgeTelemetry
{
    private static readonly ActivitySource ActivitySource = new("OCAP.Knowledge", "1.5.1");
    private static readonly Meter KnowledgeMeter = new("OCAP.Knowledge.Metrics", "1.5.1");

    private readonly Counter<long> _documentsProcessedCounter;
    private readonly Counter<long> _chunksGeneratedCounter;
    private readonly Counter<long> _embeddingsCreatedCounter;
    private readonly Counter<long> _retrievalsExecutedCounter;
    private readonly Counter<long> _errorsCounter;

    private readonly Histogram<double> _processingTimeHistogram;
    private readonly Histogram<double> _retrievalTimeHistogram;
    private readonly Histogram<double> _topScoreHistogram;

    private readonly ILogger<KnowledgeTelemetry> _logger;

    public KnowledgeTelemetry(ILogger<KnowledgeTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _documentsProcessedCounter = KnowledgeMeter.CreateCounter<long>("ocap_knowledge_documents_processed_total", "count", "Total de documentos procesados");
        _chunksGeneratedCounter = KnowledgeMeter.CreateCounter<long>("ocap_knowledge_chunks_generated_total", "count", "Total de chunks generados");
        _embeddingsCreatedCounter = KnowledgeMeter.CreateCounter<long>("ocap_knowledge_embeddings_created_total", "count", "Total de embeddings generados");
        _retrievalsExecutedCounter = KnowledgeMeter.CreateCounter<long>("ocap_knowledge_retrievals_executed_total", "count", "Total de búsquedas RAG ejecutadas");
        _errorsCounter = KnowledgeMeter.CreateCounter<long>("ocap_knowledge_errors_total", "count", "Total de errores en módulo Knowledge");

        _processingTimeHistogram = KnowledgeMeter.CreateHistogram<double>("ocap_knowledge_document_processing_duration_ms", "ms", "Duración del procesamiento de documentos");
        _retrievalTimeHistogram = KnowledgeMeter.CreateHistogram<double>("ocap_knowledge_retrieval_duration_ms", "ms", "Duración de búsquedas RAG");
        _topScoreHistogram = KnowledgeMeter.CreateHistogram<double>("ocap_knowledge_retrieval_top_score", "score", "Puntuación de relevancia máxima en búsquedas RAG");
    }

    public void RecordDocumentProcessed(Guid tenantId, string documentType, long bytesProcessed, double durationMs)
    {
        using var activity = ActivitySource.StartActivity("Knowledge.DocumentProcessed");
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("document.type", documentType);
        activity?.SetTag("document.bytes", bytesProcessed);

        _documentsProcessedCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()), new KeyValuePair<string, object?>("document_type", documentType));
        _processingTimeHistogram.Record(durationMs, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

        _logger.LogInformation("[Telemetry] DocumentProcessed | Tenant: {TenantId} | Type: {Type} | Duration: {Duration}ms", tenantId, documentType, durationMs);
    }

    public void RecordChunkGenerated(Guid tenantId, int chunkCount)
    {
        _chunksGeneratedCounter.Add(chunkCount, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
    }

    public void RecordEmbeddingCreated(Guid tenantId, string provider, string model, int tokenCount, double durationMs)
    {
        using var activity = ActivitySource.StartActivity("Knowledge.EmbeddingCreated");
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("embedding.provider", provider);
        activity?.SetTag("embedding.model", model);

        _embeddingsCreatedCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()), new KeyValuePair<string, object?>("provider", provider));
    }

    public void RecordRetrievalExecuted(Guid tenantId, string strategy, int topK, int resultsCount, double topScore, double durationMs)
    {
        using var activity = ActivitySource.StartActivity("Knowledge.RetrievalExecuted");
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("retrieval.strategy", strategy);
        activity?.SetTag("retrieval.top_k", topK);

        _retrievalsExecutedCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()), new KeyValuePair<string, object?>("strategy", strategy));
        _retrievalTimeHistogram.Record(durationMs, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
        _topScoreHistogram.Record(topScore, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

        _logger.LogInformation("[Telemetry] RetrievalExecuted | Tenant: {TenantId} | Strategy: {Strategy} | Results: {Count} | TopScore: {Score:F2} | Duration: {Duration}ms", tenantId, strategy, resultsCount, topScore, durationMs);
    }

    public void RecordError(Guid tenantId, string operation, string errorMessage)
    {
        using var activity = ActivitySource.StartActivity("Knowledge.Error");
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("operation", operation);

        _errorsCounter.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()), new KeyValuePair<string, object?>("operation", operation));
        _logger.LogError("[Telemetry Error] Operation: {Operation} | Tenant: {TenantId} | Error: {Error}", operation, tenantId, errorMessage);
    }
}
