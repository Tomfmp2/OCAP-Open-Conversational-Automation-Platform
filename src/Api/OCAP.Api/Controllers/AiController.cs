using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Api.Services;
using OCAP.Intelligence.Abstractions;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using System.Diagnostics;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = OcapPermissions.AiExecute)]
public class AiController : ControllerBase
{
    private readonly IAiProvider _aiProvider;
    private readonly IAiProviderRegistry _registry;
    private readonly OCAPDbContext _dbContext;
    private readonly ILogger<AiController> _logger;
    private readonly TenantAiProviderPreference _preference;

    public AiController(
        IAiProvider aiProvider, 
        IAiProviderRegistry registry,
        OCAPDbContext dbContext,
        ILogger<AiController> logger,
        TenantAiProviderPreference preference)
    {
        _preference = preference ?? throw new ArgumentNullException(nameof(preference));
        _logger = logger;
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("status")]
    public async Task<ActionResult<AiStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var activeName = await _preference.GetActiveProviderNameAsync(cancellationToken);
        var activeProvider = _registry.GetProvider(activeName) ?? _aiProvider;
        var modelInfo = activeProvider.GetModelInformation();
        var health = await activeProvider.HealthAsync(cancellationToken);

        var dto = new AiStatusDto(
            ActiveProvider: modelInfo.Provider,
            ActiveModel: modelInfo.Model,
            Status: health.IsHealthy ? "Online" : (string.IsNullOrWhiteSpace(health.StatusMessage) ? "Offline" : "Degraded"),
            LastCheckedUtc: DateTime.UtcNow
        );
        return Ok(dto);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<AiUsageDto>> GetUsage([FromServices] ITenantContext tenant, CancellationToken cancellationToken)
    {
        var logs = _dbContext.AiExecutionLogs.AsNoTracking().Where(l => l.TenantId == tenant.TenantId);

        var totals = await logs
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Tokens = g.Sum(l => (long)l.Tokens),
                Count = g.Count(),
                AverageMs = g.Average(l => l.ExecutionTimeMs),
                Succeeded = g.Count(l => l.Success),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var since = DateTime.UtcNow.AddDays(-1);
        var lastDay = await logs
            .Where(l => l.ExecutedAt >= since)
            .GroupBy(l => new { l.Provider, l.Model })
            .Select(g => new AiModelUsageDto(g.Key.Provider, g.Key.Model, g.Count(), g.Count(l => !l.Success), g.Sum(l => (long)l.Tokens)))
            .ToListAsync(cancellationToken);

        var dto = new AiUsageDto(
            TotalTokensUsed: (int)Math.Min(totals?.Tokens ?? 0, int.MaxValue),
            TotalExecutionsCount: totals?.Count ?? 0,
            AverageLatencyMs: totals?.AverageMs ?? 0,
            SuccessRatePercentage: totals is null ? 0 : totals.Succeeded * 100.0 / totals.Count,
            LastDay: lastDay.OrderByDescending(m => m.Requests).ToList());
        return Ok(dto);
    }

    [HttpGet("models")]
    public ActionResult<List<AiModelInfoDto>> GetModels()
    {
        var models = new List<AiModelInfoDto>();
        var names = _registry.GetRegisteredProviderNames();
        
        foreach (var name in names)
        {
            var provider = _registry.GetProvider(name);
            if (provider != null)
            {
                var info = provider.GetModelInformation();
                models.Add(new AiModelInfoDto(info.Provider, info.Model, info.ContextSize, info.Capabilities));
            }
        }

        if (!models.Any())
        {
            var currentModel = _aiProvider.GetModelInformation();
            models.Add(new AiModelInfoDto(currentModel.Provider, currentModel.Model, currentModel.ContextSize, currentModel.Capabilities));
        }

        return Ok(models);
    }

    /// <summary>
    /// POST /api/ai/test-generation
    /// Ejecuta un prompt de prueba contra el proveedor LLM indicado y devuelve la respuesta con métricas reales.
    /// </summary>
    [HttpPost("test-generation")]
    public async Task<IActionResult> TestGeneration(
        [FromBody] AiTestGenerationRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Prompt))
            return BadRequest(new { message = "El campo 'Prompt' es requerido." });

        // Seleccionar proveedor según la solicitud o usar el proveedor activo
        IAiProvider? provider = null;
        if (!string.IsNullOrWhiteSpace(dto.Provider))
        {
            provider = _registry.GetProvider(dto.Provider);
        }
        provider ??= _registry.GetProvider(await _preference.GetActiveProviderNameAsync(cancellationToken)) ?? _aiProvider;

        var request = new AiRequest
        {
            UserMessage = dto.Prompt,
            SystemInstructions = "Eres un asistente de IA de OCAP. Responde de forma clara y concisa.",
            Temperature = dto.Temperature,
            MaxTokens = dto.MaxTokens,
            EnableStreaming = false
        };

        var sw = Stopwatch.StartNew();
        AiResponse aiResponse;

        try
        {
            aiResponse = await provider.GenerateResponseAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // El detalle del proveedor puede incluir la petición o datos de la cuenta: va al log, no al cliente.
            _logger.LogError(ex, "El proveedor de IA {Provider} falló al generar la respuesta.", dto.Provider);
            return StatusCode(502, new { message = $"El proveedor '{dto.Provider}' no pudo generar la respuesta. Revisa su configuración en Modelos de IA." });
        }

        sw.Stop();

        // Costo estimado (USD por 1000 tokens) — valores de referencia del mercado
        var costPer1KTokens = (dto.Provider ?? string.Empty).ToUpperInvariant() switch
        {
            "OPENAI"    => 0.002,
            "GEMINI"    => 0.00035,
            "ANTHROPIC" => 0.003,
            _           => 0.0001  // Mock / Ollama / local
        };
        var estimatedCost = (aiResponse.TokensUsed / 1000.0) * costPer1KTokens;

        return Ok(new
        {
            Content          = aiResponse.GeneratedText,
            TokensUsed       = aiResponse.TokensUsed,
            LatencyMs        = sw.Elapsed.TotalMilliseconds,
            EstimatedCostUsd = Math.Round(estimatedCost, 6),
            Provider         = aiResponse.ProviderName,
            Model            = aiResponse.ModelName
        });
    }
}

/// <summary>DTO de solicitud para el endpoint test-generation.</summary>
public class AiTestGenerationRequestDto
{
    public string  Prompt          { get; set; } = string.Empty;
    public string? Provider        { get; set; }
    public string? Model           { get; set; }
    public double  Temperature     { get; set; } = 0.7;
    public int     MaxTokens       { get; set; } = 512;
    public bool    EnableStreaming  { get; set; } = false;
}
