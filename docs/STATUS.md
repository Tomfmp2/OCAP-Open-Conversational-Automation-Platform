# Estado actual de OCAP (hito local v1)

Última actualización: 2026-10-23

## Resumen

OCAP se instala con Docker (`./scripts/ocap-up.sh`) o se desarrolla **en local sin Docker** (`scripts/ocap-dev.sh` en Linux/macOS, `scripts/ocap-dev.ps1` en Windows), con:

- API en `http://localhost:5229`
- Panel en `http://localhost:3000`
- Primer acceso: configuración inicial en `/setup` con el código que imprime la API (ver `docs/deployment/FIRST_RUN_SETUP.md`)

El foco de esta etapa es el **agente principal + proveedores de IA + WhatsApp Evolution**, no el diseñador de workflows (retirado de la UI v1).

## Qué está listo

| Área | Estado | Notas |
| --- | --- | --- |
| Arranque local (UseInMemory) | Listo | `.env` + `ocap-dev.sh` / `ocap-dev.ps1`; reiniciar API tras cambiar keys |
| Instalación | Listo | `ocap-setup.sh` genera `.env` y secretos; configuración inicial en `/setup` protegida por código |
| Agente principal (home) | Listo | Chat tipo asistente; sin keys de IA responde en modo demo (Mock) |
| IA y modelos | Listo | CRUD por tenant, probar/usar/editar, preferred provider |
| Gemini | Listo | Modelo por defecto `gemini-3.5-flash`; auth por `x-goog-api-key` |
| WhatsApp Evolution | Listo | QR desde el panel, webhook firmado por organización, respuesta del asistente; validado contra Evolution v2.3.7 real salvo el escaneo con teléfono ([checklist](testing/WHATSAPP_E2E.md)) |
| Agentes por canal | Listo | Asignables por canal; prompt, historial, conocimiento, herramientas seguras para contactos, pase a una persona; chat de prueba |
| Bandeja | Listo | `/inbox`: tomar, responder por el canal y devolver al agente |
| WebChat | Listo | Canal usable en panel |
| Telegram | Listo | Bot desde el panel; polling con el token de cada bot o webhook por conexión con secreto derivado del token; responde chats privados |
| Workflows UI | Fuera de v1 | Rutas placeholder; motor backend sigue en el repo |

## Cómo verificar IA

1. Conecta el proveedor en el paso **IA** de `/setup` o en **IA y modelos** (la clave se guarda cifrada); también sirve `AiProviders__Gemini__ApiKey` en `.env`.
2. Si usaste `.env`, reinicia la API (la configuración desde el panel no lo necesita).
3. En **IA y modelos**, pulsa **Probar** en Gemini.
4. En el home, envía un mensaje al agente principal.

## Pendiente / siguiente

- Prueba manual de WhatsApp con un teléfono real (`docs/testing/WHATSAPP_E2E.md`).
- Persistir fuera de UseInMemory cuando se quiera DB Postgres local/Docker.
- Reintroducir workflows en UI solo cuando el producto lo priorice.
- Rotar secrets de desarrollo si se compartieron en entornos no locales.

## Artefactos locales (no versionados)

- `.env` (lo genera `./scripts/ocap-setup.sh`)
