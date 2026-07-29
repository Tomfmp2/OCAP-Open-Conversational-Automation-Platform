# ADR-005: Módulo de Administración de Identidades y Organizaciones (CAP-16)

## Estado
Aprobado

## Contexto
Para consolidar las capacidades empresariales de OCAP y preparar la plataforma para integraciones avanzadas de SSO (SAML2), aprovisionamiento SCIM y autenticación sin contraseña (Passkeys), se requiere una capa centralizada y desacoplada para la administración de usuarios, roles, permisos, grupos y organizaciones (Tenants).

## Decisiones de Diseño

1. **Abstracción del Dominio de Administración**:
   - `IUserManagementService`: Encapsula operaciones administrativas de ciclo de vida de usuario (invitación, bloqueo, desbloqueo, activación, desactivación, reseteo y cambio de contraseña, verificación de correo).
   - `IGroupService`: Administra grupos multi-tenant, asignación masiva de usuarios y roles por grupo.
   - `ITenantManagementService`: Gestiona la jerarquía de tenants y membresías (`TenantMember`).

2. **Aislamiento Multi-Tenant**:
   - Todas las consultas y mutaciones administrativas imponen filtro estricto por `TenantId`.

3. **Auditoría Estricta**:
   - Cada evento administrativo genera un registro inmutable en `AuditLog` etiquetado con tipo de evento y resultado (`User.Invited`, `User.Locked`, `Group.Created`, `Role.Created`).

## Consecuencias
- Módulo de administración completamente testeable e independiente.
- Transición transparente hacia CAP-17 (MFA/TOTP/Passkeys) y CAP-18 (SAML2/SCIM).
