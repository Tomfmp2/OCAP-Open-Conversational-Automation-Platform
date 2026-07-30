using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Intelligence.Abstractions;
using OCAP.Infrastructure.Persistence.Context;
using System.Diagnostics;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiProvider _aiProvider;
    private readonly IAiProviderSelector _selector;
    private readonly IAiProviderRegistry _registry;
    private readonly OCAPDbContext _dbContext;

    public AiController(
        IAiProvider aiProvider, 
        IAiProviderSelector selector,
        IAiProviderRegistry registry,
        OCAPDbContext dbContext)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("status")]
    public ActionResult<AiStatusDto> GetStatus()
    {
        var activeName = _selector.ActiveProviderName;
        var activeProvider = _registry.GetProvider(activeName);
        var modelInfo = activeProvider?.GetModelInformation() ?? _aiProvider.GetModelInformation();
        
        var dto = new AiStatusDto(
            ActiveProvider: modelInfo.Provider,
            ActiveModel: modelInfo.Model,
            Status: "Online",
            LastCheckedUtc: DateTime.UtcNow
        );
        return Ok(dto);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<AiUsageDto>> GetUsage(CancellationToken cancellationToken)
    {
        var logs = await _dbContext.AiExecutionLogs.ToListAsync(cancellationToken);
        
        var totalTokens = logs.Sum(l => l.Tokens);
        var totalExecutions = logs.Count;
        var avgLatency = logs.Any() ? logs.Average(l => l.ExecutionTimeMs) : 0.0;
        var successCount = logs.Count(l => l.Success);
        var successRate = logs.Any() ? (successCount / (double)totalExecutions) * 100.0 : 100.0;

        var dto = new AiUsageDto(
            TotalTokensUsed: totalTokens,
            TotalExecutionsCount: totalExecutions,
            AverageLatencyMs: avgLatency,
            SuccessRatePercentage: successRate
        );
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
        provider ??= _registry.GetProvider(_selector.ActiveProviderName) ?? _aiProvider;

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
        catch (Exception ex)
        {
            return StatusCode(502, new { message = $"Error al generar respuesta del proveedor '{dto.Provider}': {ex.Message}" });
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
