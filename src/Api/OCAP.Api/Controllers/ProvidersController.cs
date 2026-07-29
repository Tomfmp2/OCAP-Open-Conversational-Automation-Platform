using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Providers;
using OCAP.Intelligence.Abstractions;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IAiProviderSelector _selector;
    private readonly IAiProviderRegistry _registry;
    private readonly OCAPDbContext _dbContext;

    public ProvidersController(IAiProviderSelector selector, IAiProviderRegistry registry, OCAPDbContext dbContext)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProviderInfoDto>>> GetProviders(CancellationToken cancellationToken)
    {
        var active = _selector.ActiveProviderName;
        var configs = await _dbContext.AiProviderConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.ProviderName)
            .ToListAsync(cancellationToken);

        var list = configs
            .GroupBy(c => c.ProviderName)
            .Select((g, i) => 
            {
                var c = g.First();
                return new ProviderInfoDto(
                    c.ProviderName, 
                    c.ModelName, 
                    active.Equals(c.ProviderName, StringComparison.OrdinalIgnoreCase), 
                    i + 1);
            }).ToList();

        if (!list.Any())
        {
            var registeredNames = _registry.GetRegisteredProviderNames();
            list = registeredNames.Select((name, i) => 
                new ProviderInfoDto(name, "default", active.Equals(name, StringComparison.OrdinalIgnoreCase), i + 1)).ToList();
        }

        return Ok(list);
    }

    [HttpGet("status")]
    public async Task<ActionResult<List<ProviderHealth>>> GetStatus(CancellationToken cancellationToken)
    {
        var health = await _selector.GetAllProviderHealthAsync(cancellationToken);
        return Ok(health);
    }

    [HttpGet("models")]
    public async Task<ActionResult<Dictionary<string, List<string>>>> GetModels(CancellationToken cancellationToken)
    {
        var dict = new Dictionary<string, List<string>>();
        var names = _registry.GetRegisteredProviderNames();
        
        foreach (var name in names)
        {
            var provider = _registry.GetProvider(name);
            if (provider != null)
            {
                try
                {
                    var models = await provider.GetAvailableModelsAsync(cancellationToken);
                    dict[name] = models.ToList();
                }
                catch
                {
                    dict[name] = new List<string>();
                }
            }
        }
        
        return Ok(dict);
    }

    [HttpPost("select")]
    public IActionResult SelectProvider([FromBody] SelectProviderRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderName)) return BadRequest(new { message = "El nombre del proveedor es requerido." });

        _selector.SetActiveProvider(request.ProviderName);
        return Ok(new { message = $"Proveedor activo actualizado correctamente a '{_selector.ActiveProviderName}'." });
    }

    [HttpPost("test")]
    public async Task<ActionResult<TestProviderResponseDto>> TestProvider([FromBody] TestProviderRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { message = "El prompt no puede ser vacío." });

        if (!string.IsNullOrWhiteSpace(request.ProviderName))
        {
            _selector.SetActiveProvider(request.ProviderName);
        }

        var aiRequest = new AiRequest
        {
            UserMessage = request.Prompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            SystemInstructions = "Eres el asistente inteligente oficial de OCAP."
        };

        var stopwatch = Stopwatch.StartNew();
        var response = await _selector.ExecuteWithFailoverAsync(aiRequest, cancellationToken);
        stopwatch.Stop();

        double estimatedCost = 0;
        
        var config = await _dbContext.AiProviderConfigurations
            .FirstOrDefaultAsync(c => c.ProviderName == response.ProviderName && c.IsEnabled, cancellationToken);

        if (config != null && !string.IsNullOrWhiteSpace(config.SettingsJson))
        {
            try 
            {
                var settings = System.Text.Json.JsonDocument.Parse(config.SettingsJson);
                if (settings.RootElement.TryGetProperty("CostPer1kTokens", out var costProp) && costProp.TryGetDouble(out var costPer1k))
                {
                    estimatedCost = (response.TokensUsed / 1000.0) * costPer1k;
                }
            }
            catch { /* Ignore parsing errors */ }
        }

        var dto = new TestProviderResponseDto(
            ProviderUsed: response.ProviderName,
            ModelUsed: response.ModelName,
            GeneratedText: response.GeneratedText,
            TokensUsed: response.TokensUsed,
            LatencyMs: stopwatch.Elapsed.TotalMilliseconds,
            EstimatedCostUsd: estimatedCost,
            FromCache: response.Metadata.ContainsKey("FromCache")
        );

        return Ok(dto);
    }

    [HttpPost("stream")]
    public async Task StreamResponse([FromBody] TestProviderRequestDto request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";

        var aiRequest = new AiRequest
        {
            UserMessage = request.Prompt,
            EnableStreaming = true,
            SystemInstructions = "Eres el asistente inteligente oficial de OCAP."
        };

        await foreach (var chunk in _selector.StreamWithFailoverAsync(aiRequest, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
