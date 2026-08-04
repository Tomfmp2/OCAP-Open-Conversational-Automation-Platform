using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Workflow;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Designer.Models;
using OCAP.Workflow.Designer.DTOs;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowDesignerMapper _workflowDesignerMapper;
    private readonly OCAPDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public WorkflowsController(
        IWorkflowEngine workflowEngine,
        IWorkflowValidator workflowValidator,
        IWorkflowDesignerMapper workflowDesignerMapper,
        OCAPDbContext dbContext,
        ITenantContext tenantContext,
        IUserContext userContext)
    {
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        _workflowValidator = workflowValidator ?? throw new ArgumentNullException(nameof(workflowValidator));
        _workflowDesignerMapper = workflowDesignerMapper ?? throw new ArgumentNullException(nameof(workflowDesignerMapper));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkflowDefinitionDto>>> GetWorkflows(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowDefinitions.AsQueryable();

        if (tenantId != Guid.Empty)
        {
            query = query.Where(w => w.TenantId == tenantId);
        }

        var workflows = await query
            .Select(w => new WorkflowDefinitionDto(w.Id, w.TenantId, w.Name, w.Description, w.CurrentVersion, w.Status.ToString(), w.Steps.Count, w.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        
        return Ok(workflows);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> GetWorkflowById(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowDefinitions.AsQueryable();

        if (tenantId != Guid.Empty)
        {
            query = query.Where(x => x.TenantId == tenantId);
        }

        var w = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            
        if (w == null) return NotFound();

        var workflow = new WorkflowDefinitionDto(w.Id, w.TenantId, w.Name, w.Description, w.CurrentVersion, w.Status.ToString(), w.Steps.Count, w.CreatedAtUtc);
        return Ok(workflow);
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<object>> GetWorkflowStatus(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowDefinitions.AsQueryable();

        if (tenantId != Guid.Empty)
        {
            query = query.Where(x => x.TenantId == tenantId);
        }

        var w = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (w == null) return NotFound();

        var totalExec = await _dbContext.WorkflowExecutions.CountAsync(e => e.WorkflowDefinitionId == id, cancellationToken);
        var successExec = await _dbContext.WorkflowExecutions.CountAsync(e => e.WorkflowDefinitionId == id && e.Status == WorkflowStatus.Completed, cancellationToken);
        var failedExec = await _dbContext.WorkflowExecutions.CountAsync(e => e.WorkflowDefinitionId == id && e.Status == WorkflowStatus.Failed, cancellationToken);
        
        var rate = totalExec > 0 ? (double)successExec / totalExec * 100 : 0;
        var lastExec = await _dbContext.WorkflowExecutions
            .Where(e => e.WorkflowDefinitionId == id)
            .OrderByDescending(e => e.StartedAtUtc)
            .Select(e => e.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var status = new
        {
            WorkflowId = id,
            Status = w.Status.ToString(),
            TotalExecutions = totalExec,
            SuccessfulExecutions = successExec,
            FailedExecutions = failedExec,
            SuccessRatePercentage = Math.Round(rate, 2),
            LastExecutedAtUtc = lastExec == default ? (DateTime?)null : lastExec
        };
        return Ok(status);
    }

    [HttpGet("{id}/executions")]
    public async Task<ActionResult<List<WorkflowExecutionDto>>> GetExecutionsForWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var executions = await _dbContext.WorkflowExecutions
            .Where(e => e.WorkflowDefinitionId == id)
            .OrderByDescending(e => e.StartedAtUtc)
            .Select(e => new WorkflowExecutionDto(
                e.Id, e.WorkflowDefinitionId, e.TenantId, e.UserId, e.AgentId,
                e.CurrentStepId, e.Status.ToString(), e.StartedAtUtc, e.CompletedAtUtc, e.OutputJson, e.ErrorMessage))
            .ToListAsync(cancellationToken);

        return Ok(executions);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionDto>> CreateWorkflow([FromBody] CreateWorkflowRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "El nombre del workflow es obligatorio." });
        }

        var tenantId = Security.TenantSecurity.RequireTenantId(_tenantContext);
        var definition = new WorkflowDefinition(Guid.NewGuid(), tenantId, request.Name, request.Description);
        
        _dbContext.WorkflowDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new WorkflowDefinitionDto(definition.Id, definition.TenantId, definition.Name, definition.Description, definition.CurrentVersion, definition.Status.ToString(), definition.Steps.Count, definition.CreatedAtUtc);
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> UpdateWorkflow(Guid id, [FromBody] CreateWorkflowRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "El nombre del workflow es obligatorio." });
        }

        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowDefinitions.AsQueryable();

        if (tenantId != Guid.Empty)
        {
            query = query.Where(x => x.TenantId == tenantId);
        }

        var definition = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition == null) return NotFound();

        definition.UpdateDetails(request.Name, request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new WorkflowDefinitionDto(
            definition.Id,
            definition.TenantId,
            definition.Name,
            definition.Description,
            definition.CurrentVersion,
            definition.Status.ToString(),
            definition.Steps.Count,
            definition.CreatedAtUtc);

        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowDefinitions.AsQueryable();

        if (tenantId != Guid.Empty)
        {
            query = query.Where(x => x.TenantId == tenantId);
        }

        var definition = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition == null) return NotFound();

        _dbContext.WorkflowDefinitions.Remove(definition);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return Ok(new { message = $"Workflow {id} eliminado correctamente." });
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<WorkflowExecutionDto>> ExecuteWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = Security.TenantSecurity.RequireTenantId(_tenantContext);
        var userId = Security.TenantSecurity.RequireUserId(_userContext);

        var context = new WorkflowContext
        {
            TenantId = tenantId,
            UserId = userId
        };

        var execution = await _workflowEngine.StartWorkflowAsync(id, context, cancellationToken);
        var dto = new WorkflowExecutionDto(
            execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.UserId, execution.AgentId,
            execution.CurrentStepId, execution.Status.ToString(), execution.StartedAtUtc, execution.CompletedAtUtc, execution.OutputJson, execution.ErrorMessage
        );

        return Ok(dto);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<WorkflowExecutionDto>> CancelWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var execution = await _workflowEngine.CancelWorkflowAsync(id, cancellationToken);
        var dto = new WorkflowExecutionDto(
            execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.UserId, execution.AgentId,
            execution.CurrentStepId, execution.Status.ToString(), execution.StartedAtUtc, execution.CompletedAtUtc, execution.OutputJson, execution.ErrorMessage
        );

        return Ok(dto);
    }

    [HttpPost("executions/{executionId}/resume")]
    public async Task<ActionResult<WorkflowExecutionDto>> ResumeExecution(
        Guid executionId,
        [FromBody] ResumeWorkflowRequestDto? request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;
        var context = new WorkflowContext
        {
            TenantId = tenantId,
            UserId = userId
        };

        WorkflowExecution execution;
        if (!string.IsNullOrWhiteSpace(request?.Signal))
        {
            execution = await _workflowEngine.ResumeWithSignalAsync(
                executionId,
                tenantId,
                request.Signal,
                request.PayloadJson,
                context,
                cancellationToken);
        }
        else
        {
            execution = await _workflowEngine.ResumeWorkflowAsync(executionId, context, cancellationToken);
        }

        return Ok(ToExecutionDto(execution));
    }

    [HttpPost("executions/{executionId}/signal")]
    public async Task<ActionResult<WorkflowExecutionDto>> SignalExecution(
        Guid executionId,
        [FromBody] ResumeWorkflowRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Signal))
            return BadRequest(new { message = "Signal is required." });

        var context = new WorkflowContext
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId
        };

        var execution = await _workflowEngine.ResumeWithSignalAsync(
            executionId,
            _tenantContext.TenantId,
            request.Signal,
            request.PayloadJson,
            context,
            cancellationToken);

        return Ok(ToExecutionDto(execution));
    }

    [HttpGet("executions/{executionId}/history")]
    public async Task<ActionResult> GetExecutionHistory(Guid executionId, CancellationToken cancellationToken)
    {
        var history = await _workflowEngine.GetExecutionHistoryAsync(executionId, cancellationToken);
        return Ok(history.Select(h => new
        {
            h.Id,
            h.ExecutionId,
            h.StepId,
            h.StepName,
            h.NodeType,
            h.Status,
            h.DurationMs,
            h.InputJson,
            h.OutputJson,
            h.ErrorMessage,
            h.ExecutedAtUtc
        }));
    }

    [HttpGet("executions")]
    public async Task<ActionResult<List<WorkflowExecutionDto>>> GetExecutions(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowExecutions.AsQueryable();
        if (tenantId != Guid.Empty)
            query = query.Where(e => e.TenantId == tenantId);

        var executions = await query
            .OrderByDescending(e => e.StartedAtUtc)
            .Take(100)
            .Select(e => new WorkflowExecutionDto(
                e.Id, e.WorkflowDefinitionId, e.TenantId, e.UserId, e.AgentId,
                e.CurrentStepId, e.Status.ToString(), e.StartedAtUtc, e.CompletedAtUtc, e.OutputJson, e.ErrorMessage))
            .ToListAsync(cancellationToken);

        return Ok(executions);
    }

    [HttpGet("executions/{id}")]
    public async Task<ActionResult<WorkflowExecutionDto>> GetExecutionById(Guid id, CancellationToken cancellationToken)
    {
        var execution = await _workflowEngine.GetExecutionAsync(id, cancellationToken);
        if (execution == null)
        {
            return NotFound();
        }

        return Ok(ToExecutionDto(execution));
    }

    [HttpPost("designer/validate")]
    public ActionResult<WorkflowValidationResult> ValidateWorkflow([FromBody] VisualWorkflowGraph graph)
    {
        var result = _workflowValidator.Validate(graph);
        return Ok(result);
    }

    [HttpGet("{id}/designer")]
    public async Task<ActionResult<VisualWorkflowGraph>> GetDesignerGraph(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _dbContext.WorkflowDefinitions.AsQueryable();

        if (tenantId != Guid.Empty)
        {
            query = query.Where(x => x.TenantId == tenantId);
        }

        var definition = await query
            .Include(x => x.Steps)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition == null) return NotFound();

        return Ok(_workflowDesignerMapper.MapFromDomain(definition));
    }

    [HttpPost("designer/save")]
    public async Task<ActionResult<WorkflowDefinitionDto>> SaveWorkflow([FromBody] VisualWorkflowGraph graph, CancellationToken cancellationToken)
    {
        var validationResult = _workflowValidator.Validate(graph);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult);
        }

        var tenantId = Security.TenantSecurity.RequireTenantId(_tenantContext);
        var mapped = _workflowDesignerMapper.MapToDomain(graph, tenantId);

        WorkflowDefinition definition;
        if (Guid.TryParse(graph.Id, out var existingId) && existingId != Guid.Empty)
        {
            var query = _dbContext.WorkflowDefinitions.AsQueryable();
            if (tenantId != Guid.Empty)
            {
                query = query.Where(x => x.TenantId == tenantId);
            }

            var existing = await query.FirstOrDefaultAsync(x => x.Id == existingId, cancellationToken);
            if (existing != null)
            {
                _dbContext.WorkflowDefinitions.Remove(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        definition = mapped;
        _dbContext.WorkflowDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new WorkflowDefinitionDto(
            definition.Id,
            definition.TenantId,
            definition.Name,
            definition.Description,
            definition.CurrentVersion,
            definition.Status.ToString(),
            definition.Steps.Count,
            definition.CreatedAtUtc
        );

        return Ok(dto);
    }

    private static WorkflowExecutionDto ToExecutionDto(WorkflowExecution execution) => new(
        execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.UserId, execution.AgentId,
        execution.CurrentStepId, execution.Status.ToString(), execution.StartedAtUtc, execution.CompletedAtUtc,
        execution.OutputJson, execution.ErrorMessage);
}

public record ResumeWorkflowRequestDto(string? Signal, string? PayloadJson);
