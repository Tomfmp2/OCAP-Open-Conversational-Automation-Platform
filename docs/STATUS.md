# Estado de OCAP 1.0.0

Última actualización: 2026-11-03

OCAP 1.0.0 es el primer lanzamiento público. Se instala con `./ocap install` (o la línea de `install.sh`) y corre
completo en Docker: panel en `http://localhost:3000` y API en `http://localhost:5000`.

## Qué está listo

| Área | Estado | Notas |
| --- | --- | --- |
| Instalación y comando `ocap` | Listo | Instalador en español o inglés, secretos aleatorios, inicio automático, copias y restauración |
| Asistente del panel | Listo | Conversaciones guardadas por usuario, memoria, adjuntos (imágenes, video con Gemini, documentos), Google Workspace |
| Agentes de canal | Listo | Instrucciones, memoria, chat de prueba, pase a una persona |
| Capacidades por canal | Listo | El canal elige colecciones y acciones de su agente ([ADR-011](adr/ADR-011-channel-capabilities.md)) |
| Memoria | Listo | Hechos, episodios y resúmenes con plazo configurable y «olvídame» ([ADR-010](adr/ADR-010-agent-memory.md)) |
| WhatsApp por QR | Listo | Evolution API v2.3.7, contactos con LID, QR negro |
| WhatsApp Business | Listo | API de Meta configurada desde el panel, firma por número |
| Telegram | Listo | Varios bots, polling con offset persistente y deduplicación, webhook opcional |
| WebChat | Listo | Widget con dominios autorizados y clave rotable |
| Bandeja | Listo | Tomar, responder y devolver; el agente retoma tras 15 minutos sin el equipo |
| Apagado de canales | Listo | Termina los chats en curso y se apaga hasta encenderlo |
| Conocimiento | Listo | PDF, Word, Excel, CSV y más; respuestas escritas; importar páginas; ver contenido |
| Modelos de IA | Listo | Gemini, OpenAI, Claude, Ollama y compatibles con OpenAI; varias configuraciones por proveedor |
| Organización | Listo | SMTP, Google Workspace, idioma, zona horaria, retención, memoria, aviso de privacidad |
| Equipo | Listo | Invitaciones con roles y recuperación de contraseña |
| Aislamiento por organización | Listo | Filtros de EF y *row-level security* en PostgreSQL con rol sin privilegios |
| Workflows | Solo API | Motor en el repositorio, sin interfaz en la v1 |

## Calidad

- Backend: build sin avisos, pruebas unitarias, de integración y de endpoints, también sobre PostgreSQL real
  (Testcontainers, RLS y rol limitado).
- Frontend: lint, tipos y Vitest.
- E2E: Playwright en escritorio y móvil, y auditoría de accesibilidad con axe, sobre un stack aislado.
- `./scripts/ci-local.sh` reproduce el CI de GitHub y corre antes de cada push (hook en `.githooks`).

## Limitaciones conocidas

- **Búsqueda en el conocimiento**: semántica (pgvector) con una búsqueda por palabras básica. La reforma (búsqueda
  híbrida real, fragmentos con contexto, reordenamiento y consultas exactas sobre tablas) es lo siguiente en el
  [roadmap](roadmap/ROADMAP.md).
- **PDF escaneados**: sin OCR; se indica que no tienen texto.
- **Archivos originales**: no se guardan; para reprocesar un documento hay que volver a subirlo.
- **Correo saliente**: la conexión SMTP valida el servidor antes de conectar, pero no fija la IP validada (riesgo
  residual de DNS rebinding limitado a puertos de correo).
- **WhatsApp por QR**: depende de Evolution API (Baileys), que no es la API oficial de Meta.
