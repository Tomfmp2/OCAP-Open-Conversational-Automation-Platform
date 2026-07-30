# OCAP — Modelo de Ejecución y Observabilidad

## Mapeo de Observabilidad
Cada ejecución registra:
- `ExecutionId`: identificador único de ejecución.
- `TenantId` / `UserId` / `AgentId`: aislamiento multi-tenant y rastreo.
- `WorkflowVersionNumber`: versión de definición usada al iniciar.
- `DurationMs` / `Status` por paso en `WorkflowExecutionHistory` (`Success`, `Failed`, `Compensated`, …).
- `WaitSignal` / `WaitUntilUtc`: esperas y delays programados.
- `CompensationJson`: pila LIFO de pasos de compensación.
