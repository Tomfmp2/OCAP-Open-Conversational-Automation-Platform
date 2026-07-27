using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Intelligence.Application.Services;

// Orquestador inteligente de proveedores de IA con selección dinámica, conmutación por error (Failover) y reintentos (Retries).
public class AiProviderSelector : IAiProviderSelector
{
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly IAiResponseCache _cache;
    private readonly ILogger<AiProviderSelector> _logger;
    private string _activeProviderName = "OpenAI";

    public string ActiveProviderName => _activeProviderName;

    public AiProviderSelector(
        IEnumerable<IAiProvider> providers,
        IAiResponseCache cache,
        ILogger<AiProviderSelector> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void SetActiveProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName)) return;
        _activeProviderName = providerName;
        _logger.LogInformation("Proveedor de IA activo cambiado manualmente a: {ProviderName}", providerName);
    }

    public Task<IAiProvider> SelectProviderAsync(CancellationToken cancellationToken = default)
    {
        // 1. Intentar seleccionar el proveedor activo actual
        var provider = _providers.FirstOrDefault(p => p.Name.Equals(_activeProviderName, StringComparison.OrdinalIgnoreCase));
        if (provider != null) return Task.FromResult(provider);

        // 2. Si el activo no está registrado, seleccionar OpenAI -> Gemini -> Ollama -> MockAI como fallback
        provider = _providers.FirstOrDefault(p => p.Name.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                ?? _providers.FirstOrDefault(p => p.Name.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                ?? _providers.FirstOrDefault(p => p.Name.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
                ?? _providers.First();

        return Task.FromResult(provider);
    }

    public async Task<AiResponse> ExecuteWithFailoverAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // 1. Verificar caché si hay un mensaje idéntico
        var cacheKey = $"{_activeProviderName}:{request.UserMessage}";
        var cachedResponse = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Respuesta de IA obtenida de la memoria Caché (TTL Activo).");
            return cachedResponse;
        }

        // 2. Lista ordenada de proveedores para failover
        var primaryProvider = await SelectProviderAsync(cancellationToken);
        var fallbackProviders = _providers.Where(p => p.Name != primaryProvider.Name).ToList();

        var orderedCandidates = new List<IAiProvider> { primaryProvider };
        orderedCandidates.AddRange(fallbackProviders);

        Exception? lastException = null;

        foreach (var candidate in orderedCandidates)
        {
            try
            {
                _logger.LogInformation("Ejecutando solicitud de IA en el proveedor: {ProviderName}", candidate.Name);
                var response = await candidate.GenerateResponseAsync(request, cancellationToken);

                // Guardar en caché con TTL de 5 minutos
                await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Falla en proveedor de IA {ProviderName}. Iniciando conmutación por error (Failover)...", candidate.Name);
            }
        }

        throw new InvalidOperationException("Todos los proveedores de IA fallaron durante la ejecución con Failover.", lastException);
    }

    public async IAsyncEnumerable<string> StreamWithFailoverAsync(AiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var provider = await SelectProviderAsync(cancellationToken);

        IAsyncEnumerable<string>? stream = null;
        try
        {
            stream = provider.StreamResponseAsync(request, cancellationToken);
        }
        catch
        {
            var fallback = _providers.FirstOrDefault(p => p.Name != provider.Name) ?? _providers.First();
            stream = fallback.StreamResponseAsync(request, cancellationToken);
        }

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<IReadOnlyList<ProviderHealth>> GetAllProviderHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthList = new List<ProviderHealth>();

        foreach (var provider in _providers)
        {
            try
            {
                var health = await provider.HealthAsync(cancellationToken);
                healthList.Add(health);
            }
            catch (Exception ex)
            {
                healthList.Add(new ProviderHealth
                {
                    ProviderName = provider.Name,
                    IsHealthy = false,
                    LatencyMs = 999.0,
                    StatusMessage = ex.Message
                });
            }
        }

        return healthList;
    }
}
