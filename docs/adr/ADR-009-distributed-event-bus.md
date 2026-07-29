# ADR-009: Bus de Eventos Distribuido, Escalabilidad Horizontal y Cluster HA (CAP-20)

## Estado
Aprobado

## Contexto
Con la finalización de los módulos de automatización empresarial de OCAP, se requiere reemplazar el Bus de Eventos puramente In-Memory con una arquitectura distribuida tolerante a fallos que permita escalar horizontalmente múltiples nodos API y Worker en un clúster activo-activo, manteniendo compatibilidad total hacia atrás con la interfaz `IEventBus`.

## Decisiones de Diseño

1. **Abstracción Agnóstica de Transporte (`IEventTransport`)**:
   - Soporte desacoplado para RabbitMQ (Topic Exchanges, Publisher Confirms, DLX/DLQ, PreFetch QoS), NATS JetStream (Streams, Durable Consumers, Replay), InMemory (desarrollo/tests), y preparación para Azure Service Bus, AWS SQS, Kafka, Redis Streams y Google PubSub.

2. **Garantía de Entrega At-Least-Once & Idempotencia**:
   - **Patrón Outbox (`IOutboxStore`, `OutboxProcessorBackgroundService`)**: Persistencia transaccional de eventos antes de su publicación en la red.
   - **Patrón Inbox (`IInboxStore`)**: Deduplicación de mensajes entrantes mediante trazabilidad por `MessageId` y `ConsumerGroup`.
   - **Manejo de Mensajes Muertos (`IMessageDeadLetterHandler`)**: Registro inmutable y reprocesamiento de eventos en Dead Letter Queue (DLQ).

3. **Propagación en Tiempo Real Multi-Nodo (SignalR Gateway)**:
   - Integración transparente con SignalR Live Gateway para propagación clúster de eventos en vivo a todos los clientes conectados.

## Consecuencias
- Despliegue en producción con tolerancia a fallos y alta disponibilidad.
- Cero acoplamiento entre los módulos de aplicación de OCAP y el proveedor de mensajería subyacente.
