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

    public async Task<Agent> CreateAgentAsync(string name, string description, string systemPrompt, List<string> allowedTools, CancellationToken cancellationToken = default, IReadOnlyCollection<Guid>? knowledgeBaseIds = null)
    {
        await EnsureUniqueNameAsync(name, exceptId: null, cancellationToken);
        var configuration = new AgentConfiguration(systemPrompt, KnowledgeParameters(knowledgeBaseIds), allowedTools);
        var agent = new Agent(Guid.NewGuid(), new AgentName(name), description, configuration);
        
        await _agentRepository.AddAsync(agent, cancellationToken);
        return agent;
    }

    public async Task UpdateAgentAsync(Guid id, string name, string description, string systemPrompt, List<string> allowedTools, CancellationToken cancellationToken = default, IReadOnlyCollection<Guid>? knowledgeBaseIds = null)
    {
        var agent = await GetAgentAsync(id, cancellationToken);
        await EnsureUniqueNameAsync(name, exceptId: id, cancellationToken);
        var configuration = new AgentConfiguration(systemPrompt, KnowledgeParameters(knowledgeBaseIds), allowedTools);
        
        agent.UpdateProfile(new AgentName(name), description);
        agent.UpdateConfiguration(configuration);
        
        await _agentRepository.UpdateAsync(agent, cancellationToken);
    }

    private static Dictionary<string, string>? KnowledgeParameters(IReadOnlyCollection<Guid>? knowledgeBaseIds) =>
        knowledgeBaseIds is { Count: > 0 }
            ? new Dictionary<string, string> { [AgentConfiguration.KnowledgeBaseIdsParameter] = string.Join(',', knowledgeBaseIds.Distinct()) }
            : null;

    public async Task DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentAsync(id, cancellationToken);
        await _agentRepository.DeleteAsync(agent, cancellationToken);
    }

    /// <summary>El nombre es único por organización (índice de la base); se avisa antes con un error legible.</summary>
    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var agents = await _agentRepository.GetAllAsync(cancellationToken);
        if (agents.Any(a => a.Id != exceptId && string.Equals(a.Name.Value, name?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ya existe un agente llamado \"{name?.Trim()}\".");
        }
    }
}
