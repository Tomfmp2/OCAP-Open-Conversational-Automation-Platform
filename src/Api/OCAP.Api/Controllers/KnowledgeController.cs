using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly KnowledgeService _knowledgeService;
    private readonly ITenantContext _tenantContext;

    public KnowledgeController(KnowledgeService knowledgeService, ITenantContext tenantContext)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            Status = "Healthy",
            TenantId = _tenantContext.TenantId,
            SupportedTypes = Enum.GetNames<DocumentType>(),
            SupportedChunkingStrategies = Enum.GetNames<ChunkingStrategy>(),
            VectorDbProviders = Enum.GetNames<VectorDbProviderType>(),
            MultiTenantIsolation = "Enforced"
        });
    }

    [HttpGet]
    public async Task<ActionResult> GetKnowledgeBases(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var list = await _knowledgeService.GetKnowledgeBasesAsync(tenantId, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult> CreateKnowledgeBase([FromBody] CreateKnowledgeBaseDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required for KnowledgeBase.");

        var tenantId = _tenantContext.TenantId;
        var kb = await _knowledgeService.CreateKnowledgeBaseAsync(tenantId, dto.Name, dto.Description, dto.Strategy, dto.VectorDbProvider, cancellationToken);
        return Ok(kb);
    }

    [HttpPost("upload")]
    public async Task<ActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] Guid knowledgeBaseId, [FromForm] DocumentCategory category, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest("File is empty or missing.");

        var tenantId = _tenantContext.TenantId;
        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        
        var documentType = extension switch
        {
            "pdf" => DocumentType.Pdf,
            "docx" => DocumentType.Docx,
            "md" or "markdown" => DocumentType.Markdown,
            "csv" => DocumentType.Csv,
            "json" => DocumentType.Json,
            "html" or "htm" => DocumentType.Html,
            "xml" => DocumentType.Xml,
            _ => DocumentType.Txt
        };

        using var stream = file.OpenReadStream();
        var doc = await _knowledgeService.UploadDocumentAsync(tenantId, knowledgeBaseId, stream, file.FileName, documentType, category, "WebUser", cancellationToken);

        return Ok(doc);
    }

    [HttpGet("search")]
    public async Task<ActionResult> Search([FromQuery] string query, [FromQuery] SearchStrategyType strategy = SearchStrategyType.Hybrid, [FromQuery] int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query parameter is required for search.");

        var tenantId = _tenantContext.TenantId;
        var results = await _knowledgeService.SearchAsync(tenantId, null, query, strategy, topK, 0.4, cancellationToken);
        return Ok(results);
    }

    [HttpPost("reindex")]
    public async Task<ActionResult> Reindex([FromBody] ReindexRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || request.KnowledgeBaseId == Guid.Empty) return BadRequest("KnowledgeBaseId is required.");

        var tenantId = _tenantContext.TenantId;
        await _knowledgeService.ReindexAsync(tenantId, request.KnowledgeBaseId, cancellationToken);
        return Ok(new { Message = "Knowledge base reindexed successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return BadRequest("Invalid document ID.");

        var tenantId = _tenantContext.TenantId;
        await _knowledgeService.DeleteDocumentAsync(tenantId, id, cancellationToken);
        return Ok(new { Message = $"Document {id} deleted successfully." });
    }

    [HttpGet("jobs")]
    public async Task<ActionResult> GetJobs(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var jobs = await _knowledgeService.GetJobsAsync(tenantId, cancellationToken);
        return Ok(jobs);
    }
}

public record CreateKnowledgeBaseDto(string Name, string Description, ChunkingStrategy Strategy, VectorDbProviderType VectorDbProvider);
public record ReindexRequestDto(Guid KnowledgeBaseId);
