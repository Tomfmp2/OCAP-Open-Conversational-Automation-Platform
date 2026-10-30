# OCAP — Aislamiento Multi-Tenant (Persistencia)

## Estrategia

OCAP aplica **aislamiento lógico por `TenantId`** en EF Core:

1. **HasQueryFilter global** en toda entidad con propiedad `TenantId` (excepto catálogos globales).
2. **`TenantSaveChangesInterceptor`** asigna `TenantId` en inserts, rechaza writes cross-tenant e impide mutar `TenantId`.
3. **`ITenantContext`** aporta el tenant activo (`HttpTenantContext` en API; bypass solo sin `HttpContext` para jobs de sistema).
4. **Row-Level Security en PostgreSQL** como segunda barrera: aunque una consulta olvide el filtro de EF o use
   `IgnoreQueryFilters`, la base no devuelve ni acepta filas de otra organización.

## Row-Level Security y rol de la base

- `RowLevelSecurity.ApplyPoliciesAsync` crea la política `ocap_tenant_isolation` en **toda tabla con `TenantId`**
  después de cada migración (recorre el esquema: una tabla nueva queda protegida sola).
- `TenantRlsConnectionInterceptor` fija en cada apertura de conexión `ocap.tenant_id` (organización de la petición)
  y `ocap.bypass_rls`. Sin organización resuelta (login, webhooks antes de identificar el canal, jobs) la conexión
  queda en bypass, igual que `BypassTenantFilters`. Una conexión que no fijó nada no ve filas (falla cerrado).
- En Docker, `ocap-migrate` corre la API con `--migrate` y la contraseña del **dueño** de la base: migra, aplica las
  políticas y crea o actualiza el rol de la API (`OCAP_DB_APP_USER`, por defecto `ocap_app`): sin superusuario,
  sin `BYPASSRLS` y sin permisos de esquema. `ocap-api` solo conoce la contraseña de ese rol.
- La suite de la API corre también sobre PostgreSQL con ese rol (`OCAP_TEST_DATABASE=postgres`, Testcontainers) y
  `PostgresSecurityTests` verifica las políticas y los permisos.

## Entidades globales (sin filtro)

| Entidad | Motivo |
|---------|--------|
| `Tenant` | Catálogo de organizaciones; el Id *es* el tenant |
| `Permission` | Catálogo global de códigos de permiso |
| `InboxMessage` | Idempotencia de mensajería (clave MessageId+ConsumerGroup) |
| OpenIddict.* | Infraestructura OAuth del host |

## IgnoreQueryFilters justificado

- **Refresh tokens**: lookup por el SHA-256 del token opaco (`RefreshTokenService`) — el secreto es el aislante.
- **Auth refresh**: carga de `UserIdentity` / `UserRole` / `Role` tras validar el refresh token.

Cualquier otro `IgnoreQueryFilters` debe documentarse en el mismo sitio de uso.

## Background jobs

Sin `HttpContext`, `HttpTenantContext.BypassTenantFilters == true` para que outbox/retention puedan procesar todas las filas. No usar bypass en peticiones HTTP.
