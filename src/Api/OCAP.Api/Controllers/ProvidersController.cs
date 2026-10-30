using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Providers;
using OCAP.Api.Services;
using OCAP.Intelligence.Abstractions;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IAiProviderSelector _selector;
    private readonly IAiProviderRegistry _registry;
    private readonly IAiProviderConfigurationService _configurations;
    private readonly ITenantContext _tenantContext;
    private readonly OCAPDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly TenantAiProviderPreference _preference;

    public ProvidersController(
        IAiProviderSelector selector,
        IAiProviderRegistry registry,
        IAiProviderConfigurationService configurations,
        ITenantContext tenantContext,
        OCAPDbContext dbContext,
        IConfiguration configuration,
        TenantAiProviderPreference preference)
    {
        _preference = preference ?? throw new ArgumentNullException(nameof(preference));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProviderInfoDto>>> GetProviders(CancellationToken cancellationToken)
    {
        var active = await _preference.GetActiveProviderNameAsync(cancellationToken);
        var configs = await _dbContext.AiProviderConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.ProviderName)
            .ToListAsync(cancellationToken);

        // El predeterminado se guarda como id de configuración; los guardados antes como nombre de proveedor marcan la
        // primera configuración de ese proveedor, que es la que se usa en tiempo de ejecución.
        var activeId = configs.FirstOrDefault(c => c.Id.ToString() == active)?.Id
            ?? configs.FirstOrDefault(c => c.ProviderName.Equals(active, StringComparison.OrdinalIgnoreCase))?.Id;
        var list = configs
            .Select((c, i) => new ProviderInfoDto(c.ProviderName, c.ModelName, c.Id == activeId, i + 1, c.Id))
            .ToList();

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
    [Authorize(Policy = OcapPermissions.SettingsManage)]
    public async Task<IActionResult> SelectProvider([FromBody] SelectProviderRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderName))
            return BadRequest(new { message = "El nombre del proveedor es requerido." });

        // Un id de configuración (varias por proveedor) o, como antes, un nombre de proveedor.
        var name = request.ProviderName.Trim();
        var configured = Guid.TryParse(name, out var configId)
            ? await _dbContext.AiProviderConfigurations.AnyAsync(c => c.IsEnabled && c.Id == configId, cancellationToken)
            : await _dbContext.AiProviderConfigurations.AnyAsync(c => c.IsEnabled && c.ProviderName.ToLower() == name.ToLower(), cancellationToken);
        if (!configured && _registry.GetProvider(name) is null)
            return BadRequest(new { message = $"Proveedor '{name}' no está registrado ni configurado." });

        if (!await _preference.SetActiveProviderNameAsync(name, cancellationToken))
            return NotFound(new { message = "Organización no encontrada." });

        return Ok(new { message = $"Proveedor activo actualizado correctamente a '{name}'." });
    }

    /// <summary>
    /// Prueba UN proveedor concreto (vault del tenant o estático). Sin failover a Claude/Ollama/etc.
    /// </summary>
    [HttpPost("test")]
    [Authorize(Policy = OcapPermissions.SettingsManage)]
    public async Task<ActionResult<TestProviderResponseDto>> TestProvider(
        [FromBody] TestProviderRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { message = "El prompt no puede ser vacío." });

        var providerName = string.IsNullOrWhiteSpace(request.ProviderName)
            ? (_configuration["AiProviders:PreferredProvider"] ?? await _preference.GetActiveProviderNameAsync(cancellationToken))
            : request.ProviderName.Trim();

        IAiProvider? provider = null;
        var source = "static";

        try
        {
            provider = await _configurations.GetRuntimeProviderForTenantAsync(
                _tenantContext.TenantId,
                providerName,
                cancellationToken);
            if (provider != null)
                source = "tenant-vault";
        }
        catch
        {
            // cae a estático
        }

        provider ??= _registry.GetProvider(providerName);
        if (provider is null)
        {
            return BadRequest(new
            {
                message = $"Proveedor '{providerName}' no está registrado. Regístralo en IA y modelos o en .env."
            });
        }

        var aiRequest = new AiRequest
        {
            UserMessage = request.Prompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens ?? 64,
            SystemInstructions = "Responde de forma breve."
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await provider.GenerateResponseAsync(aiRequest, cancellationToken);
            stopwatch.Stop();

            return Ok(new TestProviderResponseDto(
                ProviderUsed: $"{response.ProviderName} ({source})",
                ModelUsed: response.ModelName,
                GeneratedText: response.GeneratedText,
                TokensUsed: response.TokensUsed,
                LatencyMs: stopwatch.Elapsed.TotalMilliseconds,
                EstimatedCostUsd: 0,
                FromCache: false
            ));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new
            {
                message = $"Timeout al llamar a {providerName}. Revisa red hacia el proveedor o la API key.",
                provider = providerName,
                source,
                latencyMs = stopwatch.Elapsed.TotalMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new
            {
                message = ex.Message,
                provider = providerName,
                source,
                latencyMs = stopwatch.Elapsed.TotalMilliseconds
            });
        }
    }

    [HttpPost("stream")]
    [Authorize(Policy = OcapPermissions.AiExecute)]
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
