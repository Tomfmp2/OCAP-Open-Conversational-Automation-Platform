namespace OCAP.Core.Events;

// Evento publicado al iniciar la ejecución de un Workflow.
public record WorkflowStartedEvent(
    Guid ExecutionId,
    Guid WorkflowDefinitionId,
    Guid TenantId,
    Guid UserId,
    Guid? AgentId,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public WorkflowStartedEvent(Guid executionId, Guid workflowDefinitionId, Guid tenantId, Guid userId, Guid? agentId)
        : this(executionId, workflowDefinitionId, tenantId, userId, agentId, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado al finalizar exitosamente un Workflow.
public record WorkflowCompletedEvent(
    Guid ExecutionId,
    Guid WorkflowDefinitionId,
    Guid TenantId,
    string OutputJson,
    double TotalDurationMs,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public WorkflowCompletedEvent(Guid executionId, Guid workflowDefinitionId, Guid tenantId, string outputJson, double totalDurationMs)
        : this(executionId, workflowDefinitionId, tenantId, outputJson, totalDurationMs, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado al fallar la ejecución de un Workflow.
public record WorkflowFailedEvent(
    Guid ExecutionId,
    Guid WorkflowDefinitionId,
    Guid TenantId,
    string ErrorMessage,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public WorkflowFailedEvent(Guid executionId, Guid workflowDefinitionId, Guid tenantId, string errorMessage)
        : this(executionId, workflowDefinitionId, tenantId, errorMessage, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado tras la ejecución de cada nodo de un Workflow.
public record NodeExecutedEvent(
    Guid ExecutionId,
    string StepId,
    string StepName,
    string NodeType,
    bool Success,
    double DurationMs,
    string OutputJson,
    string? ErrorMessage,
    Guid TenantId,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public NodeExecutedEvent(Guid executionId, string stepId, string stepName, string nodeType, bool success, double durationMs, string outputJson, string? errorMessage, Guid tenantId)
        : this(executionId, stepId, stepName, nodeType, success, durationMs, outputJson, errorMessage, tenantId, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado al iniciar el procesamiento de un Agente IA.
public record AgentStartedEvent(
    Guid AgentId,
    Guid ConversationId,
    Guid TenantId,
    Guid UserId,
    string InputMessage,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public AgentStartedEvent(Guid agentId, Guid conversationId, Guid tenantId, Guid userId, string inputMessage)
        : this(agentId, conversationId, tenantId, userId, inputMessage, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado al completar el procesamiento de un Agente IA.
public record AgentCompletedEvent(
    Guid AgentId,
    Guid ConversationId,
    Guid TenantId,
    Guid UserId,
    string ResponseMessage,
    bool Success,
    double DurationMs,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public AgentCompletedEvent(Guid agentId, Guid conversationId, Guid tenantId, Guid userId, string responseMessage, bool success, double durationMs)
        : this(agentId, conversationId, tenantId, userId, responseMessage, success, durationMs, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado al recibir un mensaje desde cualquier canal (Telegram, WhatsApp, WebChat, etc.).
public record MessageReceivedEvent(
    string ChannelProvider,
    string SenderId,
    string Content,
    Guid TenantId,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public MessageReceivedEvent(string channelProvider, string senderId, string content, Guid tenantId)
        : this(channelProvider, senderId, content, tenantId, DateTime.UtcNow, Guid.NewGuid()) { }
}

// Evento publicado al enviar un mensaje de respuesta hacia cualquier canal.
public record MessageSentEvent(
    string ChannelProvider,
    string RecipientId,
    string Content,
    bool Success,
    Guid TenantId,
    DateTime OccurredAtUtc,
    Guid EventId
) : IEvent
{
    public MessageSentEvent(string channelProvider, string recipientId, string content, bool success, Guid tenantId)
        : this(channelProvider, recipientId, content, success, tenantId, DateTime.UtcNow, Guid.NewGuid()) { }
}

public record ConversationStartedEvent(
    Guid ConversationId,
    Guid UserId,
    DateTime OccurredAtUtc,
    Guid EventId,
    Guid TenantId
) : IEvent
{
    public ConversationStartedEvent(Guid conversationId, Guid userId, DateTime occurredAtUtc)
        : this(conversationId, userId, occurredAtUtc, Guid.NewGuid(), Guid.Empty) { }
}

public record ConversationClosedEvent(
    Guid ConversationId,
    DateTime OccurredAtUtc,
    Guid EventId,
    Guid TenantId
) : IEvent
{
    public ConversationClosedEvent(Guid conversationId, DateTime occurredAtUtc)
        : this(conversationId, occurredAtUtc, Guid.NewGuid(), Guid.Empty) { }
}

public record HumanInterventionRequestedEvent(
    Guid ConversationId,
    Guid UserId,
    DateTime OccurredAtUtc,
    Guid EventId,
    Guid TenantId
) : IEvent
{
    public HumanInterventionRequestedEvent(Guid conversationId, Guid userId, DateTime occurredAtUtc)
        : this(conversationId, userId, occurredAtUtc, Guid.NewGuid(), Guid.Empty) { }
}
