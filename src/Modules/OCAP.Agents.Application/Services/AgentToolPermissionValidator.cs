using OCAP.Agents.Abstractions.Ports;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Services;

/// <summary>
/// Permisos de herramientas: el agente principal sigue su política fija; un agente creado en el panel puede usar
/// exactamente las herramientas que su dueño le habilitó (<c>AllowedToolNames</c>), y ninguna otra.
/// </summary>
public sealed class AgentToolPermissionValidator : IPermissionValidator
{
    private readonly DefaultPermissionValidator _policies;
    private readonly IAgentRepository _agents;

    public AgentToolPermissionValidator(DefaultPermissionValidator policies, IAgentRepository agents)
    {
        _policies = policies;
        _agents = agents;
    }

    public Task<bool> HasPermissionAsync(Guid agentId, string permission, CancellationToken cancellationToken = default) =>
        _policies.HasPermissionAsync(agentId, permission, cancellationToken);

    public async Task<bool> CanExecuteToolAsync(Guid agentId, ITool tool, CancellationToken cancellationToken = default)
    {
        if (tool is null) return false;
        if (await _policies.CanExecuteToolAsync(agentId, tool, cancellationToken)) return true;

        var agent = await _agents.GetByIdAsync(agentId, cancellationToken);
        return agent is not null
               && agent.Configuration.AllowedToolNames.Contains(tool.Definition.Name, StringComparer.OrdinalIgnoreCase);
    }
}
