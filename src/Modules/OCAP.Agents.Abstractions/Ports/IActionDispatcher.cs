using OCAP.Agents.Domain.Entities;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Abstractions.Ports;

// Puerto encargado del despacho y ejecución de acciones determinadas por el agente.
public interface IActionDispatcher
{
    // Despacha la ejecución de una acción hacia la herramienta correspondiente.
    Task<ToolExecutionResult> DispatchActionAsync(AgentAction action, CancellationToken cancellationToken = default);
}
