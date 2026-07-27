using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Providers;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IAiProviderSelector _selector;

    public ProvidersController(IAiProviderSelector selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    [HttpGet]
    public ActionResult<List<ProviderInfoDto>> GetProviders()
    {
        var active = _selector.ActiveProviderName;
        var list = new List<ProviderInfoDto>
        {
            new("OpenAI", "gpt-4o", active.Equals("OpenAI", StringComparison.OrdinalIgnoreCase), 1),
            new("Gemini", "gemini-1.5-flash", active.Equals("Gemini", StringComparison.OrdinalIgnoreCase), 2),
            new("Ollama", "llama3", active.Equals("Ollama", StringComparison.OrdinalIgnoreCase), 3),
            new("MockAI", "mock-gpt-4o", active.Equals("MockAI", StringComparison.OrdinalIgnoreCase), 4)
        };
        return Ok(list);
    }

    [HttpGet("status")]
    public async Task<ActionResult<List<ProviderHealth>>> GetStatus(CancellationToken cancellationToken)
    {
        var health = await _selector.GetAllProviderHealthAsync(cancellationToken);
        return Ok(health);
    }

    [HttpGet("models")]
    public ActionResult<Dictionary<string, List<string>>> GetModels()
    {
        var dict = new Dictionary<string, List<string>>
        {
            ["OpenAI"] = new() { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo" },
            ["Gemini"] = new() { "gemini-1.5-pro", "gemini-1.5-flash", "gemini-1.0-pro" },
            ["Ollama"] = new() { "llama3", "mistral", "phi3", "codellama" },
            ["MockAI"] = new() { "mock-gpt-4o", "mock-gpt-3.5-turbo" }
        };
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

        // Estimación de costo simulada basada en tokens usados ($0.002 por cada 1K tokens)
        var estimatedCost = (response.TokensUsed / 1000.0) * 0.002;

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
