# Bus de Eventos Distribuido (CAP-20)

Especificación técnica de la infraestructura distribuida de mensajería de OCAP.

## Arquitectura de Componentes

- `IEventBus`: Punto de entrada único del dominio de OCAP (100% compatible hacia atrás).
- `DistributedEventBus`: Implementación de orquestación con soporte para Outbox, Inbox, DLQ y envoltura de sobres `EventEnvelope<T>`.
- `IEventTransport`: Adaptador de comunicación de red (`InMemoryTransport`, `RabbitMqEventTransport`, `NatsJetStreamEventTransport`).
- `IOutboxStore`: Persistencia transaccional de mensajes pendientes.
- `IInboxStore`: Deduplicación e idempotencia.
- `IMessageDeadLetterHandler`: Almacenamiento y reintento de mensajes descartados.

## API REST de Administración

- `GET /api/eventbus/status`: Estado del transporte y nodo del clúster.
- `GET /api/eventbus/metrics`: Métricas de mensajes procesados, outbox pendiente y DLQ.
- `GET /api/eventbus/retries`: Política de reintentos y backoff.
- `GET /api/eventbus/deadletters`: Lista de mensajes en DLQ.
- `POST /api/eventbus/deadletters/retry`: Reprocesa un mensaje en DLQ.
- `GET /api/eventbus/connections`: Conexiones activas al clúster.
- `GET /api/eventbus/providers`: Proveedores de transporte soportados.
