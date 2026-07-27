using Microsoft.Extensions.Logging;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Application.Services;

public class KnowledgeService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly IKnowledgeDocumentRepository _documentRepository;
    private readonly IKnowledgeChunkRepository _chunkRepository;
    private readonly IDocumentProcessingJobRepository _jobRepository;
    private readonly IDocumentParserFactory _parserFactory;
    private readonly IChunkerFactory _chunkerFactory;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IVectorDatabase _vectorDatabase;
    private readonly IKnowledgeRetriever _retriever;
    private readonly IFileUploadValidator _fileUploadValidator;
    private readonly IKnowledgeTelemetry? _telemetry;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(
        IKnowledgeBaseRepository knowledgeBaseRepository,
        IKnowledgeDocumentRepository documentRepository,
        IKnowledgeChunkRepository chunkRepository,
        IDocumentProcessingJobRepository jobRepository,
        IDocumentParserFactory parserFactory,
        IChunkerFactory chunkerFactory,
        IEmbeddingGenerator embeddingGenerator,
        IVectorDatabase vectorDatabase,
        IKnowledgeRetriever retriever,
        IFileUploadValidator fileUploadValidator,
        ILogger<KnowledgeService> logger,
        IKnowledgeTelemetry? telemetry = null)
    {
        _knowledgeBaseRepository = knowledgeBaseRepository ?? throw new ArgumentNullException(nameof(knowledgeBaseRepository));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
        _chunkerFactory = chunkerFactory ?? throw new ArgumentNullException(nameof(chunkerFactory));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _vectorDatabase = vectorDatabase ?? throw new ArgumentNullException(nameof(vectorDatabase));
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _fileUploadValidator = fileUploadValidator ?? throw new ArgumentNullException(nameof(fileUploadValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetry = telemetry;
    }

    public async Task<KnowledgeBase> CreateKnowledgeBaseAsync(Guid tenantId, string name, string description, ChunkingStrategy strategy, VectorDbProviderType vectorDbProvider, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for tenant isolation.", nameof(tenantId));

        var kb = new KnowledgeBase(Guid.NewGuid(), tenantId, name, description, strategy, 500, 50, 1000, 50, vectorDbProvider);
        await _knowledgeBaseRepository.AddAsync(kb, cancellationToken);
        _logger.LogInformation("Knowledge Base creada: {Id} para Tenant {TenantId}", kb.Id, tenantId);
        return kb;
    }

    public async Task<IReadOnlyList<KnowledgeBase>> GetKnowledgeBasesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for tenant isolation.", nameof(tenantId));
        return await _knowledgeBaseRepository.GetByTenantAsync(tenantId, cancellationToken);
    }

    public async Task<KnowledgeDocument> UploadDocumentAsync(
        Guid tenantId,
        Guid knowledgeBaseId,
        Stream fileStream,
        string fileName,
        DocumentType fileType,
        DocumentCategory category,
        string author = "System",
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for tenant isolation.", nameof(tenantId));
        
        // 1. Validar seguridad del archivo y prevenir Path Traversal / Archivos corruptos
        var validation = _fileUploadValidator.ValidateFile(fileStream, fileName, string.Empty);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Intento de subida de archivo no válido: {Error}", validation.ErrorMessage);
            throw new ArgumentException($"Seguridad de archivo: {validation.ErrorMessage}");
        }

        var sanitizedFileName = validation.SanitizedFileName;
        var sha256Hash = _fileUploadValidator.ComputeSha256Hash(fileStream);

        _logger.LogInformation("Procesando subida de documento seguro '{FileName}' (Hash: {Hash}) para Tenant {TenantId}", sanitizedFileName, sha256Hash, tenantId);

        // 2. Verificar pertenencia de la KnowledgeBase al Tenant
        var kb = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId, cancellationToken);
        if (kb != null && kb.TenantId != tenantId)
        {
            _logger.LogWarning("Acceso no autorizado a Knowledge Base {KbId} por Tenant {TenantId}", knowledgeBaseId, tenantId);
            throw new UnauthorizedAccessException("Acceso denegado a la Base de Conocimiento de otro Tenant.");
        }

        // 3. Instanciar documento y guardar estado pendiente
        var document = new KnowledgeDocument(Guid.NewGuid(), knowledgeBaseId, tenantId, sanitizedFileName, sanitizedFileName, fileType, category, author);
        await _documentRepository.AddAsync(document, cancellationToken);

        var job = new DocumentProcessingJob(Guid.NewGuid(), document.Id, tenantId);
        await _jobRepository.AddAsync(job, cancellationToken);

        document.MarkProcessing();
        await _documentRepository.UpdateAsync(document, cancellationToken);

        try
        {
            // 4. Parsear contenido
            var parser = _parserFactory.GetParser(fileType);
            var parsedResult = await parser.ParseAsync(fileStream, sanitizedFileName, cancellationToken);
            job.UpdateProgress(30);
            await _jobRepository.UpdateAsync(job, cancellationToken);

            // 5. Chunking
            var strategy = kb?.Strategy ?? ChunkingStrategy.Paragraph;
            var chunker = _chunkerFactory.GetChunker(strategy);

            var chunks = chunker.ChunkDocument(
                document.Id,
                knowledgeBaseId,
                tenantId,
                parsedResult.Text,
                kb?.ChunkSize ?? 500,
                kb?.Overlap ?? 50,
                kb?.MaxTokens ?? 1000,
                kb?.MinTokens ?? 50
            );

            await _chunkRepository.AddBatchAsync(chunks, cancellationToken);
            job.UpdateProgress(60);
            await _jobRepository.UpdateAsync(job, cancellationToken);

            // 6. Generate Embeddings & Store in Vector DB
            var vectors = await _embeddingGenerator.GenerateVectorsForChunksAsync(chunks, "OpenAI", "text-embedding-3-small", cancellationToken);
            await _vectorDatabase.UpsertVectorsAsync(tenantId, vectors, cancellationToken);

            job.UpdateProgress(100);
            await _jobRepository.UpdateAsync(job, cancellationToken);

            document.MarkIndexed(chunks.Count);
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Documento '{FileName}' procesado exitosamente con {Count} chunks.", sanitizedFileName, chunks.Count);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar documento '{FileName}'", fileName);
            document.MarkFailed();
            await _documentRepository.UpdateAsync(document, cancellationToken);

            job.MarkFailed(ex.Message);
            await _jobRepository.UpdateAsync(job, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetDocumentsAsync(Guid tenantId, Guid knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        return await _documentRepository.GetByKnowledgeBaseAsync(knowledgeBaseId, tenantId, cancellationToken);
    }

    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string query,
        SearchStrategyType strategy = SearchStrategyType.Hybrid,
        int topK = 5,
        double minScore = 0.5,
        CancellationToken cancellationToken = default)
    {
        return await _retriever.SearchAsync(tenantId, knowledgeBaseId, query, strategy, topK, minScore, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var chunks = await _chunkRepository.GetByDocumentAsync(documentId, tenantId, cancellationToken);
        var chunkIds = chunks.Select(c => c.Id);

        await _vectorDatabase.DeleteVectorsAsync(tenantId, chunkIds, cancellationToken);
        await _chunkRepository.DeleteByDocumentAsync(documentId, tenantId, cancellationToken);
        await _documentRepository.DeleteAsync(documentId, tenantId, cancellationToken);

        _logger.LogInformation("Documento {DocumentId} eliminado correctamente para Tenant {TenantId}", documentId, tenantId);
    }

    public async Task ReindexAsync(Guid tenantId, Guid knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        var docs = await _documentRepository.GetByKnowledgeBaseAsync(knowledgeBaseId, tenantId, cancellationToken);
        foreach (var doc in docs)
        {
            var chunks = await _chunkRepository.GetByDocumentAsync(doc.Id, tenantId, cancellationToken);
            var vectors = await _embeddingGenerator.GenerateVectorsForChunksAsync(chunks.ToList(), "OpenAI", "text-embedding-3-small", cancellationToken);
            await _vectorDatabase.UpsertVectorsAsync(tenantId, vectors, cancellationToken);
        }

        _logger.LogInformation("Reindexación completada para Knowledge Base {KnowledgeBaseId}", knowledgeBaseId);
    }

    public async Task<IReadOnlyList<DocumentProcessingJob>> GetJobsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _jobRepository.GetPendingJobsAsync(tenantId, cancellationToken);
    }
}
