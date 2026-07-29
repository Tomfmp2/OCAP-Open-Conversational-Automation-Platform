# Enterprise Directory Synchronization (SCIM 2.0 & LDAP - CAP-19)

Documentación técnica y arquitectura de la plataforma de sincronización de identidades de OCAP.

## SCIM 2.0 Endpoints (RFC 7643 / RFC 7644)

- `GET /scim/v2/Users`: Consulta de usuarios con soporte de filtrado, paginación (`startIndex`, `count`) y esquema `urn:ietf:params:scim:schemas:core:2.0:User`.
- `GET /scim/v2/Users/{id}`: Obtiene un usuario específico por su ID.
- `POST /scim/v2/Users`: Aprovisiona un nuevo usuario.
- `PUT /scim/v2/Users/{id}` / `PATCH /scim/v2/Users/{id}`: Actualizaciones de usuario.
- `DELETE /scim/v2/Users/{id}`: Desaprovisionamiento y desactivación de usuario.
- `GET/POST/PUT/DELETE /scim/v2/Groups`: Gestión automatizada de grupos y membresías.
- `POST /scim/v2/Bulk`: Procesamiento en lote de operaciones de aprovisionamiento masivo.
- `GET /scim/v2/ServiceProviderConfig`: Descubrimiento de capacidades del Service Provider.
- `GET /scim/v2/Schemas`: Definición de esquemas SCIM.
- `GET /scim/v2/ResourceTypes`: Definición de tipos de recurso soportados.

## LDAP / Active Directory API REST

- `GET /api/directory/ldap/config`: Consulta la configuración LDAP activa del tenant.
- `POST /api/directory/ldap/config`: Guarda/actualiza la conexión LDAP/LDAPS.
- `POST /api/directory/ldap/test`: Prueba de conectividad Bind al servidor LDAP.
- `POST /api/directory/sync/trigger`: Dispara manualmente un trabajo de sincronización (Full, Incremental, Delta).
- `GET /api/directory/sync/status`: Obtiene el estado del trabajo de sincronización activo o último completado.
- `GET /api/directory/sync/history`: Consulta el historial auditado de ejecuciones de sincronización.
