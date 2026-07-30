namespace OCAP.Workflow.Abstractions;

public interface IWorkflowDatabaseExecutor
{
    Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(
        Guid tenantId,
        string sql,
        IDictionary<string, object?>? parameters,
        CancellationToken cancellationToken = default);
}
