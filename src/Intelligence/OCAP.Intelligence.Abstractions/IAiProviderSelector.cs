namespace OCAP.Intelligence.Abstractions;

// Selector y orquestador inteligente para resolver y conmutar proveedores de IA en tiempo de ejecución.
public interface IAiProviderSelector
{
    // Selecciona el proveedor más adecuado según política, prioridad y salud.
    Task<IAiProvider> SelectProviderAsync(CancellationToken cancellationToken = default);

    // Ejecuta una petición contra el proveedor seleccionado con reintentos y conmutación por error (Failover).
    Task<AiResponse> ExecuteWithFailoverAsync(AiRequest request, CancellationToken cancellationToken = default);

    // Ejecuta una petición en flujo continuo (Streaming) con conmutación por error (Failover).
    IAsyncEnumerable<string> StreamWithFailoverAsync(AiRequest request, CancellationToken cancellationToken = default);

    // Obtiene el estado de salud de todos los proveedores registrados.
    Task<IReadOnlyList<ProviderHealth>> GetAllProviderHealthAsync(CancellationToken cancellationToken = default);

    // Establece el proveedor activo manualmente.
    void SetActiveProvider(string providerName);

    // Obtiene el nombre del proveedor activo actual.
    string ActiveProviderName { get; }
}
