using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Intelligence.Application.Services;

/// <summary>
/// Orquestador de proveedores de IA. Failover evita proveedores sin credenciales.
/// </summary>
public class AiProviderSelector : IAiProviderSelector
{
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly IAiResponseCache _cache;
    private readonly ILogger<AiProviderSelector> _logger;
    private string _activeProviderName = "Gemini";

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
        _activeProviderName = providerName.Trim();
        _logger.LogInformation("Proveedor de IA activo cambiado manualmente a: {ProviderName}", _activeProviderName);
    }

    public Task<IAiProvider> SelectProviderAsync(CancellationToken cancellationToken = default)
    {
        var provider = _providers.FirstOrDefault(p =>
            p.Name.Equals(_activeProviderName, StringComparison.OrdinalIgnoreCase));
        if (provider != null) return Task.FromResult(provider);

        provider = _providers.FirstOrDefault(p => p.Name.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                ?? _providers.FirstOrDefault(p => p.Name.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                ?? _providers.FirstOrDefault(p => p.Name.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
                ?? _providers.FirstOrDefault(p => !p.Name.Equals("Mock", StringComparison.OrdinalIgnoreCase))
                ?? _providers.First();

        return Task.FromResult(provider);
    }

    public async Task<AiResponse> ExecuteWithFailoverAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var cacheKey = $"{_activeProviderName}:{request.UserMessage}";
        var cachedResponse = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Respuesta de IA obtenida de la memoria Caché (TTL Activo).");
            return cachedResponse;
        }

        var primaryProvider = await SelectProviderAsync(cancellationToken);
        var fallbackProviders = _providers
            .Where(p => !p.Name.Equals(primaryProvider.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var orderedCandidates = new List<IAiProvider> { primaryProvider };
        orderedCandidates.AddRange(fallbackProviders);

        Exception? lastException = null;

        foreach (var candidate in orderedCandidates)
        {
            try
            {
                _logger.LogInformation("Ejecutando solicitud de IA en el proveedor: {ProviderName}", candidate.Name);
                var response = await candidate.GenerateResponseAsync(request, cancellationToken);
                await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);
                return response;
            }
            catch (Exception ex) when (IsMissingCredential(ex))
            {
                lastException = ex;
                _logger.LogDebug(ex, "Proveedor {ProviderName} sin credenciales; se omite.", candidate.Name);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Falla en proveedor de IA {ProviderName}. Failover…", candidate.Name);
            }
        }

        throw new InvalidOperationException(
            "Todos los proveedores de IA configurados fallaron. Revisa API keys y el proveedor preferido.",
            lastException);
    }

    public async IAsyncEnumerable<string> StreamWithFailoverAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var provider = await SelectProviderAsync(cancellationToken);

        IAsyncEnumerable<string>? stream;
        try
        {
            stream = provider.StreamResponseAsync(request, cancellationToken);
        }
        catch
        {
            var fallback = _providers.FirstOrDefault(p =>
                              !p.Name.Equals(provider.Name, StringComparison.OrdinalIgnoreCase) &&
                              !p.Name.Equals("Mock", StringComparison.OrdinalIgnoreCase))
                          ?? _providers.First();
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
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linked.CancelAfter(TimeSpan.FromSeconds(
                    provider.Name.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
                    provider.Name.Equals("Local", StringComparison.OrdinalIgnoreCase)
                        ? 3
                        : 12));

                var health = await provider.HealthAsync(linked.Token);
                healthList.Add(health);
            }
            catch (Exception ex)
            {
                healthList.Add(new ProviderHealth
                {
                    ProviderName = provider.Name,
                    IsHealthy = false,
                    LatencyMs = 0,
                    StatusMessage = ex is OperationCanceledException
                        ? "Timeout de health check"
                        : ex.Message
                });
            }
        }

        return healthList;
    }

    private static bool IsMissingCredential(Exception ex) =>
        ex.Message.Contains("API Key", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("no se ha configurado", StringComparison.OrdinalIgnoreCase);
}
