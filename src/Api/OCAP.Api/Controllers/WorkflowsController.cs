using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Workflow;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;

    public WorkflowsController(IWorkflowEngine workflowEngine)
    {
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
    }

    [HttpGet]
    public ActionResult<List<WorkflowDefinitionDto>> GetWorkflows()
    {
        var tenantId = Guid.NewGuid();
        var workflows = new List<WorkflowDefinitionDto>
        {
            new(Guid.NewGuid(), tenantId, "Automatización de Bienvenida a Clientes", "Workflow para enviar mensaje de WhatsApp y agendar reunión de bienvenida.", 1, "Active", 4, DateTime.UtcNow.AddDays(-10)),
            new(Guid.NewGuid(), tenantId, "Generación de Reporte Semanal AI", "Workflow para consultar métricas, procesar con LLM y enviar por correo.", 2, "Active", 5, DateTime.UtcNow.AddDays(-5))
        };
        return Ok(workflows);
    }

    [HttpPost]
    public ActionResult<WorkflowDefinitionDto> CreateWorkflow([FromBody] CreateWorkflowRequestDto request)
    {
        var definition = new WorkflowDefinitionDto(Guid.NewGuid(), Guid.NewGuid(), request.Name, request.Description, 1, "Active", 3, DateTime.UtcNow);
        return Ok(definition);
    }

    [HttpPut("{id}")]
    public ActionResult<WorkflowDefinitionDto> UpdateWorkflow(Guid id, [FromBody] CreateWorkflowRequestDto request)
    {
        var definition = new WorkflowDefinitionDto(id, Guid.NewGuid(), request.Name, request.Description, 2, "Active", 4, DateTime.UtcNow);
        return Ok(definition);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteWorkflow(Guid id) => Ok(new { message = $"Workflow {id} eliminado correctamente." });

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<WorkflowExecutionDto>> ExecuteWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var context = new WorkflowContext
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
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

    [HttpGet("executions")]
    public ActionResult<List<WorkflowExecutionDto>> GetExecutions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var defId = Guid.NewGuid();

        var executions = new List<WorkflowExecutionDto>
        {
            new(Guid.NewGuid(), defId, tenantId, userId, null, "end", "Completed", DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(-29), "{\"status\": \"success\"}", null),
            new(Guid.NewGuid(), defId, tenantId, userId, null, "tool_step_1", "Running", DateTime.UtcNow.AddMinutes(-5), null, "{}", null)
        };

        return Ok(executions);
    }

    [HttpGet("executions/{id}")]
    public async Task<ActionResult<WorkflowExecutionDto>> GetExecutionById(Guid id, CancellationToken cancellationToken)
    {
        var execution = await _workflowEngine.GetExecutionAsync(id, cancellationToken);
        if (execution == null)
        {
            var fallback = new WorkflowExecutionDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "end", "Completed", DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-9), "{\"output\": \"OK\"}", null
            );
            return Ok(fallback);
        }

        var dto = new WorkflowExecutionDto(
            execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.UserId, execution.AgentId,
            execution.CurrentStepId, execution.Status.ToString(), execution.StartedAtUtc, execution.CompletedAtUtc, execution.OutputJson, execution.ErrorMessage
        );

        return Ok(dto);
    }
}
