using OCAP.Agents.Abstractions.Contracts;

namespace OCAP.Agents.Abstractions.Models;

public class AgentContext : IAgentContext
{
    public Guid AgentId { get; }
    public Guid TenantId { get; }
    public Guid UserId { get; }
    public string UserMessage { get; }
    public IDictionary<string, object> EnvironmentVariables { get; }

    public AgentContext(
        Guid agentId,
        Guid tenantId,
        Guid userId,
        string userMessage,
        IDictionary<string, object>? environmentVariables = null)
    {
        AgentId = agentId != Guid.Empty ? agentId : throw new ArgumentException("AgentId cannot be empty.", nameof(agentId));
        TenantId = tenantId != Guid.Empty ? tenantId : throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        UserMessage = userMessage ?? string.Empty;
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, object>();
    }
}
