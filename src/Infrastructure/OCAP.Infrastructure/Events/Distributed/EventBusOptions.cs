namespace OCAP.Infrastructure.Events.Distributed;

public class EventBusOptions
{
    public const string SectionName = "EventBus";

    public string Provider { get; set; } = "InMemory"; // InMemory, RabbitMQ, NATS
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";
    public string ExchangeName { get; set; } = "ocap.events";
    public int MaxRetries { get; set; } = 3;
    public int OutboxBatchSize { get; set; } = 100;
    public bool EnableOutbox { get; set; } = true;
    public bool EnableEncryption { get; set; } = false;
}
