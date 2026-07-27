# OCAP — Integración con WhatsApp (Evolution API Adapter)

## Descripción General

El adaptador de WhatsApp para OCAP (`OCAP.Channels.WhatsApp`) permite a la plataforma enviar y recibir mensajes de WhatsApp en tiempo real a través de **Evolution API**.

Esta integración se implementa respetando la **Arquitectura Hexagonal (Ports & Adapters)**:
- `OCAP.Core` y `OCAP.Application` no conocen detalles de la API de WhatsApp ni de Evolution API.
- `OCAP.Channels.WhatsApp` actúa únicamente como un adaptador de entrada/salida que traduce payloads HTTP a contratos agnósticos internos (`IncomingChannelMessage`, `OutgoingChannelMessage`).

---

## Arquitectura del Adaptador

```
[ Usuario WhatsApp ]
         │
         ▼
[ Evolution API ]
         │ (HTTP Webhook POST)
         ▼
[ OCAP.Api / WhatsAppWebhookController ] (/api/webhooks/whatsapp)
         │
         ▼ (Validar secret & payload)
[ WhatsAppWebhookValidator ]
         │
         ▼ (Mapear a IncomingChannelMessage)
[ WhatsAppWebhookMapper ]
         │
         ▼
[ WhatsAppMessageReceiver ]
         │
         ▼
[ OCAP.Application / ReceiveMessageUseCase ]
```

### Respuesta Saliente (Send Flow)

```
[ OCAP.Application / SendResponseUseCase ]
         │
         ▼
[ WhatsAppMessageSender ]
         │
         ▼
[ EvolutionApiClient ] (HttpClientFactory)
         │ (HTTP POST /message/sendText/{instance})
         ▼
[ Evolution API ] ──► [ Usuario WhatsApp ]
```

---

## Instalación de Evolution API con Docker

Para despliegue en entorno local o servidor de desarrollo, se incluye un archivo Docker Compose en `docker/evolution/docker-compose.evolution.yml`:

```bash
# Iniciar la instancia de Evolution API
docker compose -f docker/evolution/docker-compose.evolution.yml up -d
```

---

## Configuración en OCAP

Agregue o ajuste la sección `WhatsApp` en el archivo `appsettings.json` o variables de entorno:

```json
{
  "WhatsApp": {
    "Enabled": true,
    "BaseUrl": "http://localhost:8080",
    "Instance": "ocap-main",
    "ApiKey": "tu_api_key_de_evolution",
    "WebhookSecret": "tu_secreto_de_webhook_opcional"
  }
}
```

### Variables de Entorno Recomendadas

| Variable | Descripción | Ejemplo |
|---|---|---|
| `WhatsApp__Enabled` | Habilita el canal de WhatsApp | `true` |
| `WhatsApp__BaseUrl` | URL del servidor Evolution API | `http://localhost:8080` |
| `WhatsApp__Instance` | Nombre de la instancia activa | `ocap-main` |
| `WhatsApp__ApiKey` | API Key de autenticación | `secret_api_key` |
| `WhatsApp__WebhookSecret` | Token de seguridad para webhooks | `webhook_secret_token` |

---

## Seguridad

1. **Validación de Secreto de Webhook**: `WhatsAppWebhookValidator` comprueba el header `x-webhook-secret` contra la clave configurada.
2. **Límite de Tamaños de Payload**: Los mensajes entrantes con un cuerpo superior a 10 KB son rechazados para evitar ataques de consumo desmedido de memoria.
3. **Filtro de Mensajes Propios**: Se ignoran automáticamente los mensajes entrantes con la bandera `fromMe = true` para evitar bucles infintos de auto-respuesta.
