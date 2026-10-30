using OCAP.Agents.Abstractions.Ports;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Services;

/// <summary>
/// Permisos de herramientas: el agente principal sigue su política fija; un agente creado en el panel puede usar
/// exactamente las herramientas que su dueño le habilitó (<c>AllowedToolNames</c>), y ninguna otra. Si el canal de la
/// petición definió sus herramientas (<see cref="ChannelToolGrants"/>), valen esas.
/// </summary>
public sealed class AgentToolPermissionValidator : IPermissionValidator
{
    private readonly DefaultPermissionValidator _policies;
    private readonly IAgentRepository _agents;
    private readonly ChannelToolGrants? _channelGrants;

    public AgentToolPermissionValidator(DefaultPermissionValidator policies, IAgentRepository agents, ChannelToolGrants? channelGrants = null)
    {
        _policies = policies;
        _agents = agents;
        _channelGrants = channelGrants;
    }

    public Task<bool> HasPermissionAsync(Guid agentId, string permission, CancellationToken cancellationToken = default) =>
        _policies.HasPermissionAsync(agentId, permission, cancellationToken);

    public async Task<bool> CanExecuteToolAsync(Guid agentId, ITool tool, CancellationToken cancellationToken = default)
    {
        if (tool is null) return false;
        if (await _policies.CanExecuteToolAsync(agentId, tool, cancellationToken)) return true;
        if (_channelGrants?.Granted is { } granted) return granted.Contains(tool.Definition.Name);

        var agent = await _agents.GetByIdAsync(agentId, cancellationToken);
        return agent is not null
               && agent.Configuration.AllowedToolNames.Contains(tool.Definition.Name, StringComparer.OrdinalIgnoreCase);
    }
}
