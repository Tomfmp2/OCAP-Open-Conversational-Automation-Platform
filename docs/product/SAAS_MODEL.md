# OCAP Commercial SaaS Multi-Tenant Model

## 1. Multi-Tenant Architecture & Commercial Hierarchy
- **Organization (Tenant)**: Unidad comercial independiente con aislamiento de datos a nivel de ORM EF Core, claves de cifrado en bóveda y contexto HTTP `ITenantContext`.
- **Workspaces**: Espacios de trabajo lógicos por equipo o departamento dentro de la organización.
- **Entitlements & Quotas**: Verificación dinámica de derechos mediante `IEntitlementService` previa a la ejecución de flujos o invocación de IA.

## 2. Feature Flags & Commercial Tiers
- **Starter**: 1 Tenant, 5 Agentes, 10 Workflows, 5,000 llamadas API/mes.
- **Professional**: 1 Tenant, 25 Agentes, 50 Workflows, 50,000 llamadas API/mes, Canales ilimitados.
- **Business**: Multi-Tenant, Agentes y Workflows ilimitados, SSO SAML 2.0, SCIM 2.0 y soporte prioritario.
- **Enterprise**: Despliegue en nube dedicada o VPC híbrida, SLA 99.99%, soporte 24/7 y contratos custom.
