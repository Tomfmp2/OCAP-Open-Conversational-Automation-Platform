using Microsoft.Extensions.Caching.Memory;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Intelligence.Application.Services;

// Implementación en memoria para el almacenamiento en caché de respuestas de IA (preparada para migración transparente a Redis).
public class InMemoryAiResponseCache : IAiResponseCache
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryAiResponseCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public Task<AiResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(cacheKey, out AiResponse? cached) && cached != null)
        {
            return Task.FromResult<AiResponse?>(cached);
        }

        return Task.FromResult<AiResponse?>(null);
    }

    public Task SetAsync(string cacheKey, AiResponse response, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (response == null) return Task.CompletedTask;

        _memoryCache.Set(cacheKey, response, ttl);
        return Task.CompletedTask;
    }
}
