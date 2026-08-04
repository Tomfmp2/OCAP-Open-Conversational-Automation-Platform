# Desarrollo local sin Docker

## Arranque rápido (Windows)

```powershell
.\scripts\ocap-dev.ps1
```

Abre dos ventanas: API `http://localhost:5229` y frontend `http://localhost:3000`.

## Login

- Email: `admin@ocap.io`
- Password: `ChangeMe_Admin_2026!`

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
- WhatsApp (Evolution): `EVOLUTION_API_URL` / `EVOLUTION_API_KEY` y `WhatsApp__*` en `.env`; instancia Evolution en marcha

## Nota

Datos en memoria: al reiniciar la API se pierden. No hace falta PostgreSQL ni Docker.
