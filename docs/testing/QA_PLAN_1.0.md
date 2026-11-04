# Plan de pruebas QA 1.0

Prueba manual del panel en Chrome sobre la rama `develop`, con Gemini real. Complementa al CI y a
[E2E_CRITICAL_PATH.md](E2E_CRITICAL_PATH.md), que cubre solo el camino feliz.

## Reglas

- **Datos**: todo lo que se crea lleva el prefijo `QA-` y se elimina al terminar. No se tocan los bots, contactos ni
  documentos existentes. Antes de empezar se guarda una copia de la base con `pg_dump`.
- **Fuera de alcance sin permiso**: enviar mensajes a personas reales, cambiar el WhatsApp vinculado, invitar correos
  reales y conectar la cuenta de Google.
- **Presupuesto de Gemini**: máximo **20 peticiones de chat por modelo y día**, el límite real de la capa gratuita, y **20 de embeddings**, con textos cortos. Primero se prueba
  todo lo que no gasta tokens. Se detiene si Gemini responde 429 o si se llega al tope.
- **Medición**: se anotan las llamadas y los tokens por prueba. Si el panel no los registra, se anota como hallazgo y
  se cuentan las llamadas desde los registros de la API.

## Casos

Severidad si falla: **A** bloquea el uso, **M** tiene alternativa, **B** es menor.

| ID | Área | Prueba | Gasta tokens | Sev. |
| --- | --- | --- | --- | --- |
| AUTH-1 | Sesión | Entrar, recargar y seguir con sesión; salir | No | A |
| AUTH-2 | Sesión | Credenciales incorrectas dan un error claro; ruta privada sin sesión lleva a login | No | A |
| AUTH-3 | Sesión | «Olvidé mi contraseña» no revela si el correo existe | No | M |
| NAV-1 | Panel | Todas las páginas cargan sin errores de consola ni respuestas 4xx/5xx | No | A |
| NAV-2 | Panel | Estados vacío, carga y error; teclado y foco visibles; ancho de móvil | No | M |
| AI-1 | Modelos | Lista la configuración, nunca devuelve la clave | No | A |
| AI-2 | Modelos | Probar conexión con Gemini | Sí, 1 | A |
| AI-3 | Consumo | Los tokens gastados aparecen en el panel o en `/api/ai/usage` | No | M |
| KB-1 | Conocimiento | Crear colección `QA-` | No | A |
| KB-2 | Conocimiento | Subir `.xlsx`, `.docx`, `.pdf` y `.csv`; ver fragmentos | Embeddings | A |
| KB-3 | Conocimiento | `.xls` rechazado con aviso; archivo con extensión falsa rechazado | No | M |
| KB-4 | Conocimiento | Escribir respuesta; importar página; URL interna rechazada | Embeddings | M |
| KB-5 | Conocimiento | «Probar» encuentra el dato correcto | Embeddings | A |
| KB-6 | Conocimiento | Eliminar documento y colección | No | M |
| AG-1 | Agentes | Crear, editar y eliminar un agente `QA-`; validaciones | No | A |
| CH-1 | Canales | Lista, estados y formularios; token inválido de Telegram da un error claro | No | A |
| CH-2 | Canales | WebChat `QA-`: conectar, asignar agente, conocimiento y acciones | No | A |
| CH-3 | Canales | WebChat responde con Gemini | Sí, 1 | A |
| CH-4 | Canales | Responde con un dato del Excel y no inventa lo que no está | Sí, 2 | A |
| CH-5 | Canales | Con una colección asignada no lee las demás | Sí, 1 | A |
| CH-6 | Canales | Apagar: sigue el chat en curso y un chat nuevo no recibe respuesta; encender | Sí, 1 | A |
| INB-1 | Conversaciones | La conversación aparece; un operador responde | No | A |
| INB-2 | Conversaciones | Con operador el agente calla; «devolver al agente» lo reactiva | Sí, 1 | A |
| MEM-1 | Memoria | El agente recuerda un dato en una conversación nueva | Sí, 3 | A |
| MEM-2 | Memoria | Ver, corregir y borrar en el panel | No | M |
| MEM-3 | Memoria | «Olvídame» borra sin llamar al modelo | No | A |
| MEM-4 | Memoria | Una tarjeta o contraseña escrita no se guarda | Sí, 2 | A |
| SEC-1 | Seguridad | Inyección en el chat no revela instrucciones ni ejecuta acciones | Sí, 2 | A |
| SEC-2 | Seguridad | Sin sesión la API responde 401; sin permiso, 403 | No | A |
| SEC-3 | Seguridad | Las respuestas no incluyen claves ni tokens; XSS en nombres y mensajes no se ejecuta | No | A |
| AST-1 | Asistente | Conversa y conoce la fecha local | Sí, 2 | M |
| AST-2 | Asistente | Una acción con efectos pide confirmación antes de ejecutarse | Sí, 2 | A |
| ORG-1 | Organización | Preferencias persisten tras recargar; dirección `http://` pública rechazada | No | M |
| ORG-2 | Organización | Correo: puerto no permitido y host interno rechazados; la contraseña no vuelve | No | M |
| TEAM-1 | Equipo | Lista y roles; invitación `QA-` a `@example.com` con enlace copiable; revocar | No | M |
| OPS-1 | Operación | La API sobrevive a un reinicio y los canales siguen; registros sin secretos | No | M |

## Resultados del 2026-11-04

Ejecutado en `develop` (`9a2657d`). Los datos `QA-` se eliminaron al terminar.

| Estado | Casos |
| --- | --- |
| Pasan | AUTH-2, AUTH-3, NAV-1, NAV-2, AI-1, KB-1 a KB-5, AG-1, CH-1 a CH-6, INB-1, INB-2 (sin la reactivación con IA), MEM-2, MEM-3, SEC-2, SEC-3, AST-1, ORG-1, ORG-2, TEAM-1 |
| Fallan | AI-2 con `gemini-3.5-flash-lite` (más de 30 s), AI-3 |
| Parciales | KB-6: los documentos se borran, las colecciones no. OPS-1: la API arranca y no hay secretos en los registros; no se comprobó un mensaje real de Telegram |
| Sin probar | AUTH-1 (el admin tiene 2FA y cerrar sesión cortaba la prueba), MEM-1 y MEM-4 (sin cuota), SEC-1 (ninguna inyección llegó al modelo por 503 o cuota), AST-2 (no hay herramientas con efectos sin Google) |

Tampoco se probó el envío real por Telegram, WhatsApp o SMTP, ni la conexión con Google.

### Hallazgos

| # | Sev. | Hallazgo | Dónde |
| --- | --- | --- | --- |
| 1 | A | El proxy de Next corta a los 30 s toda petición a la API, aunque la API admite 120 s. El widget muestra «Failed to fetch», el asistente «Error HTTP 500» y el cliente se queda sin respuesta | `frontend/next.config.ts` sin `proxyTimeout`; el `catch` de `ChannelConversationService.cs:180` excluye la cancelación; `ocap-webchat.js:211` |
| 2 | A | Los errores `application/problem+json` no se interpretan: el panel muestra «Error HTTP 400» en lugar del motivo | `shared/api/apiClient.ts:195` |
| 3 | A | El consumo no es real: Gemini declara siempre 120 tokens y `/api/ai/usage` sigue en 0 tras usar canales y asistente | `GeminiAiProvider.cs:96` |
| 4 | M | El trabajo de memoria reintenta cada minuto sin espera creciente, también con 429 y 503, y gasta una cuota de solo 20 peticiones diarias | `ConversationMemoryBackgroundService.cs:25` |
| 5 | M | WebChat no permite elegir agente ni modelo en el panel, aunque Canales dice que sí; solo se puede por API | `ChannelSettings.tsx:44` |
| 6 | M | Un 503 de Gemini agota su único reintento y deja al cliente la respuesta de respaldo. No hay cambio a otro modelo | `ApiServiceExtensions.Ai.cs:24` |
| 7 | B | Las colecciones no se pueden eliminar, ni en el panel ni por API | `KnowledgeController.cs` |
| 8 | B | WebChat empieza una conversación nueva al recargar o cambiar de página, por decisión de diseño. La memoria no sirve entre visitas | `ocap-webchat.js:36` |
| 9 | B | Equipo pide definir SMTP en `.env`, aunque se configura en el panel; el agente menciona «Ajustes», que en el menú se llama Organización | `/team`, formulario de agente |

Una pestaña dejó de responder una vez al abrir la memoria. No se pudo reproducir.

### Estado de los hallazgos

Corregidos en `develop` los hallazgos 1 a 7 y 9. El 8 sigue igual a propósito: guardar la sesión de WebChat exige dejar
datos en el navegador del visitante.

El hallazgo 6 estaba mal descrito: los modelos del panel no tenían ningún reintento, porque el configurado solo cubría
los clientes tipados.

Faltan por repetir con cuota real: AI-2 con `flash-lite`, MEM-1, MEM-4, SEC-1 y AST-2.

## Segunda ronda del 2026-11-04

Ejecutada en `develop` (`46dc487`), con los datos `QA-` eliminados al terminar. Gasto: 5 llamadas de chat, más una extracción de memoria que Google rechazó por cuota.

| Estado | Casos |
| --- | --- |
| Pasan | SEC-1 (inyección en WebChat), AI-2 con `flash-lite` en una prueba corta, AI-3, KB-6, cambio real de `flash` a `flash-lite`, dominios autorizados, rotación de clave, validaciones del panel, accesibilidad con axe salvo el hallazgo 17 |
| Sin completar | MEM-1 y MEM-4: Google rechazó la extracción por cuota; la pausa de memoria solo tiene pruebas automáticas, porque la cola quedó vacía al limpiar; AST-2 y los envíos reales por Telegram, WhatsApp y SMTP |

| # | Sev. | Hallazgo | Dónde |
| --- | --- | --- | --- |
| 10 | A | El proxy del panel trunca las subidas a 10 MB, aunque el asistente admite videos de 15 MB y peticiones de 32 MB. Un archivo de 10,5 MB da un 500 sin motivo | `frontend/next.config.ts` |
| 11 | A | Se pueden subir documentos a una colección inexistente: quedan huérfanos pero indexados | `KnowledgeService.UploadDocumentAsync` |
| 12 | M | La API acepta proveedores de IA inexistentes, y esa configuración puede quedar como predeterminada | `AiProviderConfigurationService.cs` |
| 13 | M | Tarjetas y contraseñas escritas por un cliente quedan en texto plano en el historial de la conversación | `ChannelConversationService.cs` |
| 14 | M | Un intento que vence por plazo no queda en el uso, porque se confunde con una cancelación del cliente | `UsageTrackingAiProvider.cs` |
| 15 | M | Un nombre de colección de más de 250 caracteres da un 500 | `KnowledgeController.cs` |
| 16 | M | Si la persona escribe un solo mensaje, no se extrae memoria aunque traiga sus datos | `ConversationMemoryBackgroundService.cs:183` |
| 17 | B | El área de mensajes de una conversación no se puede enfocar con el teclado | `ConversationDetail.tsx` |
| 18 | B | Colecciones duplicadas, también con doble clic | `KnowledgeController.cs` |
| 19 | B | Se aceptan agentes con herramientas inexistentes o sin instrucciones | `AgentsController.cs` |
| 20 | B | Los registros marcan como 500 errores que el cliente recibe como 400 o 422 | orden de middlewares |
| 21 | B | El texto «sus 1 documentos» tiene mal la concordancia | `app/knowledge/page.tsx` |
| 22 | B | La búsqueda de memoria usa `Take` sin `OrderBy`, y `.env` y `docker-compose.yml` conservan ajustes que van en el panel | `ConversationMemoryBackgroundService.cs`, `docker-compose.yml` |

### Estado de la segunda ronda

Corregidos en `develop` los hallazgos 10 a 22. Tarjetas y claves se ocultan antes de guardar; cédulas y pasaportes siguen
visibles en el historial para la atención al cliente, aunque la memoria no los guarda. Las colecciones repetidas se
rechazan por comprobación previa, sin índice único: dos peticiones simultáneas todavía podrían crearlas.

