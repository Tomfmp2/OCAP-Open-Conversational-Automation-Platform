using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Services;

// Despachador de acciones encargada de coordinar la ejecución con IToolRegistry.
public class ActionDispatcher : IActionDispatcher
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ActionDispatcher> _logger;

    public ActionDispatcher(IToolRegistry toolRegistry, ILogger<ActionDispatcher> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> DispatchActionAsync(AgentAction action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            _logger.LogWarning("Se intentó despachar una acción nula.");
            return ToolExecutionResult.Fail("La acción a ejecutar es nula.");
        }

        _logger.LogInformation("Despachando acción de agente tipo {ActionType} (Tool: {ToolName})", action.ActionType, action.TargetToolName);

        if (!string.IsNullOrWhiteSpace(action.TargetToolName))
        {
            var tool = _toolRegistry.GetTool(action.TargetToolName);
            if (tool == null)
            {
                _logger.LogWarning("Herramienta solicitada {ToolName} no está registrada en IToolRegistry.", action.TargetToolName);
                return ToolExecutionResult.Fail($"La herramienta '{action.TargetToolName}' no se encuentra disponible.");
            }

            var args = action.Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return await tool.ExecuteAsync(args, cancellationToken);
        }

        // Si la acción no requiere herramientas externas (ej. respuesta directa de texto).
        return ToolExecutionResult.Ok($"Acción '{action.ActionType}' procesada internamente sin herramientas.");
    }
}
