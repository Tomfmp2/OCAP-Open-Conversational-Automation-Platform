namespace OCAP.Intelligence.Domain;

// Entidad que audita el uso, rendimiento y consumo de tokens en las ejecuciones de IA Generativa.
public class AiExecutionLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Tokens { get; private set; }
    public double ExecutionTimeMs { get; private set; }
    public bool Success { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    private AiExecutionLog() { } // Constructor ORM

    public AiExecutionLog(Guid id, string provider, string model, int tokens, double executionTimeMs, bool success, Guid tenantId = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de registro de IA no puede ser vacío.", nameof(id));

        Id = id;
        TenantId = tenantId;
        Provider = provider ?? string.Empty;
        Model = model ?? string.Empty;
        Tokens = tokens;
        ExecutionTimeMs = executionTimeMs;
        Success = success;
        ExecutedAt = DateTime.UtcNow;
    }
}
