using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Core.Events;

namespace OCAP.Agents.Application.Services;

public class AgentRuntime : IAgentRuntime
{
    private readonly IEnterpriseAssistantAgent _assistantAgent;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly IEventBus? _eventBus;

    public AgentRuntime(
        IEnterpriseAssistantAgent assistantAgent,
        ILogger<AgentRuntime> logger,
        IEventBus? eventBus = null)
    {
        _assistantAgent = assistantAgent;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<string> ExecuteAgentAsync(IAgentContext agentContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentContext);

        _logger.LogInformation("Agent Runtime iniciando ejecución para Agente {AgentId}, Tenant {TenantId}",
            agentContext.AgentId, agentContext.TenantId);

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(new AgentStartedEvent(
                agentContext.AgentId,
                Guid.Empty,
                agentContext.TenantId,
                agentContext.UserId,
                agentContext.UserMessage
            ), cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await _assistantAgent.ProcessRequestAsync(agentContext, cancellationToken);
        stopwatch.Stop();

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(new AgentCompletedEvent(
                agentContext.AgentId,
                Guid.Empty,
                agentContext.TenantId,
                agentContext.UserId,
                result.OutputMessage ?? string.Empty,
                result.Success,
                stopwatch.Elapsed.TotalMilliseconds
            ), cancellationToken);
        }

        if (!result.Success)
        {
            _logger.LogWarning("Ejecución del Agente {AgentId} falló.", agentContext.AgentId);
            return "Lo sentimos, ocurrió un inconveniente durante el procesamiento del agente.";
        }

        return result.OutputMessage;
    }
}
