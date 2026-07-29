# Guía de Despliegue Clúster RabbitMQ para OCAP (CAP-20)

Guía de despliegue en producción de RabbitMQ en Alta Disponibilidad (HA) para OCAP.

## Configuración de Topología en RabbitMQ

- **Exchange**: `ocap.events` (Tipo: `topic`, Durable).
- **Dead Letter Exchange**: `ocap.events.dlx` (Tipo: `fanout`, Durable).
- **Dead Letter Queue**: `ocap.events.dlq` (Durable).
- **QoS Prefetch**: `100` mensajes por consumidor.
- **Publisher Confirms**: Habilitado (Garantía de persistencia en disco).

## Configuración en `appsettings.json`

```json
{
  "EventBus": {
    "Provider": "RabbitMQ",
    "ConnectionString": "amqp://user:password@rabbitmq-node1:5672,rabbitmq-node2:5672/",
    "ExchangeName": "ocap.events",
    "MaxRetries": 5
  }
}
```
