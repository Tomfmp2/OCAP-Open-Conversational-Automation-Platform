using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;

namespace OCAP.Agents.Application.Services;

public class AgentRuntime : IAgentRuntime
{
    private readonly IEnterpriseAssistantAgent _assistantAgent;
    private readonly ILogger<AgentRuntime> _logger;

    public AgentRuntime(
        IEnterpriseAssistantAgent assistantAgent,
        ILogger<AgentRuntime> logger)
    {
        _assistantAgent = assistantAgent;
        _logger = logger;
    }

    public async Task<string> ExecuteAgentAsync(IAgentContext agentContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentContext);

        _logger.LogInformation("Agent Runtime iniciando ejecución para Agente {AgentId}, Tenant {TenantId}",
            agentContext.AgentId, agentContext.TenantId);

        var result = await _assistantAgent.ProcessRequestAsync(agentContext, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Ejecución del Agente {AgentId} falló.", agentContext.AgentId);
            return "Lo sentimos, ocurrió un inconveniente durante el procesamiento del agente.";
        }

        return result.OutputMessage;
    }
}
