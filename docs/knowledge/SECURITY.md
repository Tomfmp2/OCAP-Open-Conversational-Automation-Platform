# Seguridad y Aislamiento Multi-Tenant

## Reglas de Seguridad
1. **Aislamiento por TenantId**: Cada repositorio y base vectorial exige y filtra explícitamente por `TenantId`.
2. **Protección de Embeddings**: Los vectores flotantes crudos nunca se exponen directamente en las APIs públicas, únicamente los textos y fragmentos procesados.
3. **Control de Acceso (RBAC)**: Se integran permisos finos sobre cada documento (`CanRead`, `CanWrite`, `CanDelete`).
4. **Auditoría**: Todas las acciones de subida, eliminación y búsqueda quedan registradas en el registro de auditoría.
