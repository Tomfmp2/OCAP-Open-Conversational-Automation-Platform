# OCAP (Open Conversational Automation Platform)

OCAP es una plataforma self-hosted de automatización conversacional. Este repositorio contiene una API ASP.NET Core, una interfaz principal en Next.js y adaptadores para canales, proveedores de IA, almacenamiento e integraciones.

El proyecto está en desarrollo activo. La presencia de una abstracción o un proyecto de proveedor no implica que toda la integración esté completa para producción.

## Componentes implementados

- API y servicios de aplicación en .NET 10.
- Frontend principal en Next.js 16 (`frontend/`), incluyendo diseñador de workflows en `/workflows/designer`.
- Persistencia relacional en PostgreSQL.
- Búsqueda vectorial mediante la extensión PgVector de PostgreSQL; las pruebas y ejecuciones aisladas pueden usar almacenamiento en memoria.
- Bus de eventos seleccionable entre RabbitMQ, NATS e InMemory. Docker Compose usa RabbitMQ por defecto y también inicia NATS.
- Caché distribuida Redis cuando se configura `ConnectionStrings:Redis`.
- Canales runtime: Telegram, WhatsApp Cloud API y WebChat (`/channels/webchat`).
- Proveedores HTTP para Google Calendar, Gmail y Sheets mediante un token Bearer configurado.
- Contenedores auxiliares para observabilidad, proxy y servicios integrados.

Consulta `docs/` y los proyectos bajo `src/` para conocer el alcance de cada módulo. El estado honesto de huecos (Slack/Teams, Qdrant, etc.) está en `docs/roadmap/ROADMAP.md`.

## Inicio rápido con Docker

Requisitos: Docker Engine y Docker Compose.

```bash
./scripts/ocap-up.sh
```

El script genera el `.env` con secretos aleatorios (`./scripts/ocap-setup.sh`), construye el stack y, al terminar,
muestra el **código de configuración**. Abre `http://localhost:3000/setup`, ingrésalo y crea la organización y tu
cuenta de administrador; la conexión con un proveedor de IA es opcional y se puede hacer después.

Reset total: `docker compose down -v && ./scripts/ocap-up.sh`.

Equivalente manual: `./scripts/ocap-setup.sh && docker compose up --build -d`. Detalle en
[`docs/deployment/FIRST_RUN_SETUP.md`](docs/deployment/FIRST_RUN_SETUP.md).

Servicios y puertos publicados por `docker-compose.yml`:

| Servicio | Puerto |
| --- | --- |
| Frontend Next.js | `3000` |
| API | `5000` |
| PostgreSQL/PgVector | `5433` en el host (`5432` en el contenedor) |
| Redis | `6379` |
| RabbitMQ AMQP / administración | `5672` / `15672` |
| NATS / monitorización | `4222` / `8222` |
| Jaeger | `16686` |
| Prometheus | `9090` |
| Nginx | `80` |
| Dashboard auxiliar | `8081` |
| Evolution API | `8088` |

La API recibe PostgreSQL, Redis y el bus de eventos mediante variables de entorno del contenedor. Cambia `EVENTBUS_PROVIDER` a `Nats` para usar NATS; el valor por defecto del Compose es `RabbitMQ`.

## Administrador inicial

No hay credenciales por defecto. En una base sin usuarios, la API imprime un código de un solo uso en sus logs y el
panel pide la configuración inicial en `/setup`. Para despliegues automatizados se puede crear el admin desde el
entorno definiendo `BOOTSTRAP_ADMIN_EMAIL` y `BOOTSTRAP_ADMIN_PASSWORD` (mínimo 12 caracteres con letras y números).

## Gobernanza de API

La API aplica autenticación JWT por defecto, validación FluentValidation, errores RFC 7807 (`application/problem+json`), versionado 1.0 y cabeceras `X-Correlation-Id` / `X-Request-Id`. Detalle en `docs/API_GOVERNANCE.md`.

OpenTelemetry: paquete OTLP **1.17.0** (ver `docs/infrastructure/OPENTELEMETRY.md`).

## Salud y métricas

La API expone:

- `GET /health/live`
- `GET /health/ready`
- `GET /health/startup`
- `GET /api/health`
- `GET /api/health/system`
- `GET /api/health/diagnostic`
- `GET /metrics`

Docker usa `/health/ready` para comprobar la disponibilidad de la API.

## Desarrollo local

Backend:

```bash
dotnet restore OCAP.slnx
dotnet build OCAP.slnx
dotnet run --project src/Api/OCAP.Api
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

La configuración base está en `src/Api/OCAP.Api/appsettings.json`; los valores sensibles deben suministrarse mediante secretos o variables de entorno.

## Base de conocimiento

PgVector es el único motor vectorial persistente implementado. InMemory existe para pruebas o ejecución local aislada. Qdrant, Chroma y Pinecone no están implementados en este repositorio.

## Licencia y cambios

Consulta el archivo de licencia del repositorio y [CHANGELOG.md](CHANGELOG.md) para el historial registrado de cambios.
