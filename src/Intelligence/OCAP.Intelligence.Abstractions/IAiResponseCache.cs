namespace OCAP.Intelligence.Abstractions;

// Abstracción agnóstica para el almacenamiento en caché de respuestas de IA (preparado para In-Memory y Redis).
public interface IAiResponseCache
{
    // Obtiene una respuesta almacenada en caché si existe y no ha expirado.
    Task<AiResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);

    // Almacena una respuesta en caché con un tiempo de vida (TTL) determinado.
    Task SetAsync(string cacheKey, AiResponse response, TimeSpan ttl, CancellationToken cancellationToken = default);
}
