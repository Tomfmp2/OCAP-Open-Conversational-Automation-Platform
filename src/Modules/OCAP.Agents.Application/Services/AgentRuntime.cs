using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Core.Events;
using OCAP.Core.Ports;
using OCAP.Core.Entities;
using OCAP.Core.ValueObjects;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Agents.Application.Services;

public class AgentRuntime : IAgentRuntime
{
    private readonly IEnterpriseAssistantAgent _assistantAgent;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly IEventBus? _eventBus;
    private readonly IConversationRepository? _conversationRepository;
    private readonly IMessageRepository? _messageRepository;
    private readonly IAiConversationMemoryRepository? _memoryRepository;
    private readonly IAiExecutionLogRepository? _executionLogRepository;

    public AgentRuntime(
        IEnterpriseAssistantAgent assistantAgent,
        ILogger<AgentRuntime> logger,
        IEventBus? eventBus = null,
        IConversationRepository? conversationRepository = null,
        IMessageRepository? messageRepository = null,
        IAiConversationMemoryRepository? memoryRepository = null,
        IAiExecutionLogRepository? executionLogRepository = null)
    {
        _assistantAgent = assistantAgent;
        _logger = logger;
        _eventBus = eventBus;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _memoryRepository = memoryRepository;
        _executionLogRepository = executionLogRepository;
    }

    public async Task<string> ExecuteAgentAsync(IAgentContext agentContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentContext);

        _logger.LogInformation("Agent Runtime iniciando ejecución para Agente {AgentId}, Tenant {TenantId}",
            agentContext.AgentId, agentContext.TenantId);

        // Resolve or create conversation
        Guid conversationId = Guid.Empty;
        if (agentContext.EnvironmentVariables.TryGetValue("ConversationId", out var cIdObj) && cIdObj is Guid cId)
        {
            conversationId = cId;
        }

        Conversation? conversation = null;
        if (_conversationRepository != null && conversationId != Guid.Empty)
        {
            conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
            {
                conversation = new Conversation(conversationId, agentContext.UserId);
                await _conversationRepository.SaveAsync(conversation, cancellationToken);
            }
        }
        else if (_conversationRepository != null)
        {
            // Try finding active conversation for user
            conversation = await _conversationRepository.GetByUserIdAsync(agentContext.UserId, cancellationToken);
            if (conversation == null || conversation.Status == ConversationStatus.Closed)
            {
                conversationId = Guid.NewGuid();
                conversation = new Conversation(conversationId, agentContext.UserId);
                await _conversationRepository.SaveAsync(conversation, cancellationToken);
            }
            else
            {
                conversationId = conversation.Id;
                conversation.UpdateActivity();
                await _conversationRepository.SaveAsync(conversation, cancellationToken);
            }
        }

        // Persist User Message
        if (_messageRepository != null && conversation != null && !string.IsNullOrWhiteSpace(agentContext.UserMessage))
        {
            var userMsg = new Message(Guid.NewGuid(), conversation.Id, new MessageContent(agentContext.UserMessage), SenderType.User);
            await _messageRepository.SaveAsync(userMsg, cancellationToken);
        }

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(new AgentStartedEvent(
                agentContext.AgentId,
                conversationId,
                agentContext.TenantId,
                agentContext.UserId,
                agentContext.UserMessage
            ), cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await _assistantAgent.ProcessRequestAsync(agentContext, cancellationToken);
        stopwatch.Stop();

        // Parse metrics & model usage from result
        int tokensUsed = 0;
        string modelUsed = "Unknown";
        string providerName = result.ProviderUsed ?? "Unknown";

        if (result.Metadata != null)
        {
            if (result.Metadata.TryGetValue("TokensUsed", out var tObj) && tObj is int t)
                tokensUsed = t;
            if (result.Metadata.TryGetValue("ModelUsed", out var mObj) && mObj is string m)
                modelUsed = m;
        }

        // Persist AI Execution Log
        if (_executionLogRepository != null)
        {
            var executionLog = new AiExecutionLog(
                Guid.NewGuid(),
                providerName,
                modelUsed,
                tokensUsed,
                stopwatch.Elapsed.TotalMilliseconds,
                result.Success
            );
            await _executionLogRepository.SaveAsync(executionLog, cancellationToken);
        }

        // Persist Agent Response Message
        if (result.Success && _messageRepository != null && _conversationRepository != null && conversation != null && !string.IsNullOrWhiteSpace(result.OutputMessage))
        {
            var agentMsg = new Message(Guid.NewGuid(), conversation.Id, new MessageContent(result.OutputMessage), SenderType.Agent);
            await _messageRepository.SaveAsync(agentMsg, cancellationToken);
            
            conversation.UpdateActivity();
            await _conversationRepository.SaveAsync(conversation, cancellationToken);
        }

        // Persist Memory / Context
        if (result.Success && _memoryRepository != null && conversation != null && !string.IsNullOrWhiteSpace(agentContext.UserMessage))
        {
            var userMemory = new AiConversationMemory(
                Guid.NewGuid(),
                conversation.Id,
                "ShortTerm",
                $"User: {agentContext.UserMessage}"
            );
            await _memoryRepository.SaveAsync(userMemory, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.OutputMessage))
            {
                var agentMemory = new AiConversationMemory(
                    Guid.NewGuid(),
                    conversation.Id,
                    "ShortTerm",
                    $"Agent: {result.OutputMessage}"
                );
                await _memoryRepository.SaveAsync(agentMemory, cancellationToken);
            }
        }

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(new AgentCompletedEvent(
                agentContext.AgentId,
                conversationId,
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

        return result.OutputMessage ?? string.Empty;
    }
}
