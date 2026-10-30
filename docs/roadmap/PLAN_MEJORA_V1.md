# Plan de mejora hacia OCAP v1

Diagnóstico del 2026-10-22 sobre la rama `refactor/instalador` y plan priorizado. Cada fase termina con todo el CI en
verde; ninguna fase empieza sin que la anterior lo esté.

## Diagnóstico

### Build y CI

| Área | Estado | Detalle |
| --- | --- | --- |
| Backend build + 260 pruebas | Verde en local | Arreglado en `refactor/instalador` |
| Frontend lint, type-check, pruebas, build | Verde en local | Idem |
| CI en GitHub | **Bloqueado** | La cuenta está bloqueada por billing: los jobs no llegan a arrancar |
| Dependencias .NET | Sin vulnerabilidades | `dotnet list package --vulnerable` |
| Dependencias frontend | **1 crítica, 3 altas** | `next` 16.2.12 (corregido en 16.4.x), `sharp`, `postcss`, `nanoid` |
| Escaneo de imágenes (Trivy) | No bloquea | `exit-code: '0'`: informa pero nunca falla el pipeline |
| Pruebas huérfanas | **No se ejecutan** | `tests/OCAP.Channels.WhatsApp.Tests` tiene 3 archivos de prueba sin `.csproj`: no están en la solución |
| Avisos de compilación | 2 | `CS8602` en `AiProviderConfigurationsController` |

### WhatsApp con Evolution API (no funciona de punta a punta)

1. **Versión incompatible**: Compose usa `evoapicloud/evolution-api:v1.8.7`, pero `EvolutionApiClient` envía el
   formato de la v2 (`integration: WHATSAPP-BAILEYS`, webhook anidado) y, para `sendText`, mezcla el cuerpo de v1 y v2.
2. **La sesión no persiste**: el contenedor de Evolution no tiene volumen ni base de datos; al recrearlo se pierde la
   vinculación y hay que escanear de nuevo.
3. **El webhook apunta a un lugar que no existe dentro de Docker**: por defecto
   `http://host.docker.internal:5229/api/webhooks/whatsapp`; en Compose la API está en `http://ocap-api:5000`.
4. **Los mensajes entrantes no llegan a nadie**: `WhatsAppMessageReceiver` descarta la instancia y publica el evento
   con `TenantId = Guid.Empty`; no hay forma de saber de qué organización es el mensaje ni por qué número responder.
5. **Seguridad**:
   - `GET qr/{instanceName}` y `GET evolution/state/{instanceName}` no comprueban que la instancia sea de la
     organización (cualquier tenant puede ver o refrescar el QR de otro).
   - El webhook **acepta todo** si no hay secreto configurado (falla abierto).
   - La conexión guarda la API key global de Evolution dentro de su secreto (`evolution:{instancia}:{apikey}`).
6. **Experiencia**: el panel consulta el QR y el estado por *polling*; no reacciona a `QRCODE_UPDATED` ni a
   `CONNECTION_UPDATE`, ni avisa cuando el teléfono se desvincula.

### Arquitectura y código

| Hallazgo | Magnitud |
| --- | --- |
| Proyectos vacíos en la solución (0 líneas) | 15: `OCAP.Modules.*` (12) y canales Discord, Teams, Slack |
| Panel duplicado | `OCAP.Dashboard` (Blazor, puerto 8081) repite lo que hace el panel Next.js |
| Controladores con `OCAPDbContext` directo | 16 de 41: lógica de negocio y acceso a datos en la capa HTTP |
| Respuestas que devuelven `ex.Message` | 30: filtran detalles internos y no siguen `application/problem+json` |
| Clases de configuración enormes | `ApiServiceExtensions` (494 líneas) registra todo el sistema |
| Proveedores sin uso real | S3, Azure Blob, Google Drive, Microsoft 365: código sin pruebas ni configuración documentada |
| Sin modo demo de IA | Sin API key el asistente no responde, aunque existe el proveedor Mock |
| Desarrollo sin Docker | Solo hay `ocap-dev.ps1` (Windows) |

## Definición de terminado (aplica a todas las fases)

- Los 5 jobs del CI en verde: build y pruebas del backend, frontend, escaneo de seguridad, SBOM, build de Docker.
- Trivy y `npm audit` **fallan** el pipeline ante vulnerabilidades críticas (se quita `exit-code: '0'` y `|| true`).
- 0 avisos de compilación (`TreatWarningsAsErrors` en CI) y 0 errores de lint.
- `docker compose down -v && ./scripts/ocap-up.sh` deja OCAP funcionando desde cero.
- Documentación actualizada (README, `docs/STATUS.md`, CHANGELOG).

## Fase 0 — Base verde (prioridad máxima, ~1 día)

1. Desbloquear el billing de GitHub (Settings → Billing and plans) y relanzar el CI.
2. Actualizar `next` a 16.4.x y resolver `sharp`, `postcss` y `nanoid`; volver a pasar lint, pruebas y build.
3. Crear `OCAP.Channels.WhatsApp.Tests.csproj`, sumarlo a la solución y arreglar lo que falle.
4. Hacer bloqueantes Trivy (CRITICAL) y las auditorías de dependencias; activar `TreatWarningsAsErrors` en CI y
   corregir los 2 `CS8602`.
5. Eliminar los 15 proyectos vacíos y la carpeta residual `tests/OCAP.DeploymentManager.Tests`.
6. Actualizar `frontend/e2e/smoke.spec.ts` (textos viejos como "Sign In") y ejecutarlo en CI contra el stack de Compose.

## Fase 1 — WhatsApp automatizado: escanear el QR y listo (~4–5 días)

> **Estado: hecha.** Implementada según este plan, con dos ajustes: Evolution guarda las sesiones en un esquema
> propio (`evolution_api`) del Postgres de OCAP en vez de otra base, y el panel usa SignalR con una consulta de respaldo
> solo si el hub no conecta. Validado contra Evolution v2.3.7 real (QR, webhook firmado, desvincular); falta la
> checklist con un teléfono real (`docs/testing/WHATSAPP_E2E.md`). Diseño en `docs/channels/WHATSAPP.md`.

Objetivo: en **Canales → WhatsApp → Vincular** aparece el QR; al escanearlo con el teléfono, el canal queda vinculado
y el asistente responde los mensajes, sin configurar nada a mano.

### Infraestructura
1. Migrar a Evolution API **v2** con una versión fijada (no `latest`), usando el Postgres del stack (base `evolution`)
   y Redis, con volumen para las sesiones: la vinculación sobrevive reinicios y reconstrucciones.
2. `ocap-setup.sh` genera también `WHATSAPP_WEBHOOK_SECRET`; Compose pasa a la API la URL interna de Evolution y la
   URL del webhook (`http://ocap-api:5000/api/webhooks/whatsapp`). Nada se configura a mano.

### Backend
3. Un cliente `EvolutionApiClient` alineado al contrato v2 (crear instancia con webhook y eventos
   `QRCODE_UPDATED`, `CONNECTION_UPDATE`, `MESSAGES_UPSERT`; `sendText` con `{ number, text }`; `logout` y `delete`),
   con pruebas de contrato contra respuestas grabadas de la v2.
4. Una instancia por organización con nombre determinista (`ocap-{tenantId}`) y creación idempotente; la relación
   instancia → organización queda en la conexión del canal.
5. Webhook que **falla cerrado** (exige el secreto), resuelve la organización por la instancia y:
   - `QRCODE_UPDATED`: guarda el QR vigente y avisa al panel;
   - `CONNECTION_UPDATE`: marca el canal como vinculado (con el número) o desvinculado;
   - `MESSAGES_UPSERT`: ignora mensajes propios, grupos y estados; deduplica por id de mensaje; abre o continúa la
     conversación del contacto, ejecuta el agente principal y responde por la misma instancia.
6. Endpoints del panel con comprobación de pertenencia: `POST /api/channels/whatsapp/link` (crea o reutiliza la
   instancia y devuelve el QR), `GET .../link` (estado y QR vigente), `DELETE .../link` (desvincular). Se elimina el
   acceso por `instanceName` arbitrario y la API key global sale del secreto de la conexión.

### Frontend
7. Modal de vinculación en tres estados (QR, vinculando, vinculado con el número) que se actualiza en vivo por el
   hub de SignalR existente, con reintento del QR al vencer y botón **Desvincular**. Sin *polling*.

### Pruebas y criterio de aceptación
8. Pruebas unitarias (mapeo de eventos, resolución de organización, deduplicación, filtros) e integración del webhook
   con un Evolution falso; checklist manual con un teléfono real en `docs/testing/WHATSAPP_E2E.md`.
9. Aceptación: desde un stack recién creado, escanear el QR vincula el número en menos de un minuto; un mensaje al
   número recibe respuesta del asistente; reiniciar el stack mantiene la vinculación; desvincular desde el panel o
   desde el teléfono se refleja en el panel.

## Fase 2 — Arquitectura y código limpio (~4 días)

> **Estado: hecha, con dos ajustes de alcance.**
> - Hechos: 1 (los `catch (Exception)` que devolvían `ex.Message` quedan limitados a excepciones de negocio; el resto
>   llega al middleware `ProblemDetails`), 3, 4 (Blazor eliminado), 5 y la división de `PrincipalAgentChat`.
> - 2, parcial: `WorkflowsController` usa los repositorios existentes y `DashboardController` usa `DashboardQueries`.
>   Los controladores que hacen una sola consulta de lectura (sesiones, permisos, ejecuciones…) se dejan así: un
>   servicio por consulta sería ceremonia sin beneficio. Los filtros globales de EF ya los acotan por tenant.
> - 6, pospuesto: generar tipos desde OpenAPI agrega una dependencia y, con muchos endpoints que devuelven objetos
>   anónimos, el esquema saldría débil. Conviene hacerlo cuando los controladores devuelvan DTOs tipados.
> - `intelligence/page.tsx` (284 líneas) ya está compuesta de componentes; no se dividió.

1. **Errores uniformes**: un middleware traduce excepciones a `ProblemDetails`; se eliminan los 30 `ex.Message` y
   las respuestas `ApiResponse` ad hoc de los controladores tocados.
2. **Controladores delgados**: mover el acceso a datos de los 16 controladores con `OCAPDbContext` a casos de uso en
   la capa Application, empezando por canales, integraciones y conversaciones (los que usa la v1).
3. Dividir `ApiServiceExtensions` en un `AddXxx` por módulo (seguridad, IA, canales, conocimiento, workflows,
   observabilidad).
4. **Alcance honesto**: quitar `OCAP.Dashboard` (Blazor) y su contenedor; marcar como experimentales los proveedores de
   almacenamiento sin pruebas o sacarlos de la solución; mantener el motor de workflows fuera de la UI de la v1.
5. **Modo demo de IA**: sin ninguna clave configurada, el agente usa el proveedor Mock y el panel lo indica, para que
   quien clone el repositorio pueda probar el chat.
6. Frontend: tipos del cliente generados desde OpenAPI en lugar de interfaces escritas a mano; dividir los
   componentes de más de 250 líneas.

## Fase 3 — Producto, documentación y release (~1–2 días)

> **Estado: 1 y 2 hechos** (`ocap-dev.sh`, README con capturas, insignia de CI y alcance de la v1); CHANGELOG y
> `STATUS.md` actualizados. Pendiente de decisión: el número de versión (el CHANGELOG ya registra 2.0.0 y 2.1.0, así
> que `v1.0.0` iría hacia atrás en SemVer) y publicar la release, que requiere CI en verde.

1. `scripts/ocap-dev.sh` para Linux y macOS, equivalente a `ocap-dev.ps1`.
2. README con capturas o GIF del flujo (configuración inicial → vincular WhatsApp → conversación), insignia de CI y
   tabla de alcance de la v1.
3. CHANGELOG, `docs/STATUS.md` y release `v1.0.0` en GitHub.
4. Actualizar en el CV y el portafolio el número real de pruebas cuando todo esté en verde.

## Orden y dependencias

```
Fase 0 (CI verde) ──► Fase 1 (WhatsApp) ──► Fase 2 (arquitectura) ──► Fase 3 (release)
                          └─ el modo demo de IA (2.5) puede adelantarse si se necesita para la demo
```

Cada fase se entrega en su propia rama con commits atómicos y el CI en verde antes de fusionarla en `main`.
