using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Abstractions;

public interface IWorkflowExecutionRepository
{
    Task<WorkflowExecution?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecution>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    /// <summary>Ejecuciones de un workflow de la organización, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<WorkflowExecution>> GetByDefinitionAsync(Guid definitionId, Guid tenantId, CancellationToken cancellationToken = default);
    /// <summary>Las <paramref name="take"/> ejecuciones más recientes de la organización.</summary>
    Task<IReadOnlyList<WorkflowExecution>> GetRecentAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecution>> GetDueDelayedExecutionsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);

    Task AddHistoryAsync(WorkflowExecutionHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionHistory>> GetHistoryAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowVariable>> GetVariablesAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task SetVariablesAsync(Guid executionId, IEnumerable<WorkflowVariable> variables, CancellationToken cancellationToken = default);
}
