# OCAP — Aislamiento Multi-Tenant (Persistencia)

## Estrategia

OCAP aplica **aislamiento lógico por `TenantId`** en EF Core:

1. **HasQueryFilter global** en toda entidad con propiedad `TenantId` (excepto catálogos globales).
2. **`TenantSaveChangesInterceptor`** asigna `TenantId` en inserts, rechaza writes cross-tenant e impide mutar `TenantId`.
3. **`ITenantContext`** aporta el tenant activo (`HttpTenantContext` en API; bypass solo sin `HttpContext` para jobs de sistema).

## Entidades globales (sin filtro)

| Entidad | Motivo |
|---------|--------|
| `Tenant` | Catálogo de organizaciones; el Id *es* el tenant |
| `Permission` | Catálogo global de códigos de permiso |
| `InboxMessage` | Idempotencia de mensajería (clave MessageId+ConsumerGroup) |
| OpenIddict.* | Infraestructura OAuth del host |

## IgnoreQueryFilters justificado

- **Refresh tokens**: lookup por token opaco (`RefreshTokenService`) — el secreto es el aislante.
- **Auth refresh**: carga de `UserIdentity` / `UserRole` / `Role` tras validar el refresh token.

Cualquier otro `IgnoreQueryFilters` debe documentarse en el mismo sitio de uso.

## Background jobs

Sin `HttpContext`, `HttpTenantContext.BypassTenantFilters == true` para que outbox/retention puedan procesar todas las filas. No usar bypass en peticiones HTTP.
