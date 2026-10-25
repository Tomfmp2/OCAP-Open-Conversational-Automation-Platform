# WhatsApp por QR (Evolution API)

Cada organización vincula **su propio número** escaneando un QR desde el teléfono, igual que WhatsApp Web. No hace
falta WhatsApp Business ni Meta Cloud API. El puente es [Evolution API](https://github.com/EvolutionAPI/evolution-api)
**v2.3.7** (Baileys), incluido en el `docker-compose.yml`.

> WhatsApp Cloud API (Meta) sigue disponible con `POST /api/channels/whatsapp/connect` y el webhook
> `/api/channels/whatsapp/webhook`; este documento cubre el flujo por QR.

## Uso

1. `ocap` (o `./ocap install` la primera vez; genera `EVOLUTION_API_KEY` si falta; no hay que configurar nada más).
2. Panel → **Canales → Conectar canal → WhatsApp → Generar QR**.
3. En el teléfono: WhatsApp → Dispositivos vinculados → Vincular un dispositivo → escanear.
4. El modal pasa a "WhatsApp vinculado · +número". Desde ahí mismo se puede **Desvincular**.

El asistente principal responde los chats individuales. Se ignoran mensajes propios, grupos, estados
(`status@broadcast`), canales y mensajes sin texto (imágenes, audios).

## Diseño

```
Teléfono ─QR─► Evolution (instancia ocap-{tenantId:N}) ──webhook firmado──► POST /api/webhooks/whatsapp
                     ▲                                                          │ verifica firma → tenant
                     └──────────── POST message/sendText/{instancia} ◄──────────┤ conversación + agente
                                                                                └► SignalR "WhatsAppLinkUpdated" → panel
```

| Decisión | Por qué |
|---|---|
| Una instancia por organización, nombre `ocap-{tenantId:N}` | El webhook sabe de qué organización es un evento sin consultar nada; vincular dos veces reutiliza la misma instancia. |
| Firma por instancia: `X-OCAP-Webhook-Signature = HMAC-SHA256(clave, instancia)`, con la clave derivada de `EVOLUTION_API_KEY` (o `WHATSAPP_WEBHOOK_SECRET` si se define) | Evolution la envía como cabecera fija del webhook de cada instancia. La firma de una organización no sirve para otra. |
| El webhook **falla cerrado** | Sin API key de Evolution responde 503. Los eventos que OCAP procesa (QR, conexión, mensajes) sin firma válida reciben 401; los que no procesa se confirman con 200 sin efecto, porque Evolution envía algunos de ciclo de vida sin cabeceras. Nadie puede inyectar mensajes falsos. |
| Tenant fijado por el servidor (`HttpTenantContext.UseTrustedTenant`) | La petición no tiene sesión; tras verificar la firma, las lecturas y escrituras de EF quedan aisladas en esa organización. |
| Deduplicación por `key.id` (6 h, memoria) | Evolution reintenta los webhooks que tardan o fallan; un mensaje se responde una sola vez. |
| Respuesta de respaldo si el agente falla | El contacto nunca queda sin contestación (p. ej. sin proveedor de IA configurado). |
| Sesiones en el Postgres de OCAP, esquema `evolution_api`, y Redis db 6 | La vinculación sobrevive a reinicios sin otra base ni volumen. |
| Puerto de Evolution solo en `127.0.0.1` | La API key global de Evolution controla todas las sesiones. |
| Sin webhook global (`WEBHOOK_GLOBAL_ENABLED=false`) | Cada instancia lleva su propia URL y firma. |

## API del panel (requiere sesión)

| Método | Ruta | Resultado |
|---|---|---|
| `POST` | `/api/channels/whatsapp/link` | Crea o reutiliza la instancia y devuelve `{ state: "pending", qrBase64, pairingCode }`, o `linked` si ya estaba vinculado. 503 si el servidor no tiene WhatsApp configurado; 502 si Evolution no responde. |
| `GET` | `/api/channels/whatsapp/link?qr=true` | `{ state: "unlinked" \| "pending" \| "linked", qrBase64?, phoneNumber?, profileName? }`. |
| `DELETE` | `/api/channels/whatsapp/link` | Cierra la sesión del teléfono, borra la instancia y la conexión. 204. |
| `GET` | `/api/channels/whatsapp/health` | `{ provider, configured, reachable }`. |

Todas actúan sobre la organización de la sesión; no aceptan nombres de instancia.

Desde la tarjeta del canal (comunes a todos los canales):

| Método | Ruta | Resultado |
|---|---|---|
| `PATCH` | `/api/channels/connections/{id}` | `{ displayName }`, 1–100 caracteres. El nombre elegido se conserva al reconectar el teléfono. 204. |
| `DELETE` | `/api/channels/{id}` | En WhatsApp por QR equivale a desvincular (cierra la sesión del teléfono y borra la instancia); 502 si Evolution no responde. En Telegram por webhook también quita el webhook del bot. Las conversaciones se conservan. |
| `PUT` | `/api/channels/connections/{id}/agent` y `/ai` | Agente y proveedor de IA del canal (ver [proveedores de IA](../intelligence/AI_PROVIDERS.md)). |

## Configuración

Compose la inyecta sola. Para la API fuera de Docker (`dotnet run`, puerto 5229) con Evolution en Docker:

| Clave | Docker | Local |
|---|---|---|
| `WhatsApp__BaseUrl` | `http://evolution-api:8080` | `http://localhost:8088` (en `appsettings.Development.json`) |
| `WhatsApp__ApiKey` | `EVOLUTION_API_KEY` | exportar `EVOLUTION_API_KEY` |
| `WhatsApp__WebhookUrl` | `http://ocap-api:5000/api/webhooks/whatsapp` | `http://host.docker.internal:5229/api/webhooks/whatsapp` |
| `WhatsApp__WebhookSecret` | opcional (`WHATSAPP_WEBHOOK_SECRET`) | opcional |

Cambiar `EVOLUTION_API_KEY` (o `WHATSAPP_WEBHOOK_SECRET`) invalida las firmas de las instancias existentes; se
renuevan al pulsar **Generar QR** otra vez (también se actualizan la URL y los eventos del webhook).

## Pruebas

- Unitarias: `tests/OCAP.Channels.WhatsApp.Tests/Evolution/` (contrato HTTP v2, firma, eventos del webhook).
- Integración: `tests/OCAP.Api.Tests/Endpoints/WhatsAppEvolutionEndpointTests.cs`, con un Evolution en memoria
  (vincular → escanear → mensaje → respuesta única → desvincular; firmas inválidas; sin secreto).
- Con un teléfono real: [`docs/testing/WHATSAPP_E2E.md`](../testing/WHATSAPP_E2E.md).
