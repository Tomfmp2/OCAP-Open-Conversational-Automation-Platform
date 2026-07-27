namespace OCAP.Intelligence.Abstractions;

// Contrato para el rastreo y auditoría del consumo de tokens y ejecuciones de modelos de IA.
public interface IAiUsageTracker
{
    // Registra métricas de uso de IA Generativa por usuario y agente.
    Task TrackUsageAsync(Guid userId, Guid agentId, string provider, string model, int tokens, bool success, CancellationToken cancellationToken = default);
}
