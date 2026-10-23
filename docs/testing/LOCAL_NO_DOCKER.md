# Desarrollo local sin Docker

## Arranque rápido (Windows)

```powershell
.\scripts\ocap-dev.ps1
```

Abre dos ventanas: API `http://localhost:5229` y frontend `http://localhost:3000`.

## Primer acceso

La base es en memoria, así que cada reinicio de la API empieza vacío:

- Abre `http://localhost:3000/setup` e ingresa el código que imprime la ventana de la API, o
- define `BOOTSTRAP_ADMIN_EMAIL` y `BOOTSTRAP_ADMIN_PASSWORD` (mínimo 12 caracteres con letras y números) en `.env`
  para que el admin se cree solo en cada arranque.

Detalle en [`docs/deployment/FIRST_RUN_SETUP.md`](../deployment/FIRST_RUN_SETUP.md).

## Qué ya funciona sin keys

- Login / panel
- Canales → conectar **WebChat**
- Chat en `/channels/webchat` (respuestas del proveedor **Mock**)
- Workflows → `/workflows/designer`

## Datos opcionales (IA real)

Edita `.env` en la raíz y rellena **solo lo que tengas**:

| Variable | Dónde obtenerla |
| --- | --- |
| `AiProviders__OpenAI__ApiKey` | https://platform.openai.com/api-keys |
| `AiProviders__Gemini__ApiKey` | https://aistudio.google.com/apikey |
| `AiProviders__Claude__ApiKey` | https://console.anthropic.com/ |
| Ollama | Instalar Ollama; dejar `AiProviders__Ollama__BaseUrl=http://localhost:11434` |

Sin keys, `AiProviders__EnableMock=true` responde en local.

## Telegram / WhatsApp (opcional)

- Telegram: token de @BotFather → conectar en UI Canales
- WhatsApp (Evolution): levanta solo Evolution con `docker compose up -d evolution-api` y exporta `EVOLUTION_API_KEY` (el valor del `.env`). Ver `docs/channels/WHATSAPP.md`

## Nota

Datos en memoria: al reiniciar la API se pierden. No hace falta PostgreSQL ni Docker.
