# OCAP — Especificación del Motor de Ejecución (IWorkflowEngine)

Implementación: `OCAP.Workflow.Application.Services.WorkflowEngine`.

## Capacidades
1. **StartWorkflowAsync**: valida tenant, carga definición, fija `WorkflowVersionNumber`, ejecuta desde Start.
2. **Pause / Resume**: persistencia de variables + rehidratación; Resume exige `TenantId`.
3. **ResumeWithSignalAsync / SignalAsync**: reanuda Wait, Delay (`__delay__`) y HumanApproval (`approved`/`rejected`).
4. **CancelWorkflowAsync**: cancela y ejecuta compensación LIFO (`CompensationJson`).
5. **Retry / Timeout**: por paso vía `retryCount`, `retryDelayMs`, `timeoutSeconds`.
6. **Error handling**: enruta a nodo `ErrorHandler` si existe; si no, falla y compensa.
7. **Historial**: cada paso escribe `WorkflowExecutionHistory` (tenant-scoped).
8. **Transiciones**: evaluadas con `IWorkflowExpressionEvaluator` cuando `ConditionExpression` no es trivial.
9. **Scheduler**: `WorkflowResumeHostedService` reanuda ejecuciones con `WaitUntilUtc` vencido.

## Persistencia
- Repositorios EF: `EfWorkflowDefinitionRepository`, `EfWorkflowExecutionRepository`.
- Columnas de ejecución enterprise: `WorkflowVersionNumber`, `WaitSignal`, `WaitUntilUtc`, `CompensationJson`, `ResumePayloadJson`.
- Migración: `AddWorkflowRuntimeEnterprise`.

## Aislamiento
- Lecturas de ejecución con tenant (`GetByIdAsync(id, tenantId)`).
- Variables e historial asociados a `TenantId`.
