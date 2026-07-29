using Microsoft.Extensions.Logging;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Application.Services;

// Servicio de auditoría y rastreo de consumo de tokens y ejecuciones de IA Generativa.
public class AiUsageTracker : IAiUsageTracker
{
    private readonly IAiExecutionLogRepository? _repository;
    private readonly ILogger<AiUsageTracker> _logger;

    public AiUsageTracker(ILogger<AiUsageTracker> logger, IAiExecutionLogRepository? repository = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository;
    }

    public async Task TrackUsageAsync(Guid userId, Guid agentId, string provider, string model, int tokens, bool success, CancellationToken cancellationToken = default)
    {
        var log = new AiExecutionLog(Guid.NewGuid(), provider, model, tokens, 120.0, success);
        if (_repository != null)
        {
            await _repository.SaveAsync(log, cancellationToken);
        }

        _logger.LogInformation("Auditoría IA: Usuario {UserId}, Agente {AgentId}, Proveedor {Provider}, Modelo {Model}, Tokens {Tokens}, Éxito {Success}",
            userId, agentId, provider, model, tokens, success);
    }
}
