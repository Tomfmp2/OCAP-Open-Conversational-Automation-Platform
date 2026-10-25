# OCAP (Open Conversational Automation Platform)

[![CI](https://github.com/Tomfmp2/OCAP-Open-Conversational-Automation-Platform/actions/workflows/ci.yml/badge.svg)](https://github.com/Tomfmp2/OCAP-Open-Conversational-Automation-Platform/actions/workflows/ci.yml)

OCAP es una plataforma self-hosted de automatización conversacional. Este repositorio contiene una API ASP.NET Core, una interfaz principal en Next.js y adaptadores para canales, proveedores de IA, almacenamiento e integraciones.

El proyecto está en desarrollo activo. La presencia de una abstracción o un proyecto de proveedor no implica que toda la integración esté completa para producción.

| Asistente principal | Vincular WhatsApp por QR |
| --- | --- |
| ![Chat con el asistente principal en modo demo](docs/assets/readme/03-chat-modo-demo.png) | ![Modal de vinculación de WhatsApp con el código QR](docs/assets/readme/04-whatsapp-qr.png) |

## Alcance de la v1

| Área | Estado |
| --- | --- |
| Instalación con `./ocap install`, arranque con `ocap` y configuración inicial en `/setup` | Listo |
| Asistente principal (chat del panel), con modo demo si no hay keys de IA | Listo |
| Agentes por canal: instrucciones propias, historial, base de conocimiento, chat de prueba y pase a una persona | Listo |
| Bandeja de conversaciones: ver chats de WhatsApp y Telegram, tomarlos, responder y devolverlos al agente | Listo |
| IA y modelos por organización: Gemini, OpenAI, Claude, Ollama | Listo |
| WhatsApp por QR (Evolution API v2), con respuesta del asistente | Listo ([guía](docs/channels/WHATSAPP.md)) |
| WebChat | Listo |
| Telegram (bot conectado desde el panel, polling o webhook) | Listo |
| WhatsApp Cloud API (Meta) | Listo con `WhatsApp__AppSecret` (firma de Meta); responde con el asistente |
| Google Calendar, Gmail y Sheets | Disponible con token OAuth |
| Motor de workflows | Solo API; fuera de la interfaz de la v1 |
| Slack, Teams, Discord; Qdrant, Chroma, Pinecone | No implementado |

## Componentes implementados

- API y servicios de aplicación en .NET 10.
- Frontend principal en Next.js 16 (`frontend/`).
- Persistencia relacional en PostgreSQL.
- Búsqueda vectorial mediante la extensión PgVector de PostgreSQL; las pruebas y ejecuciones aisladas pueden usar almacenamiento en memoria.
- Bus de eventos seleccionable entre RabbitMQ, NATS e InMemory. Docker Compose usa RabbitMQ por defecto y también inicia NATS.
- Caché distribuida Redis cuando se configura `ConnectionStrings:Redis`.
- Canales runtime: Telegram, WhatsApp (vinculación por QR con Evolution API, o WhatsApp Cloud API) y WebChat (`/channels/webchat`).
- Proveedores HTTP para Google Calendar, Gmail y Sheets mediante un token Bearer configurado.
- Contenedores auxiliares para observabilidad, proxy y servicios integrados.

Consulta `docs/` y los proyectos bajo `src/` para conocer el alcance de cada módulo. El estado honesto de huecos (Slack/Teams, Qdrant, etc.) está en `docs/roadmap/ROADMAP.md`.

## Inicio rápido

En Linux o macOS (en Windows, dentro de WSL2), una sola línea:

```bash
curl -fsSL https://raw.githubusercontent.com/Tomfmp2/OCAP-Open-Conversational-Automation-Platform/main/install.sh | bash
```

Descarga OCAP en `~/ocap` y ejecuta `ocap install`, que **instala lo que falte** (Docker con Compose, git, curl)
pidiendo confirmación, genera el `.env` con secretos aleatorios, construye las imágenes y deja el comando `ocap`.
Docker se instala con el gestor de paquetes del sistema: `pacman` (Arch, CachyOS, Manjaro), `apt` con el
repositorio oficial de Docker (Ubuntu, Debian, Mint) o `dnf` (Fedora, RHEL); en macOS, Homebrew con Colima. Pide
tu contraseña una vez y agrega tu usuario al grupo `docker` (que equivale a permisos de administrador).

O, si ya clonaste el repo:

```bash
./ocap install      # desde la carpeta del repo, una sola vez
```

Desde entonces, en cualquier terminal y carpeta:

```bash
ocap                # enciende Docker si está apagado, levanta todo y abre el panel
```

`ocap` levanta PostgreSQL (con PgVector), Redis, RabbitMQ, NATS, la API, el panel y Evolution (WhatsApp), espera
a que respondan y abre `http://localhost:3000`. Si el código cambió (por ejemplo tras un `git pull`), reconstruye
solo. La primera vez muestra el **código de configuración** para crear la organización y tu cuenta en `/setup`.

| Comando | Qué hace |
| --- | --- |
| `ocap` | Levanta OCAP y abre el panel |
| `ocap stop` | Lo apaga (los datos se conservan) |
| `ocap status` | Estado de los servicios |
| `ocap logs [servicio]` | Logs en vivo, p. ej. `ocap logs ocap-api` |
| `ocap update` | `git pull` + reconstruir + levantar |
| `ocap reset` | Borra todos los datos y empieza de cero (pide confirmación) |
| `ocap uninstall` | Quita el comando (no borra el repo ni los datos) |

Detalle de la configuración inicial en [`docs/deployment/FIRST_RUN_SETUP.md`](docs/deployment/FIRST_RUN_SETUP.md).

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
| Evolution API (solo `127.0.0.1`) | `8088` |

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

Sin Docker, API en memoria (`:5229`) y panel (`:3000`) en una sola terminal:

```bash
./scripts/ocap-dev.sh          # Linux y macOS
.\scripts\ocap-dev.ps1         # Windows (PowerShell)
```

Por separado:

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

### CI local

`./scripts/ci-local.sh` corre las mismas comprobaciones que `.github/workflows/ci.yml` sin depender de GitHub
Actions: build del backend sin avisos y sus pruebas; lint, tipos, pruebas y build del frontend; y dependencias
vulnerables. `--docker` agrega las imágenes escaneadas con Trivy, `--e2e` el smoke de Playwright sobre un stack
aislado (no toca tu `.env` ni un OCAP ya levantado) y `--all` corre todo (unos 6 minutos).

Para que corra solo antes de cada `git push`, activa el hook una vez por clon:

```bash
git config core.hooksPath .githooks
```

Para saltarlo puntualmente: `OCAP_SKIP_CI=1 git push`.

La configuración base está en `src/Api/OCAP.Api/appsettings.json`; los valores sensibles deben suministrarse mediante secretos o variables de entorno.

## Base de conocimiento

PgVector es el único motor vectorial persistente implementado. InMemory existe para pruebas o ejecución local aislada. Qdrant, Chroma y Pinecone no están implementados en este repositorio.

## Licencia y cambios

Consulta el archivo de licencia del repositorio y [CHANGELOG.md](CHANGELOG.md) para el historial registrado de cambios.
