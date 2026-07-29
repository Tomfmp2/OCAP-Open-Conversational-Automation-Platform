using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Abstractions;

public interface IAiExecutionLogRepository
{
    Task SaveAsync(AiExecutionLog log, CancellationToken cancellationToken = default);
}
