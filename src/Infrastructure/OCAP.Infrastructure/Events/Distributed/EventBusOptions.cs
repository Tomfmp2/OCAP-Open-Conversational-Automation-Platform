namespace OCAP.Infrastructure.Events.Distributed;

public class EventBusOptions
{
    public const string SectionName = "EventBus";

    /// <summary>InMemory | RabbitMQ | NATS</summary>
    public string Provider { get; set; } = "InMemory";

    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";
    public string NatsUrl { get; set; } = "nats://localhost:4222";
    public string ExchangeName { get; set; } = "ocap.events";
    public string QueueName { get; set; } = "ocap.workers";
    public string DeadLetterExchange { get; set; } = "ocap.events.dlx";
    public string DeadLetterQueue { get; set; } = "ocap.events.dlq";
    public string JetStreamName { get; set; } = "OCAP_EVENTS";
    public string ConsumerGroup { get; set; } = "ocap-workers";
    public int MaxRetries { get; set; } = 5;
    public int OutboxBatchSize { get; set; } = 50;
    public int PrefetchCount { get; set; } = 32;
    public bool EnableOutbox { get; set; } = true;

    /// <summary>
    /// Si true, publica al transporte en el mismo request (InMemory/dev).
    /// Si false, solo escribe outbox y el dispatcher publica (brokers).
    /// </summary>
    public bool ImmediateDispatch { get; set; } = true;

    public bool EnableInbox { get; set; } = true;
    public int PublisherConfirmTimeoutMs { get; set; } = 5000;
    public int ReconnectDelaySeconds { get; set; } = 5;
}
