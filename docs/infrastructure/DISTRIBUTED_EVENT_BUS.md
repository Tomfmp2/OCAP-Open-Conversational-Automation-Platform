# Bus de Eventos Distribuido (Sprint 4)

Estado alineado con el código en `OCAP.Infrastructure`.

## Transportes implementados

| Provider | Clase | Notas |
|----------|-------|-------|
| InMemory | `InMemoryEventTransport` | Solo Development/Testing (`EventBus:Provider=InMemory`) |
| RabbitMQ | `RabbitMqEventTransport` | AMQP real (`RabbitMQ.Client`), topic exchange, DLX/DLQ, publisher confirms, prefetch, recovery |
| NATS | `NatsJetStreamEventTransport` | JetStream real (`NATS.Net`), durable consumer, ack/nak, reconnect |

No hay implementaciones de Azure Service Bus, Kafka, SQS, Redis Streams ni Pub/Sub.

## Outbox / Inbox / DLQ

- Publish escribe en `DistributedOutboxMessages` cuando `EnableOutbox=true`.
- `ImmediateDispatch=true` (InMemory): publica al transporte y marca processed.
- Brokers: `ImmediateDispatch=false`; `OutboxProcessorBackgroundService` publica por lotes con retry exponencial; poison → DLQ.
- Inbox deduplica por `(MessageId, ConsumerGroup)` en el handler del bus.
- Replay DLQ reencola en outbox.

## Configuración

```json
"EventBus": {
  "Provider": "RabbitMQ",
  "ConnectionString": "amqp://ocap:pass@rabbitmq:5672/",
  "NatsUrl": "nats://nats:4222",
  "EnableOutbox": true,
  "ImmediateDispatch": false,
  "MaxRetries": 5
}
```

Compose incluye servicios `rabbitmq` y `nats`.
