# OCAP — Arquitectura Multi-Tenant

## Aislamiento de Organizaciones
OCAP implementa la estrategia de aislamiento por `TenantId`.
Todas las consultas de dominio, almacenamiento en PostgreSQL y ejecución de agentes incluyen el filtro explícito de `TenantId` para prevenir fugas de datos entre empresas.

## Agregado Tenant
- `Tenant`: Representa la empresa u organización cliente.
- `TenantMember`: Relación entre un usuario e identidades de tenant.
- `SettingsJson`: Reglas de configuración por organización.
