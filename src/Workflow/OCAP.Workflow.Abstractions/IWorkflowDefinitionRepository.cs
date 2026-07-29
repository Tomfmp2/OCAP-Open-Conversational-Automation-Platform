using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Abstractions;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}
