# Deployment (Sprint 4)

## Compose

Servicios: `postgres` (pgvector, host port `5433` por defecto), `rabbitmq`, `nats`, `jaeger`, `prometheus`, `ocap-api`, `ocap-frontend`, `ocap-dashboard`, `evolution-api`, `nginx`.

## Deployment Manager

`OCAP.DeploymentManager` valida de forma real:

- Parámetros de configuración
- Disponibilidad de `docker` / `docker compose`
- Existencia de `docker-compose.yml`
- TCP a Postgres / RabbitMQ / NATS
- Escritura de storage
- Endpoint OTLP / health (si configurados)
- Genera `.env` (no inventa métricas ni simula éxito de contenedores)

No ejecuta automáticamente `docker compose up` (imprime el comando sugerido).

## Variables clave

Ver `.env.example`: JWT, Postgres, EventBus, Storage, OTEL.
