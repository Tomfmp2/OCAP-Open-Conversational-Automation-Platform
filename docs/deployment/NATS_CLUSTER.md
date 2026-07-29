# Guía de Despliegue Clúster NATS JetStream para OCAP (CAP-20)

Guía de despliegue en producción de NATS JetStream en Alta Disponibilidad (HA) para OCAP.

## Configuración de Stream JetStream

- **Stream Name**: `OCAP_EVENTS`
- **Subjects**: `ocap.events.>`
- **Storage**: `File` (Persistente)
- **Replication Factor**: `3` (Raft Consensus)
- **Retention Policy**: `Limits`

## Configuración en `appsettings.json`

```json
{
  "EventBus": {
    "Provider": "NATS",
    "ConnectionString": "nats://nats-node1:4222,nats-node2:4222,nats-node3:4222",
    "ExchangeName": "OCAP_EVENTS",
    "MaxRetries": 5
  }
}
```
