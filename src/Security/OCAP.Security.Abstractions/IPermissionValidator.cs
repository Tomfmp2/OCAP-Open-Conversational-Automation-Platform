using OCAP.Tools.Abstractions;

namespace OCAP.Security.Abstractions;

// Contrato para el validador de permisos de ejecución de herramientas para agentes.
public interface IPermissionValidator
{
    // Evalúa si un agente dispone de un permiso individual.
    Task<bool> HasPermissionAsync(Guid agentId, string permission, CancellationToken cancellationToken = default);

    // Evalúa si un agente dispone de todos los permisos requeridos para ejecutar una herramienta determinada.
    Task<bool> CanExecuteToolAsync(Guid agentId, ITool tool, CancellationToken cancellationToken = default);
}
