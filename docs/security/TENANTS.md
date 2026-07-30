# OCAP — Arquitectura Multi-Tenant

## Aislamiento de Organizaciones
OCAP implementa la estrategia de aislamiento por `TenantId` en la capa de persistencia (EF Core `HasQueryFilter` + interceptor de escritura).

Detalle operativo: [TENANT_ISOLATION.md](./TENANT_ISOLATION.md).

## Agregado Tenant
- `Tenant`: Representa la empresa u organización cliente.
- `TenantMember`: Relación entre un usuario e identidades de tenant.
- `SettingsJson`: Reglas de configuración por organización.
