# ADR-008: Enterprise Directory Synchronization (SCIM 2.0 & LDAP / Active Directory) (CAP-19)

## Estado
Aprobado

## Contexto
Para habilitar el aprovisionamiento automatizado y la sincronización centralizada de identidades en organizaciones globales (Microsoft Entra ID, Okta, Keycloak, JumpCloud, OneLogin, Google Workspace, Active Directory, OpenLDAP, FreeIPA), OCAP requiere una plataforma unificada de sincronización de directorios basada en los estándares SCIM 2.0 (RFC 7643 y RFC 7644) y conectores de directorio LDAP/LDAPS.

## Decisiones de Diseño

1. **Implementación SCIM 2.0 (RFC 7643 / RFC 7644)**:
   - Endpoints SCIM 2.0 bajo el prefijo `/scim/v2/` (`/Users`, `/Groups`, `/ServiceProviderConfig`, `/Schemas`, `/ResourceTypes`, `/Bulk`).
   - Soporte para metadatos, filtro, paginación, operaciones en lote (Bulk API) y respuestas estructuradas de error con esquema `urn:ietf:params:scim:api:messages:2.0:Error`.

2. **Conector LDAP & Motor de Sincronización**:
   - `LdapService` para la validación de credenciales, Bind, búsquedas filtradas y TLS/LDAPS.
   - `DirectorySyncEngine` para la ejecución de sincronizaciones Completas (Full), Incrementales y Delta con resolución de conflictos y aprovisionamiento automático.
   - `DirectorySyncBackgroundService` para la ejecución de trabajos periódicos en segundo plano.

3. **Seguridad & Aislamiento Multi-Tenant**:
   - Mapeo de identidades externas vía `ScimExternalMapping` con control de versión mediante ETag.
   - Auditoría de seguridad exhaustiva en todas las acciones de aprovisionamiento, desaprovisionamiento, cambios de grupos y autenticaciones LDAP.

## Consecuencias
- Ciclo de vida de identidades automatizado en tiempo real.
- Integración nativa sin dependencias propietarias con los principales proveedores de identidades de la industria.
