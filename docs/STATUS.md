# Estado actual de OCAP (hito local v1)

Última actualización: 2026-08-04

## Resumen

OCAP ya se puede desarrollar y probar **en local sin Docker** (`scripts/ocap-dev.ps1`), con:

- API en `http://localhost:5229`
- Panel en `http://localhost:3000`
- Login bootstrap: `admin@ocap.io` / `ChangeMe_Admin_2026!` (cambiar en producción)

El foco de esta etapa es el **agente principal + proveedores de IA + WhatsApp Evolution**, no el diseñador de workflows (retirado de la UI v1).

## Qué está listo

| Área | Estado | Notas |
| --- | --- | --- |
| Arranque local (UseInMemory) | Listo | `.env` + `ocap-dev.ps1`; reiniciar API tras cambiar keys |
| Instalador (Dev / Local / Web) | Listo | Wizard mínimo; secretos van al `.env`, no a `installation.json` |
| Agente principal (home) | Listo | Chat tipo asistente con contexto de sistema OCAP |
| IA y modelos | Listo | CRUD por tenant, probar/usar/editar, preferred provider |
| Gemini | Listo | Modelo por defecto `gemini-3.5-flash`; auth por `x-goog-api-key` |
| WhatsApp Evolution | Listo (código) | Cliente + webhooks; requiere Evolution en marcha y vars `EVOLUTION_*` / `WhatsApp__*` |
| WebChat | Listo | Canal usable en panel |
| Workflows UI | Fuera de v1 | Rutas placeholder; motor backend sigue en el repo |

## Cómo verificar IA

1. Configura `AiProviders__Gemini__ApiKey` en `.env` (no uses `installation.json` para secretos).
2. Reinicia la API.
3. En **IA y modelos**, pulsa **Probar** en Gemini.
4. En el home, envía un mensaje al agente principal.

Si ves `API_KEY_INVALID` con key `***`, es un artefacto viejo de instalación: regenera/limpia `ApiKey` en JSON de instalación o asegúrate de que el `.env` gane (la API vuelve a cargar variables de entorno después de `installation.json`).

## Pendiente / siguiente

- Conectar Evolution real de punta a punta (QR, instancia estable, mensajes entrantes/salientes).
- Persistir fuera de UseInMemory cuando se quiera DB Postgres local/Docker.
- Reintroducir workflows en UI solo cuando el producto lo priorice.
- Rotar secrets de desarrollo si se compartieron en entornos no locales.

## Artefactos locales (no versionados)

- `.env`
- `src/Api/OCAP.Api/config/installation.json`
- `src/Api/OCAP.Api/config/generated.env`
