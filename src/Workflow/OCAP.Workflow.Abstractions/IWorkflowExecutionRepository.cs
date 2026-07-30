using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Abstractions;

public interface IWorkflowExecutionRepository
{
    Task<WorkflowExecution?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecution>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecution>> GetDueDelayedExecutionsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);

    Task AddHistoryAsync(WorkflowExecutionHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionHistory>> GetHistoryAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowVariable>> GetVariablesAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task SetVariablesAsync(Guid executionId, IEnumerable<WorkflowVariable> variables, CancellationToken cancellationToken = default);
}
