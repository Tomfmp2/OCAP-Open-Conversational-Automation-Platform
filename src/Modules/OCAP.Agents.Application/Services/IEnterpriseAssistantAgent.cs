using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;

namespace OCAP.Agents.Application.Services;

// Interfaz del Enterprise Assistant Agent, el agente global orquestador de OCAP.
public interface IEnterpriseAssistantAgent
{
    Guid GlobalAgentId { get; }
    Task<AgentExecutionResult> ProcessRequestAsync(IAgentContext context, CancellationToken cancellationToken = default);
}
