using OCAP.Agents.Abstractions.Contracts;

namespace OCAP.Agents.Application.Services;

public class AgentResolver : IAgentResolver
{
    public Task<Guid> ResolveAgentIdAsync(Guid tenantId, Guid userId, string messageContent, CancellationToken cancellationToken = default)
    {
        // Por defecto, todas las solicitudes son orquestadas por el Enterprise Assistant Agent.
        // En versiones futuras, se podrá enrutar a agentes especializados basándose en intenciones o configuraciones del tenant.
        return Task.FromResult(EnterpriseAssistantAgent.EnterpriseAssistantAgentId);
    }
}
