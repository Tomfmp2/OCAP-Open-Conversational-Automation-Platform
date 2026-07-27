using Microsoft.Extensions.Logging;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Application.Services;

// Servicio de auditoría y rastreo de consumo de tokens y ejecuciones de IA Generativa.
public class AiUsageTracker : IAiUsageTracker
{
    private readonly List<AiExecutionLog> _logs = new();
    private readonly ILogger<AiUsageTracker> _logger;

    public AiUsageTracker(ILogger<AiUsageTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task TrackUsageAsync(Guid userId, Guid agentId, string provider, string model, int tokens, bool success, CancellationToken cancellationToken = default)
    {
        var log = new AiExecutionLog(Guid.NewGuid(), provider, model, tokens, 120.0, success);
        _logs.Add(log);

        _logger.LogInformation("Auditoría IA: Usuario {UserId}, Agente {AgentId}, Proveedor {Provider}, Modelo {Model}, Tokens {Tokens}, Éxito {Success}",
            userId, agentId, provider, model, tokens, success);

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<AiExecutionLog> GetLogs() => _logs.AsReadOnly();
}
