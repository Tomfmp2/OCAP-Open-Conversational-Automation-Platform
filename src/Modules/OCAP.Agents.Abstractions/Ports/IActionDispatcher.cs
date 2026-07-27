using OCAP.Agents.Domain.Entities;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Abstractions.Ports;

// Puerto encargado del despacho y ejecución de acciones determinadas por el agente con validación de permisos.
public interface IActionDispatcher
{
    // Despacha la ejecución de una acción hacia la herramienta correspondiente validando permisos.
    Task<ToolResult> DispatchActionAsync(Guid agentId, Guid userId, Guid conversationId, AgentAction action, CancellationToken cancellationToken = default);
}
