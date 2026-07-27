# OCAP — Modelo de Ejecución y Observabilidad

## Mapeo de Observabilidad
Cada ejecución registra:
- `ExecutionId`: Identificador único de ejecución.
- `TenantId` / `UserId` / `AgentId`: Aislamiento por organización y rastreo del agente invocador.
- `DurationMs`: Medición de tiempo por paso e id del nodo ejecutado.
- `Status`: Estado inmutable por paso (`Success`, `Failed`, `Paused`).
