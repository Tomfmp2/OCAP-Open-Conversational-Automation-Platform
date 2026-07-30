namespace OCAP.Workflow.Abstractions;

public interface IWorkflowScheduler
{
    Task ScheduleResumeAsync(Guid executionId, Guid tenantId, DateTime resumeAtUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Guid ExecutionId, Guid TenantId)>> GetDueResumesAsync(DateTime now, CancellationToken cancellationToken = default);
}
