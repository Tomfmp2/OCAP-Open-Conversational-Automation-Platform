using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;
using OCAP.Core.Ports;
using OCAP.Core.Entities;

namespace OCAP.Agents.Application.Services;

// Despachador de acciones encargada de coordinar la ejecución con IToolRegistry e IPermissionValidator.
public class ActionDispatcher : IActionDispatcher
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IPermissionValidator _permissionValidator;
    private readonly ILogger<ActionDispatcher> _logger;
    private readonly IToolExecutionRepository? _repository;

    public ActionDispatcher(
        IToolRegistry toolRegistry,
        IPermissionValidator permissionValidator,
        ILogger<ActionDispatcher> logger,
        IToolExecutionRepository? repository = null)
    {
        _toolRegistry = toolRegistry;
        _permissionValidator = permissionValidator;
        _logger = logger;
        _repository = repository;
    }

    public async Task<ToolResult> DispatchActionAsync(
        Guid agentId,
        Guid userId,
        Guid conversationId,
        AgentAction action,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            _logger.LogWarning("Se intentó despachar una acción nula.");
            return ToolResult.Fail("INVALID_ACTION", "La acción a ejecutar es nula.");
        }

        _logger.LogInformation("Despachando acción {ActionType} para agente {AgentId} (Tool: {ToolName})", action.ActionType, agentId, action.TargetToolName);

        if (!string.IsNullOrWhiteSpace(action.TargetToolName))
        {
            var tool = _toolRegistry.GetTool(action.TargetToolName);
            if (tool == null)
            {
                _logger.LogWarning("Herramienta solicitada '{ToolName}' no está registrada en IToolRegistry.", action.TargetToolName);
                return ToolResult.Fail("TOOL_NOT_FOUND", $"La herramienta '{action.TargetToolName}' no se encuentra disponible.");
            }

            // 1. Validar permisos del agente sobre la herramienta
            var canExecute = await _permissionValidator.CanExecuteToolAsync(agentId, tool, cancellationToken);
            if (!canExecute)
            {
                _logger.LogWarning("Ejecución denegada para agente {AgentId} sobre herramienta '{ToolName}'.", agentId, action.TargetToolName);
                return ToolResult.Fail("PERMISSION_DENIED", $"El agente no dispone de permisos para ejecutar '{action.TargetToolName}'.");
            }

            // 2. Construir el contexto de ejecución inmutable
            var parameters = action.Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var execContext = new ToolExecutionContext(agentId, userId, conversationId, parameters);

            // 3. Ejecutar la herramienta
            var result = await tool.ExecuteAsync(execContext, cancellationToken);

            if (_repository != null)
            {
                var toolExecution = new ToolExecution(
                    Guid.NewGuid(),
                    agentId,
                    userId,
                    conversationId,
                    action.TargetToolName,
                    result.Success,
                    result.Success ? null : result.ErrorCode
                );
                await _repository.SaveAsync(toolExecution, cancellationToken);
            }

            return result;
        }

        return ToolResult.Ok(null, $"Acción '{action.ActionType}' procesada internamente sin herramientas.");
    }
}
