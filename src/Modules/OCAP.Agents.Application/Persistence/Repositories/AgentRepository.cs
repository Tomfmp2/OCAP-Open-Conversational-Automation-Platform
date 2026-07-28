using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using System.Collections.Concurrent;

namespace OCAP.Agents.Application.Persistence.Repositories;

public class AgentRepository : IAgentRepository
{
    private static readonly ConcurrentDictionary<Guid, Agent> _agents = new();

    public Task<Agent?> GetByIdAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<Agent?> GetDefaultAgentAsync(CancellationToken cancellationToken = default)
    {
        var defaultAgent = _agents.Values.FirstOrDefault();
        return Task.FromResult(defaultAgent);
    }

    public Task SaveAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        if (agent != null)
        {
            _agents[agent.Id] = agent;
        }
        return Task.CompletedTask;
    }
}
