namespace OCAP.Core.Events;

public class MessageReceivedEvent
{
    public Guid MessageId { get; }
    public Guid ConversationId { get; }
    public DateTime ReceivedAt { get; }

    public MessageReceivedEvent(Guid messageId, Guid conversationId, DateTime receivedAt)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        ReceivedAt = receivedAt;
    }
}

public class ConversationStartedEvent
{
    public Guid ConversationId { get; }
    public Guid UserId { get; }
    public DateTime StartedAt { get; }

    public ConversationStartedEvent(Guid conversationId, Guid userId, DateTime startedAt)
    {
        ConversationId = conversationId;
        UserId = userId;
        StartedAt = startedAt;
    }
}

public class ConversationClosedEvent
{
    public Guid ConversationId { get; }
    public DateTime ClosedAt { get; }

    public ConversationClosedEvent(Guid conversationId, DateTime closedAt)
    {
        ConversationId = conversationId;
        ClosedAt = closedAt;
    }
}

public class HumanInterventionRequestedEvent
{
    public Guid ConversationId { get; }
    public Guid UserId { get; }
    public DateTime RequestedAt { get; }

    public HumanInterventionRequestedEvent(Guid conversationId, Guid userId, DateTime requestedAt)
    {
        ConversationId = conversationId;
        UserId = userId;
        RequestedAt = requestedAt;
    }
}
