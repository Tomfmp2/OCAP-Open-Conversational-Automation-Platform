using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;

namespace OCAP.Agents.Application.Services;

public class AgentService
{
    private readonly IAgentRepository _agentRepository;

    public AgentService(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
    }

    public async Task<Agent> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id, cancellationToken);
        if (agent == null)
            throw new KeyNotFoundException($"Agent with ID {id} not found.");
        return agent;
    }

    public async Task<IEnumerable<Agent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        return await _agentRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Agent> CreateAgentAsync(string name, string description, string systemPrompt, List<string> allowedTools, CancellationToken cancellationToken = default)
    {
        var configuration = new AgentConfiguration(systemPrompt, null, allowedTools);
        var agent = new Agent(Guid.NewGuid(), new AgentName(name), description, configuration);
        
        await _agentRepository.AddAsync(agent, cancellationToken);
        return agent;
    }

    public async Task UpdateAgentAsync(Guid id, string name, string description, string systemPrompt, List<string> allowedTools, CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentAsync(id, cancellationToken);
        var configuration = new AgentConfiguration(systemPrompt, null, allowedTools);
        
        // Note: Currently Agent entity has no Update method for Name and Description.
        // We'll update configuration for now, or we can add UpdateProfile to Agent.
        agent.UpdateConfiguration(configuration);
        
        await _agentRepository.UpdateAsync(agent, cancellationToken);
    }

    public async Task DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentAsync(id, cancellationToken);
        await _agentRepository.DeleteAsync(agent, cancellationToken);
    }
}
