using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Infrastructure.Repositories;

namespace OCAP.Workflow.Infrastructure.Services;

/// <summary>
/// Scheduler sin estado: las reanudaciones viven en WaitUntilUtc de WorkflowExecution.
/// </summary>
public sealed class EfWorkflowScheduler : IWorkflowScheduler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfWorkflowScheduler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public Task ScheduleResumeAsync(Guid executionId, Guid tenantId, DateTime resumeAtUtc, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<(Guid ExecutionId, Guid TenantId)>> GetDueResumesAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
        var due = await repository.GetDueDelayedExecutionsAsync(now, cancellationToken);
        return due.Select(e => (e.Id, e.TenantId)).ToList();
    }
}

public class WorkflowResumeHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowResumeHostedService> _logger;

    public WorkflowResumeHostedService(IServiceScopeFactory scopeFactory, ILogger<WorkflowResumeHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueExecutionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al procesar ejecuciones de workflow vencidas.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessDueExecutionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
        var engine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();

        var dueExecutions = await repository.GetDueDelayedExecutionsAsync(DateTime.UtcNow, cancellationToken);
        foreach (var execution in dueExecutions)
        {
            try
            {
                var context = new WorkflowContext
                {
                    TenantId = execution.TenantId,
                    UserId = execution.UserId,
                    AgentId = execution.AgentId,
                    ExecutionId = execution.Id,
                    ResumeSignal = execution.WaitSignal ?? NodeExecutionHints.DelaySignal
                };

                await engine.ResumeWithSignalAsync(
                    execution.Id,
                    execution.TenantId,
                    execution.WaitSignal ?? NodeExecutionHints.DelaySignal,
                    execution.ResumePayloadJson,
                    context,
                    cancellationToken);

                _logger.LogInformation("Reanudada ejecución de workflow {ExecutionId} por espera vencida.", execution.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reanudar ejecución {ExecutionId}", execution.Id);
            }
        }
    }
}
