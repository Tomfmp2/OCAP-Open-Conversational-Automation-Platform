using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Workflow;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Designer.Models;
using OCAP.Workflow.Designer.DTOs;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowDesignerMapper _workflowDesignerMapper;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowExecutionRepository _executions;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public WorkflowsController(
        IWorkflowEngine workflowEngine,
        IWorkflowValidator workflowValidator,
        IWorkflowDesignerMapper workflowDesignerMapper,
        IWorkflowDefinitionRepository definitions,
        IWorkflowExecutionRepository executions,
        ITenantContext tenantContext,
        IUserContext userContext)
    {
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        _workflowValidator = workflowValidator ?? throw new ArgumentNullException(nameof(workflowValidator));
        _workflowDesignerMapper = workflowDesignerMapper ?? throw new ArgumentNullException(nameof(workflowDesignerMapper));
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _executions = executions ?? throw new ArgumentNullException(nameof(executions));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkflowDefinitionDto>>> GetWorkflows(CancellationToken cancellationToken)
    {
        var workflows = await _definitions.GetByTenantAsync(_tenantContext.TenantId, cancellationToken);
        return Ok(workflows.Select(ToDefinitionDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> GetWorkflowById(Guid id, CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdAsync(id, _tenantContext.TenantId, cancellationToken);
        return definition is null ? NotFound() : Ok(ToDefinitionDto(definition));
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<object>> GetWorkflowStatus(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var definition = await _definitions.GetByIdAsync(id, tenantId, cancellationToken);
        if (definition is null) return NotFound();

        var executions = await _executions.GetByDefinitionAsync(id, tenantId, cancellationToken);
        var total = executions.Count;
        var successful = executions.Count(e => e.Status == WorkflowStatus.Completed);
        var rate = total > 0 ? (double)successful / total * 100 : 0;

        return Ok(new
        {
            WorkflowId = id,
            Status = definition.Status.ToString(),
            TotalExecutions = total,
            SuccessfulExecutions = successful,
            FailedExecutions = executions.Count(e => e.Status == WorkflowStatus.Failed),
            SuccessRatePercentage = Math.Round(rate, 2),
            LastExecutedAtUtc = executions.Count > 0 ? executions[0].StartedAtUtc : (DateTime?)null
        });
    }

    [HttpGet("{id}/executions")]
    public async Task<ActionResult<List<WorkflowExecutionDto>>> GetExecutionsForWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var executions = await _executions.GetByDefinitionAsync(id, _tenantContext.TenantId, cancellationToken);
        return Ok(executions.Select(ToExecutionDto).ToList());
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
        await _definitions.AddAsync(definition, cancellationToken);

        return Ok(ToDefinitionDto(definition));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> UpdateWorkflow(Guid id, [FromBody] CreateWorkflowRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "El nombre del workflow es obligatorio." });
        }

        var definition = await _definitions.GetByIdAsync(id, _tenantContext.TenantId, cancellationToken);
        if (definition == null) return NotFound();

        definition.UpdateDetails(request.Name, request.Description);
        await _definitions.UpdateAsync(definition, cancellationToken);

        return Ok(ToDefinitionDto(definition));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (await _definitions.GetByIdAsync(id, tenantId, cancellationToken) is null) return NotFound();

        await _definitions.DeleteAsync(id, tenantId, cancellationToken);
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
        return Ok(ToExecutionDto(execution));
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<WorkflowExecutionDto>> CancelWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var execution = await _workflowEngine.CancelWorkflowAsync(id, cancellationToken);
        return Ok(ToExecutionDto(execution));
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
        var executions = await _executions.GetRecentAsync(_tenantContext.TenantId, take: 100, cancellationToken);
        return Ok(executions.Select(ToExecutionDto).ToList());
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
        var definition = await _definitions.GetByIdAsync(id, _tenantContext.TenantId, cancellationToken);
        return definition is null ? NotFound() : Ok(_workflowDesignerMapper.MapFromDomain(definition));
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
        var definition = _workflowDesignerMapper.MapToDomain(graph, tenantId);

        // Guardar desde el diseñador reemplaza el grafo completo del workflow existente.
        if (Guid.TryParse(graph.Id, out var existingId) && existingId != Guid.Empty)
        {
            await _definitions.DeleteAsync(existingId, tenantId, cancellationToken);
        }

        await _definitions.AddAsync(definition, cancellationToken);
        return Ok(ToDefinitionDto(definition));
    }

    private static WorkflowDefinitionDto ToDefinitionDto(WorkflowDefinition definition) => new(
        definition.Id, definition.TenantId, definition.Name, definition.Description, definition.CurrentVersion,
        definition.Status.ToString(), definition.Steps.Count, definition.CreatedAtUtc);

    private static WorkflowExecutionDto ToExecutionDto(WorkflowExecution execution) => new(
        execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.UserId, execution.AgentId,
        execution.CurrentStepId, execution.Status.ToString(), execution.StartedAtUtc, execution.CompletedAtUtc,
        execution.OutputJson, execution.ErrorMessage);
}

public record ResumeWorkflowRequestDto(string? Signal, string? PayloadJson);
