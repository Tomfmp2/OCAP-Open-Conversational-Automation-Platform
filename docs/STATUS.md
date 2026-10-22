# Estado actual de OCAP (hito local v1)

Última actualización: 2026-10-22

## Resumen

OCAP ya se puede desarrollar y probar **en local sin Docker** (`scripts/ocap-dev.ps1`), con:

- API en `http://localhost:5229`
- Panel en `http://localhost:3000`
- Primer acceso: configuración inicial en `/setup` con el código que imprime la API (ver `docs/deployment/FIRST_RUN_SETUP.md`)

El foco de esta etapa es el **agente principal + proveedores de IA + WhatsApp Evolution**, no el diseñador de workflows (retirado de la UI v1).

## Qué está listo

| Área | Estado | Notas |
| --- | --- | --- |
| Arranque local (UseInMemory) | Listo | `.env` + `ocap-dev.ps1`; reiniciar API tras cambiar keys |
| Instalación | Listo | `ocap-setup.sh` genera `.env` y secretos; configuración inicial en `/setup` protegida por código |
| Agente principal (home) | Listo | Chat tipo asistente con contexto de sistema OCAP |
| IA y modelos | Listo | CRUD por tenant, probar/usar/editar, preferred provider |
| Gemini | Listo | Modelo por defecto `gemini-3.5-flash`; auth por `x-goog-api-key` |
| WhatsApp Evolution | Listo (código) | Cliente + webhooks; requiere Evolution en marcha y vars `EVOLUTION_*` / `WhatsApp__*` |
| WebChat | Listo | Canal usable en panel |
| Workflows UI | Fuera de v1 | Rutas placeholder; motor backend sigue en el repo |

## Cómo verificar IA

1. Conecta el proveedor en el paso **IA** de `/setup` o en **IA y modelos** (la clave se guarda cifrada); también sirve `AiProviders__Gemini__ApiKey` en `.env`.
2. Si usaste `.env`, reinicia la API (la configuración desde el panel no lo necesita).
3. En **IA y modelos**, pulsa **Probar** en Gemini.
4. En el home, envía un mensaje al agente principal.

## Pendiente / siguiente

- Conectar Evolution real de punta a punta (QR, instancia estable, mensajes entrantes/salientes).
- Persistir fuera de UseInMemory cuando se quiera DB Postgres local/Docker.
- Reintroducir workflows en UI solo cuando el producto lo priorice.
- Rotar secrets de desarrollo si se compartieron en entornos no locales.

## Artefactos locales (no versionados)

- `.env` (lo genera `./scripts/ocap-setup.sh`)
