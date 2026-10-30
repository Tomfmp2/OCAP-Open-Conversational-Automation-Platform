using OCAP.Api.Validation;
using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = OcapPermissions.KnowledgeManage)]
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
            Status = "Ready",
            TenantId = _tenantContext.TenantId,
            SupportedTypes = Enum.GetNames<DocumentType>(),
            SupportedChunkingStrategies = Enum.GetNames<ChunkingStrategy>(),
            VectorDbProvidersImplemented = new[] { "PgVector", "InMemory" },
            VectorDbProvidersNotImplemented = new[] { "Qdrant", "Chroma", "Pinecone" },
            MultiTenantIsolation = "Enforced"
        });
    }

    [HttpGet]
    public async Task<ActionResult> GetKnowledgeBases(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var list = await _knowledgeService.GetKnowledgeBasesAsync(tenantId, cancellationToken);

        // Enriquecer con conteo real de documentos por KB
        var summaries = new List<object>();
        foreach (var kb in list)
        {
            var docs = await _knowledgeService.GetDocumentsAsync(tenantId, kb.Id, cancellationToken);
            var chunkCount = docs.Sum(d => d.TotalChunks);
            summaries.Add(new
            {
                kb.Id,
                kb.Name,
                kb.Description,
                kb.VectorDbProvider,
                kb.Strategy,
                kb.CreatedAtUtc,
                DocumentCount = docs.Count,
                VectorCount = chunkCount
            });
        }

        return Ok(summaries);
    }

    /// <summary>Documentos de una base, del más reciente al más antiguo.</summary>
    [HttpGet("{knowledgeBaseId:guid}/documents")]
    public async Task<ActionResult<IEnumerable<KnowledgeDocumentDto>>> GetDocuments(Guid knowledgeBaseId, CancellationToken cancellationToken)
    {
        var documents = await _knowledgeService.GetDocumentsAsync(_tenantContext.TenantId, knowledgeBaseId, cancellationToken);
        return Ok(documents
            .OrderByDescending(d => d.CreatedAtUtc)
            .Select(d => new KnowledgeDocumentDto(d.Id, d.Title, d.FileType.ToString(), d.Status.ToString(), d.TotalChunks, d.CreatedAtUtc)));
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
    [RequestSizeLimit(KnowledgeUploadValidator.MaxBytes + 64 * 1024)]
    public async Task<ActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] Guid knowledgeBaseId, [FromForm] DocumentCategory category, CancellationToken cancellationToken)
    {
        if (file == null) return BadRequest("Falta el archivo.");

        var (documentType, fileName, error) = await KnowledgeUploadValidator.ValidateAsync(file, cancellationToken);
        if (error is not null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Archivo no válido", detail: error);

        var tenantId = _tenantContext.TenantId;
        using var stream = file.OpenReadStream();
        var doc = await _knowledgeService.UploadDocumentAsync(tenantId, knowledgeBaseId, stream, fileName, documentType, category, "WebUser", cancellationToken);

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

    /// <summary>
    /// Texto escrito en el panel: una pregunta frecuente con su respuesta o una nota. Se indexa como un documento
    /// Markdown más (se puede ver y borrar igual).
    /// </summary>
    [HttpPost("text")]
    public async Task<ActionResult> AddText([FromBody] AddKnowledgeTextDto request, CancellationToken cancellationToken)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        var content = request.Content?.Trim() ?? string.Empty;
        if (title.Length is 0 or > MaxTitleLength || content.Length is 0 or > MaxTextLength)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Escribe un título (hasta {MaxTitleLength} caracteres) y el contenido (hasta {MaxTextLength}).");

        var markdown = $"# {title}\n\n{content}\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));
        var doc = await _knowledgeService.UploadDocumentAsync(
            _tenantContext.TenantId, request.KnowledgeBaseId, stream, FileNameFor(title, "md"), DocumentType.Markdown, DocumentCategory.General, "WebUser", cancellationToken);
        return Ok(doc);
    }

    /// <summary>
    /// Importa una página web pública (HTML, texto o PDF). Solo destinos de internet, revisados al conectar y en
    /// cada redirección; hasta 5 MB.
    /// </summary>
    [HttpPost("url")]
    public async Task<ActionResult> ImportUrl([FromBody] ImportKnowledgeUrlDto request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url?.Trim(), UriKind.Absolute, out var url))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Escribe una dirección completa (https://…).");

        using var http = new HttpClient(OutboundUrlGuard.CreateHandler(allowPrivateNetwork: false)) { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("OCAP-Knowledge/1.0");
        HttpResponseMessage? response = null;
        try
        {
            for (var hop = 0; ; hop++)
            {
                if (await OutboundUrlGuard.ValidateAsync(url.ToString(), allowPrivateNetwork: false, cancellationToken) is { } blocked)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: blocked);
                response?.Dispose();
                response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } next && hop < 3)
                {
                    url = new Uri(url, next);
                    continue;
                }
                break;
            }

            if (!response.IsSuccessStatusCode)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: $"La página respondió {(int)response.StatusCode}.");

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            (DocumentType Type, string Extension)? kind = mediaType switch
            {
                "text/html" or "application/xhtml+xml" => (DocumentType.Html, "html"),
                "text/plain" => (DocumentType.Txt, "txt"),
                "text/markdown" => (DocumentType.Markdown, "md"),
                "application/pdf" => (DocumentType.Pdf, "pdf"),
                _ => null
            };
            if (kind is null)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Solo se importan páginas web, texto o PDF.");

            var body = await ReadLimitedAsync(response, KnowledgeUploadValidator.MaxBytes, cancellationToken);
            if (body is null)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "La página supera los 10 MB.");

            using var stream = new MemoryStream(body);
            var name = FileNameFor($"{url.Host}{url.AbsolutePath.TrimEnd('/')}", kind.Value.Extension);
            var doc = await _knowledgeService.UploadDocumentAsync(
                _tenantContext.TenantId, request.KnowledgeBaseId, stream, name, kind.Value.Type, DocumentCategory.General, "WebUser", cancellationToken);
            return Ok(doc);
        }
        catch (HttpRequestException)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "No se pudo descargar la página.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "La página tardó demasiado en responder.");
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary>El texto que leyó OCAP de un documento, en el orden de sus fragmentos: lo que de verdad usan los agentes.</summary>
    [HttpGet("documents/{id:guid}/content")]
    public async Task<ActionResult> GetDocumentContent(
        Guid id,
        [FromServices] OCAP.Knowledge.Abstractions.IKnowledgeChunkRepository chunks,
        CancellationToken cancellationToken)
    {
        var parts = await chunks.GetByDocumentAsync(id, _tenantContext.TenantId, cancellationToken);
        return Ok(new { chunks = parts.Select(c => c.Content).ToList() });
    }

    private const int MaxTitleLength = 200;
    private const int MaxTextLength = 20_000;

    private static string FileNameFor(string title, string extension)
    {
        var clean = new string(title.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.' ? c : ' ').ToArray()).Trim();
        clean = string.Join(' ', clean.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length == 0) clean = "documento";
        return $"{clean[..Math.Min(clean.Length, 120)]}.{extension}";
    }

    private static async Task<byte[]?> ReadLimitedAsync(HttpResponseMessage response, long maxBytes, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > maxBytes) return null;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
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
public record AddKnowledgeTextDto(Guid KnowledgeBaseId, string Title, string Content);
public record ImportKnowledgeUrlDto(Guid KnowledgeBaseId, string Url);
public record KnowledgeDocumentDto(Guid Id, string Title, string FileType, string Status, int TotalChunks, DateTime CreatedAtUtc);
