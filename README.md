# OCAP (Open Conversational Automation Platform)

OCAP es una plataforma self-hosted de automatización conversacional. Este repositorio contiene una API ASP.NET Core, una interfaz principal en Next.js y adaptadores para canales, proveedores de IA, almacenamiento e integraciones.

El proyecto está en desarrollo activo. La presencia de una abstracción o un proyecto de proveedor no implica que toda la integración esté completa para producción.

## Componentes implementados

- API y servicios de aplicación en .NET 10.
- Frontend principal en Next.js 16 (`frontend/`).
- Persistencia relacional en PostgreSQL.
- Búsqueda vectorial mediante la extensión PgVector de PostgreSQL; las pruebas y ejecuciones aisladas pueden usar almacenamiento en memoria.
- Bus de eventos seleccionable entre RabbitMQ, NATS e InMemory. Docker Compose usa RabbitMQ por defecto y también inicia NATS.
- Caché distribuida Redis cuando se configura `ConnectionStrings:Redis`.
- Proveedores HTTP para Google Calendar, Gmail y Sheets mediante un token Bearer configurado.
- Contenedores auxiliares para observabilidad, proxy y servicios integrados.

Consulta `docs/` y los proyectos bajo `src/` para conocer el alcance de cada módulo.

## Inicio rápido con Docker

Requisitos: Docker Engine y Docker Compose.

```bash
./scripts/ocap-up.sh
```

Abre el instalador de producto en `http://localhost:3000/installer` (admin, Google, IA).  
Reset total: `docker compose down -v && ./scripts/ocap-up.sh`.

Equivalente manual: `cp .env.example .env && docker compose up --build -d`.

Antes de producción, reemplaza las credenciales de ejemplo de `.env` y configura secretos propios.

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

Al iniciar una base sin usuarios, la API crea un tenant, rol y administrador si `Bootstrap:Enabled` es `true` (valor predeterminado). Configura estos valores mediante variables de entorno:

```text
Bootstrap__Enabled=true
Bootstrap__AdminEmail=admin@example.com
Bootstrap__AdminPassword=una-clave-segura
Bootstrap__TenantName=Mi organización
Bootstrap__TenantSlug=mi-organizacion
```

Si no se proporcionan, el código contiene valores de desarrollo conocidos. No deben utilizarse en producción.

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
